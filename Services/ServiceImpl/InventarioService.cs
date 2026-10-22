using SPIn.Common;
using SPIn.Common.Excepciones;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;

namespace SPIn.Services.ServiceImpl;

public class InventarioService(
    IInventarioRepository repo,
    IInsumoRepository insumos,
    IAlmacenRepository almacenes,
    IPedidoRepository pedidos,
    ITenantProvider tenant,
    ILogger<InventarioService> logger) : IInventarioService
{
    /// <summary>Tipos que el usuario puede registrar a mano. La salida a producción la genera la Entrega 6.</summary>
    private static readonly HashSet<TipoMovimiento> TiposManuales =
    [
        TipoMovimiento.StockInicial, TipoMovimiento.EntradaCompra, TipoMovimiento.SalidaConsumo,
        TipoMovimiento.AjustePositivo, TipoMovimiento.AjusteNegativo, TipoMovimiento.Merma
    ];

    private static readonly HashSet<TipoMovimiento> RequierenMotivo =
        [TipoMovimiento.SalidaConsumo, TipoMovimiento.AjustePositivo, TipoMovimiento.AjusteNegativo, TipoMovimiento.Merma];

    // ================================================================ stock

    public async Task<GenericResponse<PaginaResponse<StockInsumoResponse>>> StockAsync(FiltroStockRequest filtro)
    {
        var lista = await repo.StockInsumosAsync(filtro);
        var enCamino = await pedidos.EnCaminoPorInsumoAsync();
        foreach (var i in lista) i.EnCamino = enCamino.GetValueOrDefault(i.InsumoId);
        if (filtro.Situacion == "sin") lista = lista.Where(i => i.Situacion == "sin").ToList();
        else if (filtro.Situacion == "bajo") lista = lista.Where(i => i.Situacion is "sin" or "bajo").ToList();
        else if (filtro.Situacion == "sobre") lista = lista.Where(i => i.Situacion == "sobre").ToList();

        var paginas = Math.Max(1, (int)Math.Ceiling(lista.Count / (double)filtro.TamanoPagina));
        var pagina = Math.Min(Math.Max(1, filtro.Pagina), paginas);
        return new GenericResponse<PaginaResponse<StockInsumoResponse>>(new PaginaResponse<StockInsumoResponse>
        {
            Items = lista.Skip((pagina - 1) * filtro.TamanoPagina).Take(filtro.TamanoPagina).ToList(),
            Total = lista.Count,
            Pagina = pagina,
            TamanoPagina = filtro.TamanoPagina
        });
    }

    public async Task<GenericResponse<ResumenStockResponse>> ResumenStockAsync(int? almacenId)
    {
        var lista = await repo.StockInsumosAsync(new FiltroStockRequest { AlmacenId = almacenId });
        return new GenericResponse<ResumenStockResponse>(new ResumenStockResponse
        {
            Insumos = lista.Count,
            SinStock = lista.Count(i => i.Situacion == "sin"),
            BajoMinimo = lista.Count(i => i.Situacion == "bajo"),
            SobreMaximo = lista.Count(i => i.Situacion == "sobre")
        });
    }

    public async Task<GenericResponse<decimal>> CantidadAsync(int insumoId, int almacenId) =>
        new(await repo.CantidadAsync(insumoId, almacenId));

    // ================================================================ movimientos

    public async Task<GenericResponse<PaginaResponse<MovimientoResponse>>> MovimientosAsync(FiltroMovimientoRequest filtro) =>
        new(await repo.ListarMovimientosAsync(filtro));

    public async Task<GenericResponse<MovimientoResponse>> RegistrarAsync(MovimientoRequest r)
    {
        if (!TiposManuales.Contains(r.Tipo))
            throw GenericException.Campo("tipo", "Este tipo de movimiento lo genera el sistema; no se registra a mano.");
        if (RequierenMotivo.Contains(r.Tipo) && string.IsNullOrWhiteSpace(r.Motivo))
            throw GenericException.Campo("motivo", "Indique el motivo: queda en el kardex para auditoría.");

        var insumo = await insumos.ObtenerAsync(r.InsumoId);
        if (insumo is null || !insumo.Activo) throw GenericException.Campo("insumoId", "Seleccione un insumo activo.");
        var almacen = await almacenes.ObtenerAsync(r.AlmacenId);
        if (almacen is null || !almacen.Activo) throw GenericException.Campo("almacenId", "Seleccione un almacén activo.");
        if (almacen.Tipo == TipoAlmacen.ProductoTerminado)
            throw GenericException.Campo("almacenId", "Los insumos no se guardan en un almacén de producto terminado.");

        var cantidad = r.EnUnidadCompra ? r.Cantidad * insumo.FactorConversion : r.Cantidad;

        var movimiento = await repo.EnTransaccionAsync(async () =>
        {
            if (r.Tipo == TipoMovimiento.StockInicial && await repo.HayMovimientosAsync(r.InsumoId, r.AlmacenId))
                throw GenericException.Campo("tipo", "Este insumo ya tiene movimientos en el almacén: use una entrada o un ajuste.");

            var m = await AplicarAsync(r.InsumoId, r.AlmacenId, r.Tipo, cantidad,
                Validaciones.TextoOpcional(r.Documento), Validaciones.TextoOpcional(r.Motivo));
            await repo.GuardarAsync();
            return m;
        });

        logger.LogInformation("Movimiento {MovimientoId} {Tipo} de {Cantidad} del insumo {InsumoId} en almacén {AlmacenId}",
            movimiento.Id, r.Tipo, cantidad, r.InsumoId, r.AlmacenId);

        var respuesta = await UnMovimientoAsync(movimiento.Id);
        return new GenericResponse<MovimientoResponse>(true, respuesta,
            $"{EtiquetasInventario.De(r.Tipo)} registrada. Stock en {almacen.Codigo}: {movimiento.SaldoAlmacen:#,0.####}.");
    }

    public async Task<GenericResponse<MovimientoResponse>> RevertirAsync(long id, RevertirMovimientoRequest request)
    {
        var reversion = await repo.EnTransaccionAsync(async () =>
        {
            var original = await repo.MovimientoAsync(id) ?? throw GenericException.NoEncontrado("El movimiento no existe.");
            if (original.Revertido) throw GenericException.Conflicto("El movimiento ya fue revertido.");
            if (original.RevierteId is not null) throw GenericException.Conflicto("No se puede revertir una reversión.");
            if (original.ConteoId is not null)
                throw GenericException.Conflicto("Los ajustes de un conteo físico no se revierten: haga un nuevo conteo o un ajuste.");
            if (original.ParteProduccionId is not null)
                throw GenericException.Conflicto("Este consumo viene de un parte de producción: anule el parte en Producción.");
            if (original.RecepcionId is not null)
                throw GenericException.Conflicto("Esta entrada viene de la recepción de un pedido: anule la recepción en Pedidos.");

            // El movimiento inverso: lo que entró sale y viceversa.
            var tipoInverso = EtiquetasInventario.Signo(original.Tipo) > 0 ? TipoMovimiento.AjusteNegativo : TipoMovimiento.AjustePositivo;
            var m = await AplicarAsync(original.InsumoId, original.AlmacenId, tipoInverso, original.Cantidad,
                original.Documento, request.Motivo.Trim(), $"Reversión del movimiento N° {original.Id}", null, original.Id);
            original.Revertido = true;
            await repo.GuardarAsync();
            return m;
        });

        logger.LogInformation("Movimiento {MovimientoId} revertido con {ReversionId}", id, reversion.Id);
        return new GenericResponse<MovimientoResponse>(true, await UnMovimientoAsync(reversion.Id),
            $"Movimiento N° {id} revertido. Stock actual: {reversion.SaldoAlmacen:#,0.####}.");
    }

    public async Task<MovimientoStock> AplicarAsync(int insumoId, int almacenId, TipoMovimiento tipo, decimal cantidad,
        string? documento = null, string? motivo = null, string? referencia = null, int? conteoId = null, long? revierteId = null,
        int? parteProduccionId = null, int? recepcionId = null)
    {
        if (cantidad <= 0) throw GenericException.Campo("cantidad", "La cantidad debe ser mayor que 0.");

        var stock = await repo.StockAsync(insumoId, almacenId);
        if (stock is null)
        {
            stock = new Stock { InsumoId = insumoId, AlmacenId = almacenId, Cantidad = 0 };
            repo.AgregarStock(stock);
        }

        var saldo = Math.Round(stock.Cantidad + EtiquetasInventario.Signo(tipo) * cantidad, 6);
        if (saldo < 0)
        {
            throw GenericException.Campo("cantidad",
                $"Stock insuficiente: hay {stock.Cantidad:#,0.####} y se intenta retirar {cantidad:#,0.####}.");
        }

        var ahora = Reloj.Ahora;
        stock.Cantidad = saldo;
        stock.UltimoMovimiento = ahora;

        var movimiento = new MovimientoStock
        {
            Fecha = ahora,
            Tipo = tipo,
            InsumoId = insumoId,
            AlmacenId = almacenId,
            Cantidad = Math.Round(cantidad, 6),
            SaldoAlmacen = saldo,
            Documento = documento,
            Motivo = motivo,
            Referencia = referencia,
            ConteoId = conteoId,
            RevierteId = revierteId,
            ParteProduccionId = parteProduccionId,
            RecepcionId = recepcionId,
            Usuario = tenant.UsuarioNombre ?? "sistema"
        };
        repo.AgregarMovimiento(movimiento);
        return movimiento;
    }

    private async Task<MovimientoResponse> UnMovimientoAsync(long id) =>
        await repo.MovimientoResumenAsync(id) ?? throw GenericException.NoEncontrado("El movimiento no existe.");

    // ================================================================ kardex

    public async Task<GenericResponse<KardexResponse>> KardexAsync(KardexRequest request) =>
        new(await GenerarKardexAsync(request, paginar: true));

    public Task<KardexResponse> KardexCompletoAsync(KardexRequest request) => GenerarKardexAsync(request, paginar: false);

    private async Task<KardexResponse> GenerarKardexAsync(KardexRequest r, bool paginar)
    {
        var insumo = await insumos.ObtenerResumenAsync(r.InsumoId) ?? throw GenericException.NoEncontrado("El insumo no existe.");
        string almacenNombre = "Todos los almacenes";
        if (r.AlmacenId is int almacenId)
        {
            var almacen = await almacenes.ObtenerAsync(almacenId) ?? throw GenericException.NoEncontrado("El almacén no existe.");
            almacenNombre = $"{almacen.Codigo} · {almacen.Nombre}";
        }

        var hoy = Reloj.Hoy;
        var hasta = r.Hasta ?? hoy;
        var desde = r.Desde ?? new DateOnly(hasta.Year, hasta.Month, 1);
        if (desde > hasta) throw GenericException.Campo("desde", "La fecha inicial no puede ser posterior a la final.");
        if (hasta.DayNumber - desde.DayNumber > 3660) throw GenericException.Campo("desde", "El rango máximo es de 10 años.");

        var inicio = desde.ToDateTime(TimeOnly.MinValue);
        var fin = hasta.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var saldoInicial = await repo.SaldoAntesDeAsync(r.InsumoId, r.AlmacenId, inicio);
        var movimientos = await repo.MovimientosEntreAsync(r.InsumoId, r.AlmacenId, inicio, fin);

        var saldo = saldoInicial;
        decimal entradas = 0, salidas = 0;
        var lineas = new List<KardexLineaResponse>(movimientos.Count);
        foreach (var m in movimientos)
        {
            var signo = EtiquetasInventario.Signo(m.Tipo);
            if (signo > 0) entradas += m.Cantidad; else salidas += m.Cantidad;
            saldo += signo * m.Cantidad;
            lineas.Add(new KardexLineaResponse
            {
                Id = m.Id,
                Fecha = m.Fecha,
                Tipo = m.Tipo,
                TipoNombre = EtiquetasInventario.De(m.Tipo),
                AlmacenCodigo = m.Almacen?.Codigo ?? string.Empty,
                Documento = m.Documento,
                Detalle = string.Join(" · ", new[] { m.Referencia, m.Motivo }.Where(x => !string.IsNullOrWhiteSpace(x))),
                Entrada = signo > 0 ? m.Cantidad : null,
                Salida = signo < 0 ? m.Cantidad : null,
                Saldo = Math.Round(saldo, 6),
                Usuario = m.Usuario,
                Revertido = m.Revertido
            });
        }

        var tam = paginar ? r.TamanoPagina : Math.Max(1, lineas.Count);
        var paginas = Math.Max(1, (int)Math.Ceiling(lineas.Count / (double)tam));
        var pagina = paginar ? Math.Min(Math.Max(1, r.Pagina), paginas) : 1;

        return new KardexResponse
        {
            InsumoId = insumo.Id,
            InsumoCodigo = insumo.Codigo,
            InsumoNombre = insumo.Nombre,
            CategoriaNombre = insumo.CategoriaNombre,
            UnidadConsumoCodigo = insumo.UnidadConsumoCodigo,
            StockMinimo = insumo.StockMinimo,
            StockMaximo = insumo.StockMaximo,
            AlmacenNombre = almacenNombre,
            Desde = desde,
            Hasta = hasta,
            SaldoInicial = saldoInicial,
            TotalEntradas = entradas,
            TotalSalidas = salidas,
            SaldoFinal = Math.Round(saldo, 6),
            Movimientos = lineas.Count,
            Lineas = new PaginaResponse<KardexLineaResponse>
            {
                Items = lineas.Skip((pagina - 1) * tam).Take(tam).ToList(),
                Total = lineas.Count,
                Pagina = pagina,
                TamanoPagina = tam
            },
            GeneradoEn = Reloj.Ahora,
            GeneradoPor = tenant.UsuarioNombre ?? string.Empty
        };
    }
}

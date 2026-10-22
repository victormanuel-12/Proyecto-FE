using SPIn.Common;
using SPIn.Common.Excepciones;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;

namespace SPIn.Services.ServiceImpl;

public class ProductoTerminadoService(
    IVentaRepository repo,
    IProductoRepository productos,
    IAlmacenRepository almacenes,
    ITenantProvider tenant,
    ILogger<ProductoTerminadoService> logger) : IProductoTerminadoService
{
    private static readonly HashSet<TipoMovimientoProducto> TiposManuales =
    [
        TipoMovimientoProducto.StockInicial, TipoMovimientoProducto.AjustePositivo,
        TipoMovimientoProducto.AjusteNegativo, TipoMovimientoProducto.Merma
    ];

    public async Task<GenericResponse<PaginaResponse<StockProductoResponse>>> StockAsync(FiltroStockProductoRequest filtro)
    {
        var lista = await repo.StockAsync(filtro);

        // Contexto: venta promedio diaria de los últimos 28 días.
        var hoy = Reloj.Hoy;
        var promedios = await repo.VentaPromedioDiariaAsync(hoy.AddDays(-27), hoy);
        foreach (var p in lista) p.VentaDiariaPromedio = promedios.GetValueOrDefault(p.ProductoId);

        if (filtro.Estado == "activos") lista = lista.Where(p => p.Paquetes > 0).ToList();       // con stock
        else if (filtro.Estado == "inactivos") lista = lista.Where(p => p.Paquetes <= 0).ToList(); // sin stock

        var paginas = Math.Max(1, (int)Math.Ceiling(lista.Count / (double)filtro.TamanoPagina));
        var pagina = Math.Min(Math.Max(1, filtro.Pagina), paginas);
        return new GenericResponse<PaginaResponse<StockProductoResponse>>(new PaginaResponse<StockProductoResponse>
        {
            Items = lista.Skip((pagina - 1) * filtro.TamanoPagina).Take(filtro.TamanoPagina).ToList(),
            Total = lista.Count,
            Pagina = pagina,
            TamanoPagina = filtro.TamanoPagina
        });
    }

    public async Task<GenericResponse<decimal>> PaquetesAsync(int productoId, int almacenId) =>
        new(await repo.PaquetesAsync(productoId, almacenId));

    public async Task<GenericResponse<PaginaResponse<MovimientoProductoResponse>>> MovimientosAsync(int? productoId, int pagina, int tamanoPagina) =>
        new(await repo.MovimientosAsync(productoId, pagina, Math.Clamp(tamanoPagina, 5, 100)));

    public async Task<GenericResponse<MovimientoProductoResponse>> RegistrarAsync(MovimientoProductoRequest r)
    {
        if (!TiposManuales.Contains(r.Tipo))
            throw GenericException.Campo("tipo", "Este movimiento lo genera el sistema (ventas o producción).");
        if (r.Tipo != TipoMovimientoProducto.StockInicial && string.IsNullOrWhiteSpace(r.Motivo))
            throw GenericException.Campo("motivo", "Indique el motivo: queda en el historial para auditoría.");

        var producto = await productos.ObtenerAsync(r.ProductoId);
        if (producto is null || !producto.Activo) throw GenericException.Campo("productoId", "Seleccione un producto activo.");
        await ValidarAlmacenAsync(r.AlmacenId);

        var movimiento = await repo.EnTransaccionAsync(async () =>
        {
            if (r.Tipo == TipoMovimientoProducto.StockInicial && await repo.HayMovimientosAsync(r.ProductoId, r.AlmacenId))
                throw GenericException.Campo("tipo", "Este producto ya tiene movimientos en el almacén: use un ajuste.");
            var m = await AplicarAsync(r.ProductoId, r.AlmacenId, r.Tipo, r.Paquetes,
                Validaciones.TextoOpcional(r.Documento), Validaciones.TextoOpcional(r.Motivo));
            await repo.GuardarAsync();
            return m;
        });

        logger.LogInformation("Movimiento de producto {MovimientoId} {Tipo} de {Paquetes}", movimiento.Id, r.Tipo, r.Paquetes);
        var pagina = await repo.MovimientosAsync(r.ProductoId, 1, 5);
        var resp = pagina.Items.First(x => x.Id == movimiento.Id);
        return new GenericResponse<MovimientoProductoResponse>(true, resp,
            $"{EtiquetasVentas.De(r.Tipo)} registrado. Stock: {movimiento.SaldoAlmacen:#,0.##} paquetes.");
    }

    public async Task<MovimientoProducto> AplicarAsync(int productoId, int almacenId, TipoMovimientoProducto tipo, decimal paquetes,
        string? documento = null, string? motivo = null, string? referencia = null, long? ventaId = null, long? revierteId = null,
        int? parteProduccionId = null)
    {
        if (paquetes <= 0) throw GenericException.Campo("cantidad", "La cantidad debe ser mayor que 0.");

        var stock = await repo.StockDeAsync(productoId, almacenId);
        if (stock is null)
        {
            stock = new StockProducto { ProductoId = productoId, AlmacenId = almacenId, Paquetes = 0 };
            repo.AgregarStock(stock);
        }

        var saldo = Math.Round(stock.Paquetes + EtiquetasVentas.Signo(tipo) * paquetes, 4);
        if (saldo < 0)
        {
            throw GenericException.Campo("cantidad",
                $"Stock de producto terminado insuficiente: hay {stock.Paquetes:#,0.##} paquetes y se intenta retirar {paquetes:#,0.##}.");
        }

        var ahora = Reloj.Ahora;
        stock.Paquetes = saldo;
        stock.UltimoMovimiento = ahora;
        var movimiento = new MovimientoProducto
        {
            Fecha = ahora,
            Tipo = tipo,
            ProductoId = productoId,
            AlmacenId = almacenId,
            Paquetes = Math.Round(paquetes, 4),
            SaldoAlmacen = saldo,
            Documento = documento,
            Motivo = motivo,
            Referencia = referencia,
            VentaId = ventaId,
            RevierteId = revierteId,
            ParteProduccionId = parteProduccionId,
            Usuario = tenant.UsuarioNombre ?? "sistema"
        };
        repo.AgregarMovimiento(movimiento);
        return movimiento;
    }

    private async Task ValidarAlmacenAsync(int almacenId)
    {
        var almacen = await almacenes.ObtenerAsync(almacenId);
        if (almacen is null || !almacen.Activo) throw GenericException.Campo("almacenId", "Seleccione un almacén activo.");
        if (almacen.Tipo != TipoAlmacen.ProductoTerminado)
            throw GenericException.Campo("almacenId", "Elija un almacén de producto terminado.");
    }
}

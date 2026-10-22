using SPIn.Common;
using SPIn.Common.Excepciones;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;
using SPIn.Services.Prediccion;

namespace SPIn.Services.ServiceImpl;

/// <summary>
/// Reposición de insumos: convierte el pronóstico de ventas en consumo de insumos y calcula, con el stock del momento:
///   consumo diario d (pronóstico × receta con merma) y su desviación σd
///   stock de seguridad  SS  = z × √(L × σd² + d² × σL²)      (z según el nivel de servicio del insumo)
///   punto de reorden    ROP = d × L + SS                      (L = tiempo de entrega del proveedor, σL = su variación)
///   posición            P   = stock disponible + en camino
///   cantidad sugerida   Q   = (ROP + d × R) − P, redondeada al múltiplo y al pedido mínimo (R = días de revisión)
/// No guarda nada: se calcula en cada consulta, por eso refleja al instante partes, recepciones y movimientos.
/// </summary>
public class ReposicionService(
    IPrediccionRepository repo,
    IPedidoRepository pedidos,
    IRecetaService recetas,
    IProductoRepository productos) : IReposicionService
{
    private static readonly Dictionary<string, int> Prioridad = new() { ["urgente"] = 0, ["pedir"] = 1, ["sin-proveedor"] = 2, ["ok"] = 3, ["sin-demanda"] = 4 };

    public async Task<GenericResponse<ReposicionPaginaResponse>> ListarAsync(FiltroReposicionRequest f)
    {
        var (lista, fecha, revision) = await CalcularAsync();
        var urgentes = lista.Count(x => x.Estado == "urgente");
        var porPedir = lista.Count(x => x.Estado == "pedir");
        var ok = lista.Count(x => x.Estado == "ok");
        var sin = lista.Count(x => x.Estado is "sin-demanda" or "sin-proveedor");

        var texto = f.Buscar?.Trim().ToLowerInvariant();
        IEnumerable<ReposicionInsumoResponse> q = lista;
        if (!string.IsNullOrEmpty(texto)) q = q.Where(x => x.Codigo.ToLowerInvariant().Contains(texto) || x.Nombre.ToLowerInvariant().Contains(texto));
        if (f.CategoriaId is int cat) q = q.Where(x => x.CategoriaId == cat);
        q = f.Estado switch
        {
            "urgente" => q.Where(x => x.Estado == "urgente"),
            "pedir" => q.Where(x => x.Estado is "urgente" or "pedir"),
            "ok" => q.Where(x => x.Estado == "ok"),
            "sin" => q.Where(x => x.Estado is "sin-demanda" or "sin-proveedor"),
            _ => q
        };
        var filtrada = q.OrderBy(x => Prioridad.GetValueOrDefault(x.Estado, 9))
            .ThenBy(x => x.FechaLimitePedido ?? DateOnly.MaxValue).ThenBy(x => x.Codigo).ToList();

        var paginas = Math.Max(1, (int)Math.Ceiling(filtrada.Count / (double)f.TamanoPagina));
        var pagina = Math.Min(Math.Max(1, f.Pagina), paginas);
        return new GenericResponse<ReposicionPaginaResponse>(new ReposicionPaginaResponse
        {
            Items = filtrada.Skip((pagina - 1) * f.TamanoPagina).Take(f.TamanoPagina).ToList(),
            Total = filtrada.Count, Pagina = pagina, TamanoPagina = f.TamanoPagina,
            Urgentes = urgentes, PorPedir = porPedir, Ok = ok, SinDatos = sin, PronosticoDel = fecha, DiasRevision = revision
        });
    }

    public async Task<GenericResponse<ReposicionInsumoResponse>> DetalleAsync(int insumoId)
    {
        var (lista, _, _) = await CalcularAsync();
        return new(lista.FirstOrDefault(x => x.InsumoId == insumoId) ?? throw GenericException.NoEncontrado("El insumo no existe o está inactivo."));
    }

    public async Task<List<ReposicionInsumoResponse>> CalcularTodoAsync() => (await CalcularAsync()).Lista;

    private async Task<(List<ReposicionInsumoResponse> Lista, DateTime? Fecha, int Revision)> CalcularAsync()
    {
        var config = await repo.ConfiguracionAsync();
        var hoy = Reloj.Hoy;
        var insumos = await repo.InsumosReposicionAsync();
        var enCamino = await pedidos.EnCaminoPorInsumoAsync();
        var historico = await repo.ConsumoHistoricoDiarioAsync(90, Reloj.Ahora.AddDays(-90));

        // Último pronóstico exitoso: semanas futuras por producto.
        var ejecucion = await repo.UltimaEjecucionAsync(soloExitosas: true);
        var futuras = new Dictionary<int, List<PronosticoSemana>>();
        if (ejecucion is not null)
        {
            foreach (var r in await repo.ResultadosAsync(ejecucion.Id))
            {
                var semanas = r.Semanas.Where(s => !s.EsValidacion && s.Semana >= PrediccionService.Lunes(hoy)).OrderBy(s => s.Semana).ToList();
                if (semanas.Count > 0 && semanas.Any(s => s.Paquetes > 0)) futuras[r.ProductoId] = semanas;
            }
        }

        // Consumo por paquete de cada insumo según la receta vigente hoy (incluye merma).
        var coeficientes = new Dictionary<int, List<(int ProductoId, string Codigo, decimal PorPaquete)>>();
        foreach (var productoId in futuras.Keys)
        {
            try
            {
                var ex = (await recetas.ExplotarAsync(productoId, new ExplosionRequest { Cantidad = 1, Unidad = UnidadExplosion.Paquetes, Fecha = hoy })).Data!;
                var codigo = (await productos.ObtenerAsync(productoId))?.Codigo ?? productoId.ToString();
                foreach (var i in ex.Insumos)
                {
                    if (!coeficientes.TryGetValue(i.InsumoId, out var l)) coeficientes[i.InsumoId] = l = [];
                    l.Add((productoId, codigo, i.ConsumoTotal));
                }
            }
            catch (GenericException) { /* producto sin receta vigente: no aporta consumo */ }
        }

        var lista = insumos.Select(i => Calcular(i, config, hoy, enCamino.GetValueOrDefault(i.Id), historico.GetValueOrDefault(i.Id),
            coeficientes.GetValueOrDefault(i.Id), futuras)).ToList();
        return (lista, ejecucion?.Fecha, config.DiasRevision);
    }

    /// <summary>Cálculo de un insumo (interno: lo usan también las pruebas automáticas).</summary>
    internal static ReposicionInsumoResponse Calcular(
        InsumoReposicionDatos i, ConfiguracionEmpresa config, DateOnly hoy, decimal enCamino, decimal historico,
        List<(int ProductoId, string Codigo, decimal PorPaquete)>? coeficientes, Dictionary<int, List<PronosticoSemana>> futuras)
    {
        var c = i.Condicion;
        var L = c?.TiempoEntregaDias ?? config.DiasEntregaPorDefecto;
        var sigmaL = (double)(c?.VariacionEntregaDias ?? 0);
        var R = config.DiasRevision;
        var semanas = Math.Max(1, (int)Math.Ceiling((L + R) / 7.0));

        var r = new ReposicionInsumoResponse
        {
            InsumoId = i.Id, Codigo = i.Codigo, Nombre = i.Nombre, CategoriaNombre = i.CategoriaNombre, CategoriaId = i.CategoriaId,
            UnidadConsumoCodigo = i.UnidadConsumoCodigo, UnidadCompraCodigo = i.UnidadCompraCodigo, FactorConversion = i.FactorConversion,
            EsCritico = i.EsCritico, Stock = Math.Round(i.Stock, 4), EnCamino = Math.Round(enCamino, 4),
            TiempoEntrega = L, VariacionEntrega = (int)sigmaL, NivelServicio = i.NivelServicio,
            StockMinimoActual = i.StockMinimo, StockMaximo = i.StockMaximo,
            ProveedorId = c?.ProveedorId, ProveedorNombre = c?.ProveedorNombre,
            PedidoMinimo = c?.PedidoMinimo ?? 0, MultiploCompra = c?.MultiploCompra ?? 0, PrecioUnitario = c?.PrecioUnitario ?? 0,
            Simbolo = c is null ? null : EtiquetasCatalogo.Simbolo(c.Moneda)
        };

        // ---- demanda diaria
        double d = 0, sigma = 0;
        if (i.TipoConsumo == TipoConsumo.PorProduccion && coeficientes is { Count: > 0 })
        {
            double varianza = 0;
            foreach (var (productoId, codigo, porPaquete) in coeficientes)
            {
                var sem = futuras[productoId].Take(semanas).ToList();
                var paquetesDia = (double)sem.Average(s => s.Paquetes) / 7;
                var sigmaSemana = (double)sem.Average(s => (s.Maximo - s.Minimo) / 2) / MetodosPronostico.Z90;
                var sigmaDia = sigmaSemana / Math.Sqrt(7);
                d += (double)porPaquete * paquetesDia;
                varianza += Math.Pow((double)porPaquete * sigmaDia, 2);
                r.Productos.Add(new ConsumoProductoResponse
                {
                    Codigo = codigo, PaquetesDiarios = Math.Round((decimal)paquetesDia, 2), ConsumoPorPaquete = porPaquete,
                    ConsumoDiario = Math.Round((decimal)((double)porPaquete * paquetesDia), 4)
                });
            }
            sigma = Math.Sqrt(varianza);
            r.FuenteDemanda = $"Pronóstico de ventas × receta ({coeficientes.Count} producto{(coeficientes.Count == 1 ? "" : "s")})";
        }
        if (d <= 0 && historico > 0)
        {
            d = (double)historico;
            sigma = d * 0.25;
            r.FuenteDemanda = "Consumo real de los últimos 90 días";
        }
        if (d <= 0 && i.ConsumoEstimadoDiario is > 0m)
        {
            d = (double)i.ConsumoEstimadoDiario.Value;
            sigma = d * 0.25;
            r.FuenteDemanda = "Consumo estimado registrado en el insumo";
        }
        if (d <= 0) r.FuenteDemanda = "Sin demanda prevista";

        r.ConsumoDiario = Math.Round((decimal)d, 4);
        r.DesviacionDiaria = Math.Round((decimal)sigma, 4);

        // ---- política
        var z = MetodosPronostico.ZDeNivelServicio((double)i.NivelServicio / 100);
        var ss = z * Math.Sqrt(L * sigma * sigma + d * d * sigmaL * sigmaL);
        var rop = d * L + ss;
        r.Z = Math.Round((decimal)z, 3);
        r.StockSeguridad = Math.Round((decimal)ss, 2);
        r.PuntoReorden = Math.Round((decimal)rop, 2);

        if (d <= 0)
        {
            r.Estado = "sin-demanda";
            r.EstadoTexto = "Sin demanda prevista";
            return r;
        }

        var stock = (double)i.Stock;
        var posicion = stock + (double)enCamino;
        r.DiasCobertura = Math.Round((decimal)(stock / d), 1);
        r.FechaAgotamiento = hoy.AddDays((int)Math.Min(3650, Math.Floor(stock / d)));

        if (posicion < d * L) { r.Estado = "urgente"; r.EstadoTexto = "Se agotará antes de que llegue un pedido"; }
        else if (posicion <= rop) { r.Estado = "pedir"; r.EstadoTexto = "Pedir ahora"; }
        else { r.Estado = "ok"; r.EstadoTexto = "Cubierto"; }

        if (r.Estado == "ok")
        {
            r.FechaLimitePedido = hoy.AddDays((int)Math.Min(3650, Math.Floor((posicion - rop) / d)));
        }
        else
        {
            r.FechaLimitePedido = hoy;
            var objetivo = rop + d * R;
            if (i.CapacidadMaxima is > 0m) objetivo = Math.Min(objetivo, (double)i.CapacidadMaxima.Value);
            var necesario = Math.Max(0, objetivo - posicion);
            var factor = (double)(i.FactorConversion > 0 ? i.FactorConversion : 1);
            var enCompra = necesario / factor;
            var multiplo = (double)(c?.MultiploCompra ?? 0);
            enCompra = multiplo > 0 ? Math.Ceiling(enCompra / multiplo - 1e-9) * multiplo : Math.Ceiling(enCompra - 1e-9);
            enCompra = Math.Max(enCompra, (double)(c?.PedidoMinimo ?? 0));
            r.CantidadSugerida = Math.Round((decimal)enCompra, 4);
            r.CantidadSugeridaConsumo = Math.Round((decimal)(enCompra * factor), 4);
            if (c is not null) r.CostoEstimado = Math.Round(r.CantidadSugerida * c.PrecioUnitario, 2);
        }

        if (c is null)
        {
            r.Estado = r.Estado == "ok" ? "ok" : "sin-proveedor";
            if (r.Estado == "sin-proveedor") r.EstadoTexto = "Hay que pedir, pero no tiene proveedor asignado";
        }
        return r;
    }
}

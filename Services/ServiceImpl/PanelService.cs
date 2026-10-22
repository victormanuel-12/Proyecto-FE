using SPIn.Common;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;

namespace SPIn.Services.ServiceImpl;

/// <summary>
/// Panel de inicio (Entrega 9): lo que requiere atención hoy y el estado de la cadena
/// ventas → producto terminado → producción → insumos → pedidos, con los mismos cálculos que el resto del sistema.
/// </summary>
public class PanelService(
    IAlertaService alertas,
    IAlertaRepository alertasRepo,
    IReposicionService reposicion,
    IProductoTerminadoService productoTerminado,
    IPrediccionRepository prediccion,
    IProduccionOperacionRepository produccion,
    IPedidoRepository pedidos,
    IVentaRepository ventas,
    ILogger<PanelService> logger) : IPanelService
{
    public async Task<GenericResponse<PanelResponse>> PanelAsync()
    {
        var hoy = Reloj.Hoy;
        var lunes = hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7));
        try { await alertas.SincronizarAsync(); }
        catch (Exception ex) { logger.LogError(ex, "No se pudieron revisar las alertas para el panel"); }

        var panel = new PanelResponse
        {
            Generado = Reloj.Ahora,
            Alertas = await alertasRepo.ResumenAsync(),
            Principales = await alertasRepo.PrincipalesAsync(6)
        };

        // ---- insumos: los de menor cobertura (con demanda)
        var repo = await reposicion.CalcularTodoAsync();
        panel.Insumos = repo.Where(r => r.ConsumoDiario > 0 && r.DiasCobertura is not null)
            .OrderBy(r => r.Estado switch { "urgente" => 0, "pedir" => 1, "sin-proveedor" => 2, _ => 3 })
            .ThenBy(r => r.DiasCobertura)
            .Take(8)
            .Select(r => new PanelInsumoResponse
            {
                InsumoId = r.InsumoId, Codigo = r.Codigo, Nombre = r.Nombre, EsCritico = r.EsCritico, DiasCobertura = r.DiasCobertura,
                TiempoEntrega = r.TiempoEntrega, Estado = r.Estado, CantidadSugerida = r.CantidadSugerida, UnidadCompraCodigo = r.UnidadCompraCodigo
            }).ToList();

        // ---- productos: stock PT, pronóstico, producción de la semana
        var pt = (await productoTerminado.StockAsync(new FiltroStockProductoRequest { Pagina = 1, TamanoPagina = 1000 })).Data!.Items;
        var ejecucion = await prediccion.UltimaEjecucionAsync(soloExitosas: true);
        var ultima = await prediccion.UltimaEjecucionAsync(soloExitosas: false);
        panel.PronosticoDel = ejecucion?.Fecha;
        panel.MapePromedio = ejecucion?.MapePromedio;
        panel.PronosticoExitoso = ultima?.Exitosa ?? false;
        List<PronosticoProducto> pronosticos = ejecucion is null ? new List<PronosticoProducto>() : await prediccion.ResultadosAsync(ejecucion.Id);

        var partes = (await produccion.PartesAsync(new FiltroParteRequest { Desde = lunes, Hasta = hoy, TamanoPagina = 100 })).Items;
        var planes = (await produccion.PlanesAsync(new FiltroPlanRequest { Desde = lunes, Hasta = lunes.AddDays(6), Estado = EstadoPlan.Planificado, TamanoPagina = 100 }, hoy)).Items;
        panel.ProducidoSemana = partes.Sum(p => p.Paquetes);
        panel.PartesSemana = partes.Count;
        panel.ProgramadoPendienteSemana = planes.Sum(p => p.Paquetes);

        panel.Productos = pt.Select(p =>
        {
            var f = pronosticos.FirstOrDefault(x => x.ProductoId == p.ProductoId);
            var futuras = f?.Semanas.Where(s => !s.EsValidacion && s.Semana >= lunes).OrderBy(s => s.Semana).Take(4).ToList() ?? new List<PronosticoSemana>();
            var proximas = futuras.Sum(s => s.Paquetes);
            var referencia = (f?.PromedioReciente ?? 0) * futuras.Count;
            return new PanelProductoResponse
            {
                ProductoId = p.ProductoId, Codigo = p.Codigo, Nombre = p.Nombre, StockPaquetes = p.Paquetes,
                VentaDiaria = Math.Round(p.VentaDiariaPromedio, 1), DiasDeVenta = p.DiasDeVenta,
                PromedioSemanal = f?.PromedioReciente ?? 0, Proximas4 = Math.Round(proximas, 0),
                Variacion = referencia > 0 ? Math.Round((proximas - referencia) / referencia * 100, 1) : null,
                Mape = f?.Mape, Calidad = PrediccionService.Calidad(f?.Mape),
                ProducidoSemana = partes.Where(x => x.ProductoId == p.ProductoId).Sum(x => x.Paquetes),
                ProgramadoSemana = planes.Where(x => x.ProductoId == p.ProductoId).Sum(x => x.Paquetes)
            };
        }).OrderBy(p => p.DiasDeVenta ?? decimal.MaxValue).ThenBy(p => p.Codigo).ToList();

        // ---- pedidos en camino: los próximos en llegar
        var enCamino = await pedidos.ListarAsync(new FiltroPedidoRequest { Estado = "encamino", TamanoPagina = 5 }, hoy);
        panel.PedidosEnCamino = enCamino.Resumen.EnCamino;
        panel.PedidosAtrasados = enCamino.Resumen.Atrasados;
        panel.Pedidos = enCamino.Items.Select(p => new PanelPedidoResponse
        {
            Id = p.Id, Numero = p.Numero, ProveedorNombre = p.ProveedorNombre, FechaEstimada = p.FechaEstimada, DiasAtraso = p.DiasAtraso,
            Insumos = string.Join(", ", p.Detalles.Select(d => d.InsumoCodigo)), Avance = p.Avance
        }).ToList();

        // ---- ventas: últimos 7 días frente a los 7 anteriores
        var diarias = await ventas.VentasDiariasAsync(null, hoy.AddDays(-13), hoy);
        panel.VentasUltimos7 = diarias.Where(d => d.Fecha > hoy.AddDays(-7)).Sum(d => d.Paquetes);
        panel.VentasPrevios7 = diarias.Where(d => d.Fecha <= hoy.AddDays(-7)).Sum(d => d.Paquetes);

        return new GenericResponse<PanelResponse>(panel);
    }
}

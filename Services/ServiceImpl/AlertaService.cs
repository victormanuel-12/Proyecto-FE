using Microsoft.Extensions.Caching.Memory;
using SPIn.Common;
using SPIn.Common.Excepciones;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;
using SPIn.Repository.RepositoryImpl;

namespace SPIn.Services.ServiceImpl;

/// <summary>
/// Alertas (Entrega 9). En cada revisión se evalúan las reglas sobre el estado actual:
/// reposición (urgente, por pedir, bajo mínimo, sin proveedor, sobre máximo), pedidos (atrasados, por llegar),
/// producto terminado (cobertura &lt; 3 días), programación vencida, pronóstico (desactualizado, fallido, error alto)
/// y conteos abiertos hace más de 7 días.
/// Cada condición tiene una clave estable: si sigue presente se actualiza; si desaparece, la alerta se resuelve sola.
/// La revisión se hace como máximo una vez por minuto por empresa (y cada 10 minutos en segundo plano).
/// </summary>
public class AlertaService(
    IAlertaRepository repo,
    IReposicionService reposicion,
    IPedidoRepository pedidos,
    IProductoTerminadoService productoTerminado,
    IProduccionOperacionRepository produccion,
    IPrediccionRepository prediccion,
    IMemoryCache cache,
    ITenantProvider tenant,
    ILogger<AlertaService> logger) : IAlertaService
{
    private sealed record Deteccion(string Clave, TipoAlerta Tipo, SeveridadAlerta Severidad, string Titulo, string Detalle, string? Enlace, string? TextoEnlace);

    public async Task SincronizarAsync(bool forzar = false)
    {
        if (tenant.EmpresaId is not int empresaId) return;
        var clave = $"spin:alertas:{empresaId}";
        if (!forzar && cache.TryGetValue(clave, out _)) return;
        cache.Set(clave, true, TimeSpan.FromSeconds(60));

        var detectadas = await DetectarAsync();
        var ahora = Reloj.Ahora;
        var abiertas = (await repo.NoResueltasAsync()).GroupBy(a => a.Clave).ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.Id).First());

        foreach (var d in detectadas.GroupBy(x => x.Clave).Select(g => g.OrderBy(x => x.Severidad).First()))
        {
            if (abiertas.Remove(d.Clave, out var a))
            {
                // Si estaba descartada y la situación empeoró, vuelve a estar activa.
                if (a.Estado == EstadoAlerta.Descartada && d.Severidad < a.Severidad)
                {
                    a.Estado = EstadoAlerta.Activa;
                    a.DescartadaEn = null;
                    a.DescartadaPor = null;
                    a.MotivoDescarte = null;
                }
                a.Tipo = d.Tipo;
                a.Severidad = d.Severidad;
                a.Titulo = Recortar(d.Titulo, 200);
                a.Detalle = Recortar(d.Detalle, 500);
                a.Enlace = d.Enlace;
                a.TextoEnlace = d.TextoEnlace;
                a.UltimaVez = ahora;
            }
            else
            {
                repo.Agregar(new Alerta
                {
                    Clave = d.Clave, Tipo = d.Tipo, Severidad = d.Severidad, Titulo = Recortar(d.Titulo, 200), Detalle = Recortar(d.Detalle, 500),
                    Enlace = d.Enlace, TextoEnlace = d.TextoEnlace, PrimeraVez = ahora, UltimaVez = ahora
                });
            }
        }

        // Lo que ya no se detecta, se resolvió.
        foreach (var a in abiertas.Values)
        {
            a.Estado = EstadoAlerta.Resuelta;
            a.ResueltaEn = ahora;
        }
        await repo.GuardarAsync();
        await repo.PodarResueltasAsync(ahora.AddDays(-90));
        logger.LogDebug("Alertas sincronizadas: {Cantidad} condiciones detectadas", detectadas.Count);
    }

    private async Task<List<Deteccion>> DetectarAsync()
    {
        var hoy = Reloj.Hoy;
        var lista = new List<Deteccion>();
        static string N(decimal v) => v.ToString("#,0.##", System.Globalization.CultureInfo.GetCultureInfo("es-PE"));

        // ---------------------------------------------------------------- insumos (reposición)
        foreach (var r in await reposicion.CalcularTodoAsync())
        {
            var u = r.UnidadConsumoCodigo;
            var enlace = $"/reposicion?buscar={Uri.EscapeDataString(r.Codigo)}";
            switch (r.Estado)
            {
                case "urgente":
                    lista.Add(new($"insumo-reposicion:{r.InsumoId}", TipoAlerta.InsumoUrgente, SeveridadAlerta.Critica,
                        $"{r.Codigo} se agotará antes de que llegue un pedido",
                        $"Stock {N(r.Stock)} {u} ({N(r.DiasCobertura ?? 0)} días, hasta el {r.FechaAgotamiento:dd/MM/yyyy}); el proveedor tarda {r.TiempoEntrega} días. Sugerido: {N(r.CantidadSugerida)} {r.UnidadCompraCodigo}.",
                        enlace, "Ver reposición"));
                    break;
                case "pedir":
                    lista.Add(new($"insumo-reposicion:{r.InsumoId}", TipoAlerta.InsumoPorPedir, SeveridadAlerta.Alta,
                        $"Pedir {r.Codigo}: está bajo el punto de reorden",
                        $"Stock + en camino {N(r.Posicion)} {u} ≤ punto de reorden {N(r.PuntoReorden)} {u}. Sugerido: {N(r.CantidadSugerida)} {r.UnidadCompraCodigo} a {r.ProveedorNombre}.",
                        enlace, "Ver reposición"));
                    break;
                case "sin-proveedor":
                    lista.Add(new($"insumo-reposicion:{r.InsumoId}", TipoAlerta.InsumoSinProveedor, SeveridadAlerta.Alta,
                        $"{r.Codigo} necesita reposición y no tiene proveedor",
                        $"Stock {N(r.Stock)} {u} bajo el punto de reorden {N(r.PuntoReorden)} {u}. Asigne un proveedor con sus condiciones de compra.",
                        $"/insumos?buscar={Uri.EscapeDataString(r.Codigo)}", "Ir a insumos"));
                    break;
            }
            // Stock mínimo registrado (independiente del pronóstico), solo si no hay ya alerta de reposición.
            if (r.Estado is "ok" or "sin-demanda" && r.StockMinimoActual > 0 && r.Stock < r.StockMinimoActual)
            {
                lista.Add(new($"insumo-minimo:{r.InsumoId}", TipoAlerta.InsumoBajoMinimo, r.Stock <= 0 ? SeveridadAlerta.Alta : SeveridadAlerta.Media,
                    r.Stock <= 0 ? $"{r.Codigo} sin stock" : $"{r.Codigo} bajo el stock mínimo",
                    $"Stock {N(r.Stock)} {u}; mínimo registrado {N(r.StockMinimoActual)} {u}." +
                    (r.EnCamino > 0 ? $" En camino: {N(r.EnCamino)} {u}." : "") +
                    (r.Estado == "ok" ? " Con el consumo pronosticado aún está cubierto: revise si el mínimo sigue vigente." : ""),
                    $"/stock?buscar={Uri.EscapeDataString(r.Codigo)}", "Ver stock"));
            }
            if (r.StockMaximo > 0 && r.Stock > r.StockMaximo * 1.1m)
            {
                lista.Add(new($"insumo-maximo:{r.InsumoId}", TipoAlerta.InsumoSobreMaximo, SeveridadAlerta.Informativa,
                    $"{r.Codigo} sobre el stock máximo",
                    $"Stock {N(r.Stock)} {u}; máximo {N(r.StockMaximo)} {u}. Capital inmovilizado y espacio ocupado.",
                    $"/stock?buscar={Uri.EscapeDataString(r.Codigo)}", "Ver stock"));
            }
        }

        // ---------------------------------------------------------------- pedidos
        var enCamino = await pedidos.ListarAsync(new FiltroPedidoRequest { Estado = "encamino", TamanoPagina = 100 }, hoy);
        foreach (var p in enCamino.Items)
        {
            var insumos = string.Join(", ", p.Detalles.Select(d => d.InsumoCodigo));
            if (p.DiasAtraso > 0)
            {
                lista.Add(new($"pedido-atrasado:{p.Id}", TipoAlerta.PedidoAtrasado, p.DiasAtraso >= 3 ? SeveridadAlerta.Alta : SeveridadAlerta.Media,
                    $"Pedido {p.Numero} atrasado {p.DiasAtraso} día(s)",
                    $"{p.ProveedorNombre} debía entregar el {p.FechaEstimada:dd/MM/yyyy}: {insumos}. Recibido {p.Avance:0.#} %.",
                    "/pedidos?estado=atrasados", "Ver pedidos"));
            }
            else if (p.FechaEstimada.DayNumber - hoy.DayNumber <= 2)
            {
                var cuando = p.FechaEstimada == hoy ? "hoy" : p.FechaEstimada == hoy.AddDays(1) ? "mañana" : $"el {p.FechaEstimada:dd/MM}";
                lista.Add(new($"pedido-llega:{p.Id}", TipoAlerta.PedidoPorLlegar, SeveridadAlerta.Informativa,
                    $"Pedido {p.Numero} llega {cuando}",
                    $"{p.ProveedorNombre}: {insumos}. Prepare la recepción y el espacio en almacén.",
                    "/pedidos?estado=encamino", "Ver pedidos"));
            }
        }

        // ---------------------------------------------------------------- producto terminado
        var pt = await productoTerminado.StockAsync(new FiltroStockProductoRequest { Pagina = 1, TamanoPagina = 1000 });
        foreach (var p in pt.Data!.Items.Where(p => p.VentaDiariaPromedio > 0 && (p.DiasDeVenta ?? 0) < 3))
        {
            lista.Add(new($"pt-bajo:{p.ProductoId}", TipoAlerta.ProductoTerminadoBajo, p.Paquetes <= 0 ? SeveridadAlerta.Critica : SeveridadAlerta.Alta,
                p.Paquetes <= 0 ? $"{p.Codigo} sin stock para despachar" : $"{p.Codigo}: producto terminado para {p.DiasDeVenta:0.#} días",
                $"Stock {N(p.Paquetes)} paquetes; se venden {N(Math.Round(p.VentaDiariaPromedio, 1))} por día (promedio de 28 días). Programe producción.",
                "/produccion?vista=plan", "Programar producción"));
        }

        // ---------------------------------------------------------------- programación vencida
        var vencidas = await produccion.PlanesAsync(new FiltroPlanRequest { Estado = EstadoPlan.Planificado, Hasta = hoy.AddDays(-1), TamanoPagina = 100 }, hoy);
        foreach (var p in vencidas.Items)
        {
            lista.Add(new($"plan-vencido:{p.Id}", TipoAlerta.ProgramacionVencida, SeveridadAlerta.Media,
                $"Programación del {p.Fecha:dd/MM} sin parte: {p.ProductoCodigo}",
                $"{N(p.Paquetes)} paquetes en {p.LineaCodigo}. Registre el parte de lo producido o cancele la programación.",
                "/produccion?vista=plan", "Ver programación"));
        }

        // ---------------------------------------------------------------- pronóstico
        var hayVentas = (await prediccion.VentasAsync()).Count > 0;
        var ultima = await prediccion.UltimaEjecucionAsync(soloExitosas: false);
        var exitosa = ultima is { Exitosa: true } ? ultima : await prediccion.UltimaEjecucionAsync(soloExitosas: true);
        if (hayVentas && exitosa is null)
        {
            lista.Add(new("pronostico-estado", TipoAlerta.PronosticoDesactualizado, SeveridadAlerta.Media,
                "Aún no hay pronóstico de demanda", "Hay ventas registradas pero el motor no se ha entrenado. La reposición usa el consumo histórico.",
                "/pronostico", "Ir al pronóstico"));
        }
        else if (ultima is { Exitosa: false })
        {
            lista.Add(new("pronostico-estado", TipoAlerta.PronosticoDesactualizado, SeveridadAlerta.Alta,
                "El último entrenamiento del pronóstico falló", ultima.Mensaje ?? "Revise el historial de entrenamientos.",
                "/pronostico", "Ir al pronóstico"));
        }
        else if (exitosa is not null && exitosa.Fecha < Reloj.Ahora.AddDays(-8))
        {
            lista.Add(new("pronostico-estado", TipoAlerta.PronosticoDesactualizado, SeveridadAlerta.Media,
                "El pronóstico tiene más de una semana",
                $"Último entrenamiento: {exitosa.Fecha:dd/MM/yyyy HH:mm}. Revise la frecuencia en Configuración o recalcule.",
                "/pronostico", "Ir al pronóstico"));
        }
        if (exitosa is not null)
        {
            foreach (var r in (await prediccion.ResultadosAsync(exitosa.Id)).Where(r => r.Mape > 30))
            {
                lista.Add(new($"pronostico-error:{r.ProductoId}", TipoAlerta.PronosticoErrorAlto, SeveridadAlerta.Informativa,
                    $"Pronóstico poco confiable para {r.Producto?.Codigo}",
                    $"Error (MAPE) de {r.Mape:0.#} % en la validación. Revise ventas atípicas o espere más historia; amplíe el stock de seguridad si es crítico.",
                    "/pronostico", "Ver pronóstico"));
            }
        }

        // ---------------------------------------------------------------- conteos
        foreach (var c in (await repo.ConteosAbiertosAsync()).Where(c => c.CreadoEn < Reloj.Ahora.AddDays(-7)))
        {
            lista.Add(new($"conteo-abierto:{c.Id}", TipoAlerta.ConteoAbierto, SeveridadAlerta.Informativa,
                $"Conteo N° {c.Numero} abierto desde el {c.CreadoEn:dd/MM}",
                $"Almacén {c.AlmacenCodigo}. Mientras siga abierto, sus diferencias no se ajustan en el kardex.",
                $"/conteos/{c.Id}", "Ir al conteo"));
        }
        return lista;
    }

    /// <summary>Una falla al revisar no debe impedir ver las alertas ya registradas.</summary>
    private async Task SincronizarSeguroAsync()
    {
        try { await SincronizarAsync(); }
        catch (Exception ex) { logger.LogError(ex, "No se pudieron revisar las alertas"); }
    }

    public async Task<GenericResponse<AlertasPaginaResponse>> ListarAsync(FiltroAlertaRequest filtro)
    {
        await SincronizarSeguroAsync();
        var pagina = await repo.ListarAsync(filtro, Reloj.Ahora.AddDays(-30));
        return new GenericResponse<AlertasPaginaResponse>(new AlertasPaginaResponse
        {
            Items = pagina.Items, Total = pagina.Total, Pagina = pagina.Pagina, TamanoPagina = pagina.TamanoPagina,
            Resumen = await repo.ResumenAsync(), Revisado = Reloj.Ahora
        });
    }

    public async Task<GenericResponse<ResumenAlertasResponse>> ResumenAsync()
    {
        await SincronizarSeguroAsync();
        return new(await repo.ResumenAsync());
    }

    public async Task<GenericResponse<AlertaResponse>> DescartarAsync(int id, DescartarAlertaRequest request)
    {
        var a = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("La alerta no existe.");
        if (a.Estado != EstadoAlerta.Activa) throw GenericException.Conflicto("Solo se descarta una alerta activa.");
        a.Estado = EstadoAlerta.Descartada;
        a.DescartadaEn = Reloj.Ahora;
        a.DescartadaPor = tenant.UsuarioNombre;
        a.MotivoDescarte = Validaciones.TextoOpcional(request.Motivo);
        await repo.GuardarAsync();
        return new GenericResponse<AlertaResponse>(true, AlertaRepositoryImpl.Mapear(a),
            "Alerta descartada. Se cerrará sola cuando se resuelva y volverá si la situación empeora.");
    }

    public async Task<GenericResponse<AlertaResponse>> ReactivarAsync(int id)
    {
        var a = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("La alerta no existe.");
        if (a.Estado != EstadoAlerta.Descartada) throw GenericException.Conflicto("Solo se reactiva una alerta descartada.");
        a.Estado = EstadoAlerta.Activa;
        a.DescartadaEn = null;
        a.DescartadaPor = null;
        a.MotivoDescarte = null;
        await repo.GuardarAsync();
        return new GenericResponse<AlertaResponse>(true, AlertaRepositoryImpl.Mapear(a), "Alerta reactivada.");
    }

    private static string Recortar(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";
}

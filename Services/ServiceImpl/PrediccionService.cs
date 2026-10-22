using System.Collections.Concurrent;
using System.Diagnostics;
using SPIn.Common;
using SPIn.Common.Excepciones;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;
using SPIn.Services.Prediccion;

namespace SPIn.Services.ServiceImpl;

/// <summary>
/// Motor predictivo (Entrega 8).
/// 1. Arma la serie semanal (lunes a domingo) de paquetes vendidos por producto, sin la semana en curso.
/// 2. Para cada producto compara SSA (ML.NET), Holt, promedio móvil y estacional con las últimas semanas reales
///    (validación) y elige el de menor MAPE.
/// 3. Guarda el pronóstico de las próximas semanas con su banda del 90 %, y el historial de entrenamientos.
/// La reposición de insumos (ReposicionService) usa siempre el último pronóstico exitoso con el stock del momento.
/// </summary>
public class PrediccionService(
    IPrediccionRepository repo,
    ITenantProvider tenant,
    ILogger<PrediccionService> logger) : IPrediccionService
{
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> Candados = new();
    private const int EjecucionesConservadas = 30;

    public static string NombreMetodo(MetodoPronostico m) => m switch
    {
        MetodoPronostico.Ssa => "SSA (ML.NET)",
        MetodoPronostico.Holt => "Holt (tendencia)",
        MetodoPronostico.PromedioMovil => "Promedio móvil 8 semanas",
        MetodoPronostico.Estacional => "Estacional (año anterior)",
        _ => "Sin historia suficiente"
    };

    public static string NombreOrigen(OrigenEjecucion o) => o switch
    {
        OrigenEjecucion.Manual => "Manual",
        OrigenEjecucion.Programado => "Programado",
        OrigenEjecucion.AlIniciar => "Al iniciar el sistema",
        OrigenEjecucion.Importacion => "Tras importar ventas",
        _ => o.ToString()
    };

    public static DateOnly Lunes(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7));

    // ================================================================ entrenamiento
    public async Task<GenericResponse<EjecucionResponse>> EntrenarAsync(OrigenEjecucion origen)
    {
        var empresaId = tenant.EmpresaId ?? throw GenericException.Prohibido("Su usuario no tiene una empresa asignada.");
        var candado = Candados.GetOrAdd(empresaId, _ => new SemaphoreSlim(1, 1));
        if (!await candado.WaitAsync(0)) throw GenericException.Conflicto("Ya hay un entrenamiento en curso. Espere unos segundos.");
        try
        {
            var e = await EntrenarInternoAsync(origen);
            return new GenericResponse<EjecucionResponse>(e.Exitosa, Mapear(e),
                e.Exitosa ? $"Pronóstico actualizado: {e.Productos} producto(s) en {e.DuracionMs / 1000.0:0.0} s." : $"El entrenamiento falló: {e.Mensaje}");
        }
        finally
        {
            candado.Release();
        }
    }

    private async Task<EjecucionPronostico> EntrenarInternoAsync(OrigenEjecucion origen)
    {
        var reloj = Stopwatch.StartNew();
        var config = await repo.ConfiguracionAsync();
        var hoy = Reloj.Hoy;
        var lunesActual = Lunes(hoy);
        var ultimaSemana = lunesActual.AddDays(-7);
        var horizonte = config.SemanasHorizonte;

        var ejecucion = new EjecucionPronostico
        {
            Fecha = Reloj.Ahora, Origen = origen, Usuario = tenant.UsuarioNombre ?? "sistema",
            UltimaSemanaHistorial = ultimaSemana, SemanasHorizonte = horizonte
        };

        try
        {
            var ventas = await repo.VentasAsync();
            var productos = await repo.ProductosAsync();
            var conVentas = ventas.Select(v => v.ProductoId).ToHashSet();
            var notasGenerales = new List<string>();

            foreach (var (id, codigo, _, activo) in productos)
            {
                if (!activo && !conVentas.Contains(id)) continue;
                var serie = SerieSemanal(ventas.Where(v => v.ProductoId == id), ultimaSemana);
                var r = MotorPronostico.Pronosticar(serie.Select(s => s.Paquetes).ToList(), horizonte, config.SemanasValidacion, config.SemanasMinimas);

                var reciente = serie.Count == 0 ? 0 : serie.Skip(Math.Max(0, serie.Count - 8)).Average(s => s.Paquetes);
                var resultado = new PronosticoProducto
                {
                    ProductoId = id,
                    Metodo = r.Metodo,
                    Mape = Dec(r.Mape),
                    MapeSsa = Dec(r.MapePorMetodo.GetValueOrDefault(MetodoPronostico.Ssa)),
                    MapeHolt = Dec(r.MapePorMetodo.GetValueOrDefault(MetodoPronostico.Holt)),
                    MapePromedio = Dec(r.MapePorMetodo.GetValueOrDefault(MetodoPronostico.PromedioMovil)),
                    MapeEstacional = Dec(r.MapePorMetodo.GetValueOrDefault(MetodoPronostico.Estacional)),
                    SemanasHistorial = serie.Count,
                    PromedioReciente = Math.Round((decimal)reciente, 2),
                    Nota = r.Notas.Count == 0 ? null : Recortar(string.Join(" ", r.Notas), 300)
                };

                // Validación: semanas reales reservadas y lo que pronosticó el método elegido.
                if (r.ValidacionPronostico is { } vp && r.ValidacionReal.Length > 0)
                {
                    var inicio = serie.Count - r.ValidacionReal.Length;
                    for (var i = 0; i < r.ValidacionReal.Length; i++)
                    {
                        resultado.Semanas.Add(new PronosticoSemana
                        {
                            Semana = serie[inicio + i].Semana, EsValidacion = true, Real = Dec2(r.ValidacionReal[i]),
                            Paquetes = Dec2(vp.Valores[i]), Minimo = Dec2(vp.Minimos[i]), Maximo = Dec2(vp.Maximos[i])
                        });
                    }
                }
                for (var h = 0; h < horizonte; h++)
                {
                    resultado.Semanas.Add(new PronosticoSemana
                    {
                        Semana = lunesActual.AddDays(7 * h),
                        Paquetes = Dec2(r.Pronostico.Valores[h]), Minimo = Dec2(r.Pronostico.Minimos[h]), Maximo = Dec2(r.Pronostico.Maximos[h])
                    });
                }
                ejecucion.Resultados.Add(resultado);
                if (r.Notas.Any(n => n.StartsWith("SSA")) && !notasGenerales.Any(n => n.StartsWith("SSA")))
                    notasGenerales.Add(r.Notas.First(n => n.StartsWith("SSA")) + " Se usaron los otros métodos.");
                logger.LogInformation("Pronóstico {Codigo}: {Metodo}, MAPE {Mape}", codigo, r.Metodo, r.Mape);
            }

            var medidos = ejecucion.Resultados.Where(x => x.Mape is not null && x.PromedioReciente > 0).ToList();
            ejecucion.MapePromedio = medidos.Count == 0 ? null
                : Math.Round(medidos.Sum(x => x.Mape!.Value * x.PromedioReciente) / medidos.Sum(x => x.PromedioReciente), 1);
            ejecucion.Productos = ejecucion.Resultados.Count;
            ejecucion.Exitosa = true;
            ejecucion.Mensaje = ejecucion.Productos == 0
                ? "No hay productos registrados."
                : notasGenerales.Count > 0 ? Recortar(string.Join(" ", notasGenerales), 1000) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falló el entrenamiento del pronóstico");
            ejecucion.Resultados.Clear();
            ejecucion.Exitosa = false;
            ejecucion.Mensaje = Recortar((ex.InnerException ?? ex).Message, 1000);
        }

        ejecucion.DuracionMs = (int)reloj.ElapsedMilliseconds;
        config.ReentrenarPendiente = false;
        repo.AgregarEjecucion(ejecucion);
        await repo.GuardarAsync();
        await repo.PodarAsync(EjecucionesConservadas);
        return ejecucion;
    }

    /// <summary>Serie semanal desde la semana de la primera venta hasta la última semana completa (las semanas sin venta valen 0).</summary>
    public static List<(DateOnly Semana, double Paquetes)> SerieSemanal(IEnumerable<(int ProductoId, DateOnly Fecha, decimal Paquetes)> ventas, DateOnly ultimaSemana)
    {
        var porSemana = ventas.Where(v => Lunes(v.Fecha) <= ultimaSemana)
            .GroupBy(v => Lunes(v.Fecha)).ToDictionary(g => g.Key, g => (double)g.Sum(x => x.Paquetes));
        if (porSemana.Count == 0) return [];
        var lista = new List<(DateOnly, double)>();
        for (var s = porSemana.Keys.Min(); s <= ultimaSemana; s = s.AddDays(7)) lista.Add((s, porSemana.GetValueOrDefault(s)));
        return lista;
    }

    public async Task<bool> EntrenarSiCorrespondeAsync(bool alIniciar)
    {
        var config = await repo.ConfiguracionAsync();
        if (config.Id == 0) return false;
        var ultima = await repo.UltimaEjecucionAsync(soloExitosas: false);
        var ahora = Reloj.Ahora;

        OrigenEjecucion? origen = null;
        if (config.ReentrenarPendiente) origen = OrigenEjecucion.Importacion;
        else if (config.Frecuencia != FrecuenciaReentrenamiento.Manual)
        {
            var marca = UltimaMarca(config, ahora);
            if (ultima is null || ultima.Fecha < marca)
                origen = alIniciar ? OrigenEjecucion.AlIniciar : OrigenEjecucion.Programado;
        }
        if (origen is null) return false;
        if ((await repo.ProductosAsync()).Count == 0) return false;

        await EntrenarAsync(origen.Value);
        return true;
    }

    /// <summary>Último momento programado que ya pasó (lunes o cada día a la hora configurada).</summary>
    public static DateTime UltimaMarca(ConfiguracionEmpresa c, DateTime ahora)
    {
        var hoy = DateOnly.FromDateTime(ahora);
        var dia = c.Frecuencia == FrecuenciaReentrenamiento.Semanal ? Lunes(hoy) : hoy;
        var marca = dia.ToDateTime(new TimeOnly(Math.Clamp(c.HoraReentrenamiento, 0, 23), 0));
        if (marca > ahora) marca = marca.AddDays(c.Frecuencia == FrecuenciaReentrenamiento.Semanal ? -7 : -1);
        return marca;
    }

    public static DateTime? ProximaMarca(ConfiguracionEmpresa c, DateTime ahora) =>
        c.Frecuencia == FrecuenciaReentrenamiento.Manual ? null
            : UltimaMarca(c, ahora).AddDays(c.Frecuencia == FrecuenciaReentrenamiento.Semanal ? 7 : 1);

    // ================================================================ consultas
    public async Task<GenericResponse<PanelPronosticoResponse>> PanelAsync()
    {
        var config = await repo.ConfiguracionAsync();
        var ultima = await repo.UltimaEjecucionAsync(soloExitosas: false);
        var exitosa = ultima is { Exitosa: true } ? ultima : await repo.UltimaEjecucionAsync(soloExitosas: true);
        var panel = new PanelPronosticoResponse
        {
            Ultima = ultima is null ? null : Mapear(ultima),
            Frecuencia = config.Frecuencia,
            ProximoEntrenamiento = ProximaMarca(config, Reloj.Ahora),
            EnCurso = tenant.EmpresaId is int e && Candados.TryGetValue(e, out var c) && c.CurrentCount == 0
        };
        if (exitosa is not null)
        {
            var resultados = await repo.ResultadosAsync(exitosa.Id);
            panel.Productos = resultados.OrderBy(r => r.Producto!.Codigo).Select(r => MapearProducto(r, Reloj.Hoy)).ToList();
        }
        return new GenericResponse<PanelPronosticoResponse>(panel);
    }

    public async Task<GenericResponse<DetallePronosticoResponse>> DetalleAsync(int productoId)
    {
        var exitosa = await repo.UltimaEjecucionAsync(soloExitosas: true) ?? throw GenericException.NoEncontrado("Aún no hay un pronóstico. Pulse «Recalcular pronóstico».");
        var r = await repo.ResultadoAsync(exitosa.Id, productoId) ?? throw GenericException.NoEncontrado("El producto no tiene pronóstico en el último entrenamiento.");
        var baseResp = MapearProducto(r, Reloj.Hoy);
        var d = new DetallePronosticoResponse
        {
            ProductoId = baseResp.ProductoId, Codigo = baseResp.Codigo, Nombre = baseResp.Nombre, Metodo = baseResp.Metodo,
            MetodoNombre = baseResp.MetodoNombre, Mape = baseResp.Mape, Calidad = baseResp.Calidad, SemanasHistorial = baseResp.SemanasHistorial,
            PromedioReciente = baseResp.PromedioReciente, Proximas4 = baseResp.Proximas4, Variacion = baseResp.Variacion, Nota = baseResp.Nota
        };

        (MetodoPronostico M, decimal? V)[] metodos =
            [(MetodoPronostico.Ssa, r.MapeSsa), (MetodoPronostico.Holt, r.MapeHolt), (MetodoPronostico.PromedioMovil, r.MapePromedio), (MetodoPronostico.Estacional, r.MapeEstacional)];
        d.Metodos = metodos.Select(x => new MapeMetodoResponse { Metodo = x.M, Nombre = NombreMetodo(x.M), Mape = x.V, Elegido = x.M == r.Metodo }).ToList();

        // Historia real de las últimas 52 semanas (hasta la última semana completa del entrenamiento).
        var ventas = (await repo.VentasAsync()).Where(v => v.ProductoId == productoId);
        var serie = SerieSemanal(ventas, exitosa.UltimaSemanaHistorial);
        d.Historial = serie.Skip(Math.Max(0, serie.Count - 52))
            .Select(s => new PuntoSemanaResponse { Semana = s.Semana, Real = Math.Round((decimal)s.Paquetes, 2) }).ToList();
        d.Validacion = r.Semanas.Where(s => s.EsValidacion).OrderBy(s => s.Semana)
            .Select(s => new PuntoSemanaResponse { Semana = s.Semana, Real = s.Real, Pronostico = s.Paquetes, Minimo = s.Minimo, Maximo = s.Maximo }).ToList();
        d.Pronostico = r.Semanas.Where(s => !s.EsValidacion).OrderBy(s => s.Semana)
            .Select(s => new PuntoSemanaResponse { Semana = s.Semana, Pronostico = s.Paquetes, Minimo = s.Minimo, Maximo = s.Maximo }).ToList();
        return new GenericResponse<DetallePronosticoResponse>(d);
    }

    public async Task<GenericResponse<List<EjecucionResponse>>> EjecucionesAsync() =>
        new((await repo.EjecucionesAsync(15)).Select(Mapear).ToList());

    // ================================================================ configuración
    public async Task<GenericResponse<ConfiguracionResponse>> ConfiguracionAsync() => new(MapearConfig(await repo.ConfiguracionAsync()));

    public async Task<GenericResponse<ConfiguracionResponse>> GuardarConfiguracionAsync(ConfiguracionRequest r)
    {
        var c = await repo.ConfiguracionAsync();
        if (c.Id == 0) throw GenericException.Prohibido("Su usuario no tiene una empresa asignada.");
        var cambioModelo = c.SemanasHorizonte != r.SemanasHorizonte || c.SemanasValidacion != r.SemanasValidacion || c.SemanasMinimas != r.SemanasMinimas;
        c.SemanasHorizonte = r.SemanasHorizonte;
        c.SemanasValidacion = r.SemanasValidacion;
        c.SemanasMinimas = r.SemanasMinimas;
        c.Frecuencia = r.Frecuencia;
        c.HoraReentrenamiento = r.HoraReentrenamiento;
        c.DiasRevision = r.DiasRevision;
        c.DiasEntregaPorDefecto = r.DiasEntregaPorDefecto;
        c.UmbralConteo = Math.Round(r.UmbralConteo, 2);
        c.ModificadoEn = Reloj.Ahora;
        c.ModificadoPor = tenant.UsuarioNombre;
        if (cambioModelo) c.ReentrenarPendiente = true;
        await repo.GuardarAsync();
        return new GenericResponse<ConfiguracionResponse>(true, MapearConfig(c),
            cambioModelo ? "Configuración guardada. El pronóstico se recalculará en el próximo minuto con los nuevos parámetros." : "Configuración guardada.");
    }

    // ================================================================ mapeos
    private static ConfiguracionResponse MapearConfig(ConfiguracionEmpresa c) => new()
    {
        SemanasHorizonte = c.SemanasHorizonte, SemanasValidacion = c.SemanasValidacion, SemanasMinimas = c.SemanasMinimas,
        Frecuencia = c.Frecuencia, HoraReentrenamiento = c.HoraReentrenamiento, DiasRevision = c.DiasRevision,
        DiasEntregaPorDefecto = c.DiasEntregaPorDefecto, UmbralConteo = c.UmbralConteo,
        ProximoEntrenamiento = ProximaMarca(c, Reloj.Ahora), ModificadoEn = c.ModificadoEn, ModificadoPor = c.ModificadoPor
    };

    private static EjecucionResponse Mapear(EjecucionPronostico e) => new()
    {
        Id = e.Id, Fecha = e.Fecha, Origen = e.Origen, OrigenNombre = NombreOrigen(e.Origen), Usuario = e.Usuario,
        DuracionMs = e.DuracionMs, Exitosa = e.Exitosa, Mensaje = e.Mensaje, Productos = e.Productos, MapePromedio = e.MapePromedio,
        UltimaSemanaHistorial = e.UltimaSemanaHistorial, SemanasHorizonte = e.SemanasHorizonte
    };

    public static string Calidad(decimal? mape) => mape switch
    {
        null => "sin",
        < 15m => "buena",
        < 30m => "aceptable",
        _ => "baja"
    };

    private static PronosticoProductoResponse MapearProducto(PronosticoProducto r, DateOnly hoy)
    {
        var futuras = r.Semanas.Where(s => !s.EsValidacion).OrderBy(s => s.Semana).Take(4).ToList();
        var proximas = futuras.Sum(s => s.Paquetes);
        var referencia = r.PromedioReciente * futuras.Count;
        return new PronosticoProductoResponse
        {
            ProductoId = r.ProductoId, Codigo = r.Producto?.Codigo ?? string.Empty, Nombre = r.Producto?.Nombre ?? string.Empty,
            Metodo = r.Metodo, MetodoNombre = NombreMetodo(r.Metodo), Mape = r.Mape, Calidad = Calidad(r.Mape),
            SemanasHistorial = r.SemanasHistorial, PromedioReciente = r.PromedioReciente, Proximas4 = Math.Round(proximas, 2),
            Variacion = referencia > 0 ? Math.Round((proximas - referencia) / referencia * 100, 1) : null,
            Nota = r.Nota
        };
    }

    private static decimal? Dec(double? v) => v is double x && !double.IsNaN(x) && !double.IsInfinity(x) ? Math.Round((decimal)x, 1) : null;
    private static decimal Dec2(double v) => double.IsNaN(v) || double.IsInfinity(v) ? 0 : Math.Round((decimal)Math.Max(0, v), 2);
    private static string Recortar(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";
}

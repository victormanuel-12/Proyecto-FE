using SPIn.Models;

namespace SPIn.Services.Prediccion;

public class ResultadoMotor
{
    public MetodoPronostico Metodo { get; set; }
    public double? Mape { get; set; }
    public Dictionary<MetodoPronostico, double?> MapePorMetodo { get; } = [];
    public Proyeccion Pronostico { get; set; } = new([], [], []);
    /// <summary>Semanas reales reservadas y lo que el método elegido pronosticó para ellas.</summary>
    public double[] ValidacionReal { get; set; } = [];
    public Proyeccion? ValidacionPronostico { get; set; }
    public List<string> Notas { get; } = [];
}

/// <summary>
/// Elige el mejor método para una serie semanal:
/// 1. Reserva las últimas «semanas de validación» y entrena cada método con el resto.
/// 2. Mide el MAPE de cada uno contra lo que realmente se vendió.
/// 3. Elige el de menor MAPE y lo vuelve a entrenar con toda la serie para pronosticar el horizonte.
/// Si SSA (ML.NET) falla, la serie sigue con los otros métodos y se deja la nota.
/// </summary>
public static class MotorPronostico
{
    public static ResultadoMotor Pronosticar(IReadOnlyList<double> serie, int horizonte, int semanasValidacion, int semanasMinimas)
    {
        var r = new ResultadoMotor();
        var n = serie.Count;

        if (n < Math.Max(4, semanasMinimas))
        {
            r.Metodo = MetodoPronostico.SinHistorial;
            r.Pronostico = MetodosPronostico.PromedioMovil(serie, horizonte);
            r.Notas.Add(n == 0
                ? "Sin ventas registradas: se usará el consumo estimado de cada insumo."
                : $"Solo {n} semana(s) de historia (mínimo {semanasMinimas}): se usa el promedio de lo disponible.");
            return r;
        }

        // Validación: deja al menos 13 semanas para entrenar.
        var v = Math.Min(semanasValidacion, n - 13);
        if (v < 2)
        {
            r.Notas.Add("La historia no alcanza para reservar semanas de validación: el error no se pudo medir.");
            v = 0;
        }

        var candidatos = new Dictionary<MetodoPronostico, Func<IReadOnlyList<double>, int, Proyeccion>>
        {
            [MetodoPronostico.Ssa] = MetodosPronostico.Ssa,
            [MetodoPronostico.Holt] = MetodosPronostico.Holt,
            [MetodoPronostico.PromedioMovil] = (y, h) => MetodosPronostico.PromedioMovil(y, h),
            [MetodoPronostico.Estacional] = MetodosPronostico.Estacional
        };

        bool Aplicable(MetodoPronostico m, int largo, int h) => m switch
        {
            MetodoPronostico.Ssa => MetodosPronostico.SsaAplicable(largo),
            MetodoPronostico.Estacional => MetodosPronostico.EstacionalAplicable(largo, h),
            _ => true
        };

        var validaciones = new Dictionary<MetodoPronostico, Proyeccion>();
        if (v > 0)
        {
            var entrenamiento = serie.Take(n - v).ToList();
            var real = serie.Skip(n - v).ToArray();
            r.ValidacionReal = real;
            foreach (var (metodo, funcion) in candidatos)
            {
                if (!Aplicable(metodo, entrenamiento.Count, v)) { r.MapePorMetodo[metodo] = null; continue; }
                try
                {
                    var p = funcion(entrenamiento, v);
                    validaciones[metodo] = p;
                    r.MapePorMetodo[metodo] = MetodosPronostico.Mape(real, p.Valores);
                }
                catch (Exception ex)
                {
                    r.MapePorMetodo[metodo] = null;
                    if (metodo == MetodoPronostico.Ssa) r.Notas.Add($"SSA (ML.NET) no disponible: {Resumir(ex)}");
                }
            }
        }

        // Elegir: menor MAPE; si no se pudo medir, SSA si es aplicable, si no Holt.
        var medidos = r.MapePorMetodo.Where(x => x.Value is not null).OrderBy(x => x.Value).ToList();
        var orden = medidos.Select(x => x.Key).ToList();
        if (orden.Count == 0) orden = [MetodoPronostico.Ssa, MetodoPronostico.Holt, MetodoPronostico.PromedioMovil];
        foreach (var m in candidatos.Keys.Except(orden)) orden.Add(m);

        foreach (var metodo in orden)
        {
            if (!Aplicable(metodo, n, horizonte)) continue;
            try
            {
                r.Pronostico = candidatos[metodo](serie, horizonte);
                r.Metodo = metodo;
                r.Mape = r.MapePorMetodo.GetValueOrDefault(metodo);
                r.ValidacionPronostico = validaciones.GetValueOrDefault(metodo);
                return r;
            }
            catch (Exception ex)
            {
                if (metodo == MetodoPronostico.Ssa && !r.Notas.Any(x => x.StartsWith("SSA")))
                    r.Notas.Add($"SSA (ML.NET) no disponible: {Resumir(ex)}");
            }
        }

        r.Metodo = MetodoPronostico.PromedioMovil;
        r.Pronostico = MetodosPronostico.PromedioMovil(serie, horizonte);
        r.Mape = r.MapePorMetodo.GetValueOrDefault(MetodoPronostico.PromedioMovil);
        return r;
    }

    private static string Resumir(Exception ex)
    {
        var mensaje = (ex.InnerException ?? ex).Message;
        return mensaje.Length > 160 ? mensaje[..160] + "…" : mensaje;
    }
}

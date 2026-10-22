using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;

namespace SPIn.Services.Prediccion;

/// <summary>Fila de entrada del SSA (ML.NET lee las propiedades públicas por reflexión).</summary>
public sealed class PuntoSerie { public float Valor { get; set; } }

/// <summary>Salida del SSA: h valores con sus límites del intervalo de confianza.</summary>
public sealed class SalidaSsa
{
    public float[] Pronostico { get; set; } = [];
    public float[] Minimo { get; set; } = [];
    public float[] Maximo { get; set; } = [];
}

/// <summary>Resultado de pronosticar h semanas: valor central y banda de confianza del 90 %.</summary>
public record Proyeccion(double[] Valores, double[] Minimos, double[] Maximos);

/// <summary>
/// Métodos de pronóstico de series semanales. Todos son deterministas y no dependen de servicios externos.
/// - SSA (Singular Spectrum Analysis) con ML.NET: descompone la serie en tendencia y ciclos y los proyecta.
/// - Holt: suavizamiento exponencial doble (nivel + tendencia), con parámetros elegidos por mínimo error.
/// - Promedio móvil de 8 semanas.
/// - Estacional: la misma semana del año anterior, ajustada al nivel reciente.
/// </summary>
public static class MetodosPronostico
{
    public const double Z90 = 1.645;

    // ================================================================ SSA (ML.NET)
    /// <summary>
    /// Ventana del SSA: hasta 52 semanas (un año) para captar la estacionalidad anual cuando la historia lo permite;
    /// ML.NET exige que la serie de entrenamiento sea mayor que 2 × ventana.
    /// </summary>
    public static int VentanaSsa(int largo) => Math.Clamp((largo - 1) / 2, 4, 52);

    public static bool SsaAplicable(int largo) => largo >= 13 && largo > 2 * VentanaSsa(largo);

    private static readonly int[] RangosCandidatos = [3, 4, 5, 6, 8];

    /// <summary>
    /// SSA con el rango (número de componentes) elegido por una validación interna sobre la propia serie de entrenamiento:
    /// pocos componentes = solo nivel y tendencia; más componentes = también ciclos (temporadas). Así no se usan los
    /// datos de validación del motor para ajustar el modelo.
    /// </summary>
    public static Proyeccion Ssa(IReadOnlyList<double> serie, int horizonte) =>
        SsaConRango(serie, horizonte, ElegirRango(serie));

    private static int? ElegirRango(IReadOnlyList<double> y)
    {
        var n = y.Count;
        var v = Math.Min(8, n / 5);
        if (v < 2 || !SsaAplicable(n - v)) return null;
        var entrenamiento = y.Take(n - v).ToList();
        var real = y.Skip(n - v).ToArray();
        var ventana = VentanaSsa(entrenamiento.Count);
        int? mejor = null;
        var menor = double.MaxValue;
        foreach (var r in RangosCandidatos)
        {
            if (r >= ventana) continue;
            try
            {
                if (Mape(real, SsaConRango(entrenamiento, v, r).Valores) is double e && e < menor) { menor = e; mejor = r; }
            }
            catch (Exception) { /* este rango no converge: se prueba el siguiente */ }
        }
        return mejor;
    }

    public static Proyeccion SsaConRango(IReadOnlyList<double> serie, int horizonte, int? rango)
    {
        var n = serie.Count;
        var ventana = VentanaSsa(n);
        int? r = rango is int x && x >= 1 && x < ventana ? x : null;
        var ml = new MLContext(seed: 1);
        var datos = ml.Data.LoadFromEnumerable(serie.Select(v => new PuntoSerie { Valor = (float)v }));
        var tuberia = ml.Forecasting.ForecastBySsa(
            outputColumnName: nameof(SalidaSsa.Pronostico),
            inputColumnName: nameof(PuntoSerie.Valor),
            windowSize: ventana,
            seriesLength: n,
            trainSize: n,
            horizon: horizonte,
            rankSelectionMethod: r is null ? RankSelectionMethod.Exact : RankSelectionMethod.Fixed,
            rank: r,
            confidenceLevel: 0.90f,
            confidenceLowerBoundColumn: nameof(SalidaSsa.Minimo),
            confidenceUpperBoundColumn: nameof(SalidaSsa.Maximo));
        var modelo = tuberia.Fit(datos);
        using var motor = modelo.CreateTimeSeriesEngine<PuntoSerie, SalidaSsa>(ml);
        var salida = motor.Predict();
        if (salida.Pronostico.Length < horizonte) throw new InvalidOperationException("SSA devolvió menos semanas de las pedidas.");

        var valores = salida.Pronostico.Take(horizonte).Select(v => Math.Max(0, (double)v)).ToArray();
        var minimos = salida.Minimo.Length >= horizonte ? salida.Minimo.Take(horizonte).Select(v => Math.Max(0, (double)v)).ToArray() : valores;
        var maximos = salida.Maximo.Length >= horizonte ? salida.Maximo.Take(horizonte).Select(v => Math.Max(0, (double)v)).ToArray() : valores;
        if (valores.Any(v => double.IsNaN(v) || double.IsInfinity(v))) throw new InvalidOperationException("SSA devolvió valores no numéricos.");
        return new Proyeccion(valores, minimos, maximos);
    }

    // ================================================================ Holt
    public static Proyeccion Holt(IReadOnlyList<double> y, int horizonte)
    {
        if (y.Count < 3) return PromedioMovil(y, horizonte);
        double mejorError = double.MaxValue, mejorNivel = 0, mejorTendencia = 0;
        var residuos = new List<double>();
        for (var a = 0.1; a < 0.95; a += 0.1)
        {
            for (var b = 0.0; b < 0.55; b += 0.1)
            {
                double nivel = y[0], tendencia = y[1] - y[0], error = 0;
                var res = new List<double>();
                for (var t = 1; t < y.Count; t++)
                {
                    var f = nivel + tendencia;
                    res.Add(y[t] - f);
                    error += (y[t] - f) * (y[t] - f);
                    var nuevo = a * y[t] + (1 - a) * (nivel + tendencia);
                    tendencia = b * (nuevo - nivel) + (1 - b) * tendencia;
                    nivel = nuevo;
                }
                if (error < mejorError) { mejorError = error; mejorNivel = nivel; mejorTendencia = tendencia; residuos = res; }
            }
        }
        var sigma = Desviacion(residuos);
        var valores = Enumerable.Range(1, horizonte).Select(h => Math.Max(0, mejorNivel + mejorTendencia * h)).ToArray();
        return ConBanda(valores, sigma, creciente: true);
    }

    // ================================================================ promedio móvil
    public static Proyeccion PromedioMovil(IReadOnlyList<double> y, int horizonte, int semanas = 8)
    {
        if (y.Count == 0) return new Proyeccion(new double[horizonte], new double[horizonte], new double[horizonte]);
        var ultimas = y.Skip(Math.Max(0, y.Count - semanas)).ToList();
        var promedio = ultimas.Average();
        var sigma = ultimas.Count > 1 ? Desviacion(ultimas.Select(v => v - promedio).ToList()) : promedio * 0.25;
        return ConBanda(Enumerable.Repeat(promedio, horizonte).ToArray(), sigma, creciente: false);
    }

    // ================================================================ estacional
    public static bool EstacionalAplicable(int largo, int horizonte) => largo >= 52 + Math.Max(4, horizonte);

    /// <summary>Misma semana del año anterior × (nivel de las últimas 8 semanas / nivel de esas mismas semanas hace un año).</summary>
    public static Proyeccion Estacional(IReadOnlyList<double> y, int horizonte)
    {
        var n = y.Count;
        var reciente = y.Skip(n - 8).Average();
        var haceUnAnio = y.Skip(n - 52 - 8).Take(8).Average();
        var factor = haceUnAnio > 0 ? Math.Clamp(reciente / haceUnAnio, 0.5, 2.0) : 1.0;
        var valores = Enumerable.Range(0, horizonte)
            .Select(h => Math.Max(0, (n - 52 + h < n ? y[n - 52 + h] : y[n - 52 + h - 52]) * factor)).ToArray();
        var residuos = Enumerable.Range(n - 8, 8).Select(t => y[t] - y[t - 52] * factor).ToList();
        return ConBanda(valores, Desviacion(residuos), creciente: true);
    }

    // ================================================================ utilidades
    /// <summary>MAPE (%) sobre las semanas con venta real &gt; 0. Null si no hay ninguna.</summary>
    public static double? Mape(IReadOnlyList<double> real, IReadOnlyList<double> pronostico)
    {
        var errores = real.Zip(pronostico).Where(p => p.First > 0).Select(p => Math.Abs(p.First - p.Second) / p.First).ToList();
        return errores.Count == 0 ? null : errores.Average() * 100;
    }

    public static double Desviacion(IReadOnlyCollection<double> residuos)
    {
        if (residuos.Count < 2) return 0;
        var media = residuos.Average();
        return Math.Sqrt(residuos.Sum(r => (r - media) * (r - media)) / (residuos.Count - 1));
    }

    private static Proyeccion ConBanda(double[] valores, double sigma, bool creciente)
    {
        var min = new double[valores.Length];
        var max = new double[valores.Length];
        for (var h = 0; h < valores.Length; h++)
        {
            var ancho = Z90 * sigma * (creciente ? Math.Sqrt(h + 1) : 1);
            min[h] = Math.Max(0, valores[h] - ancho);
            max[h] = valores[h] + ancho;
        }
        return new Proyeccion(valores, min, max);
    }

    /// <summary>Inversa de la normal estándar (aproximación de Acklam, error &lt; 1.2e-9). Ej.: 95 % → 1.645.</summary>
    public static double ZDeNivelServicio(double probabilidad)
    {
        var p = Math.Clamp(probabilidad, 0.5, 0.9999);
        double[] a = [-3.969683028665376e+01, 2.209460984245205e+02, -2.759285104469687e+02, 1.383577518672690e+02, -3.066479806614716e+01, 2.506628277459239e+00];
        double[] b = [-5.447609879822406e+01, 1.615858368580409e+02, -1.556989798598866e+02, 6.680131188771972e+01, -1.328068155288572e+01];
        double[] c = [-7.784894002430293e-03, -3.223964580411365e-01, -2.400758277161838e+00, -2.549732539343734e+00, 4.374664141464968e+00, 2.938163982698783e+00];
        double[] d = [7.784695709041462e-03, 3.224671290700398e-01, 2.445134137142996e+00, 3.754408661907416e+00];
        const double pAlto = 1 - 0.02425;
        if (p <= pAlto)
        {
            var q = p - 0.5;
            var r = q * q;
            return (((((a[0] * r + a[1]) * r + a[2]) * r + a[3]) * r + a[4]) * r + a[5]) * q /
                   (((((b[0] * r + b[1]) * r + b[2]) * r + b[3]) * r + b[4]) * r + 1);
        }
        var s = Math.Sqrt(-2 * Math.Log(1 - p));
        return -(((((c[0] * s + c[1]) * s + c[2]) * s + c[3]) * s + c[4]) * s + c[5]) /
               ((((d[0] * s + d[1]) * s + d[2]) * s + d[3]) * s + 1);
    }
}

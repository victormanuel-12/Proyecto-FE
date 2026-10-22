namespace SPIn.Data.Demo;

/// <summary>
/// Demanda simulada de una embotelladora peruana (paquetes por día), usada solo por los datos de demostración:
/// - Verano fuerte (pico a inicios de febrero) e invierno bajo (agosto): ±30 %.
/// - Fiestas Patrias (20 al 31 de julio) +25 %, fiestas de fin de año (15 al 31 de diciembre) +30 %, Año Nuevo +15 %.
/// - Sábado +15 %, lunes −5 %, domingo −30 % (menos despachos).
/// - Crecimiento de 6 % al año y ruido diario de ±12 %.
/// </summary>
public sealed class ModeloDemanda(DateOnly inicio, int semilla)
{
    private readonly Random _azar = new(semilla);

    public static readonly (string Canal, Models.CanalVenta Valor, double Participacion)[] Canales =
    [
        ("tradicional", Models.CanalVenta.CanalTradicional, 0.50),
        ("moderno", Models.CanalVenta.CanalModerno, 0.25),
        ("mayorista", Models.CanalVenta.Mayorista, 0.15),
        ("distribuidor", Models.CanalVenta.Distribuidor, 0.10)
    ];

    public double Factor(DateOnly d)
    {
        var dia = d.DayOfYear;
        var estacion = 1 + 0.30 * Math.Cos(2 * Math.PI * (dia - 40) / 365.0);
        if (d.Month == 7 && d.Day >= 20) estacion *= 1.25;
        if (d.Month == 12 && d.Day >= 15) estacion *= 1.30;
        if (d.Month == 1 && d.Day <= 2) estacion *= 1.15;
        var semana = d.DayOfWeek switch { DayOfWeek.Saturday => 1.15, DayOfWeek.Sunday => 0.70, DayOfWeek.Monday => 0.95, _ => 1.0 };
        var tendencia = 1 + 0.06 * ((d.DayNumber - inicio.DayNumber) / 365.0);
        return estacion * semana * tendencia;
    }

    /// <summary>Venta esperada (sin ruido) del día: la usa el «planificador» simulado para producir y comprar.</summary>
    public double Esperado(double basePaquetes, DateOnly d) => basePaquetes * Factor(d);

    /// <summary>Venta real del día por canal (con ruido). Puede no haber venta en un canal.</summary>
    public List<(Models.CanalVenta Canal, int Paquetes)> Ventas(double basePaquetes, DateOnly d)
    {
        var lista = new List<(Models.CanalVenta, int)>();
        foreach (var (_, canal, participacion) in Canales)
        {
            var q = (int)Math.Round(Esperado(basePaquetes, d) * participacion * Math.Exp(Normal(0, 0.12) - 0.0072));
            if (q > 0) lista.Add((canal, q));
        }
        return lista;
    }

    public double Normal(double media, double desviacion)
    {
        var u1 = 1.0 - _azar.NextDouble();
        var u2 = _azar.NextDouble();
        return media + desviacion * Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    public double Uniforme(double min, double max) => min + (max - min) * _azar.NextDouble();
    public int Entero(int min, int maxInclusive) => _azar.Next(min, maxInclusive + 1);
    public bool Probabilidad(double p) => _azar.NextDouble() < p;
}

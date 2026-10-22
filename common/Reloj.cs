namespace SPIn.Common;

/// <summary>
/// Fecha de negocio en hora de Perú, aunque el servidor (o Docker) esté en UTC.
/// Evita que después de las 7:00 p. m. el sistema crea que ya es el día siguiente.
/// </summary>
public static class Reloj
{
    private static readonly TimeZoneInfo Zona = ObtenerZona();

    private static TimeZoneInfo ObtenerZona()
    {
        foreach (var id in new[] { "America/Lima", "SA Pacific Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        // Perú no usa horario de verano: UTC-5 fijo como último recurso.
        return TimeZoneInfo.CreateCustomTimeZone("Peru", TimeSpan.FromHours(-5), "Perú", "Perú");
    }

    // Solo para la carga de datos de demostración (Entrega 10): permite registrar operaciones con fecha pasada
    // usando los mismos servicios. AsyncLocal limita el efecto al flujo que lo activa; el resto del sistema no se entera.
    private static readonly AsyncLocal<DateTime?> Simulado = new();

    public static DateTime Ahora => Simulado.Value ?? TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona);

    /// <summary>Fija la hora de negocio para el flujo actual hasta que se libere el objeto devuelto.</summary>
    public static IDisposable Simular(DateTime ahora)
    {
        var previo = Simulado.Value;
        Simulado.Value = ahora;
        return new Restaurador(previo);
    }

    private sealed class Restaurador(DateTime? previo) : IDisposable
    {
        public void Dispose() => Simulado.Value = previo;
    }

    public static DateOnly Hoy => DateOnly.FromDateTime(Ahora);
}

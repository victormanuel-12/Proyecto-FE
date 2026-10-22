using SPIn.Data.Demo;
using SPIn.Models;
using SPIn.Services.Prediccion;
using SPIn.Services.ServiceImpl;

namespace SPIn.Pruebas;

/// <summary>Motor predictivo: métodos, elección por MAPE y SSA real de ML.NET.</summary>
public class MotorPronosticoPruebas
{
    /// <summary>Serie semanal con tendencia, estacionalidad anual y ruido, como la de una embotelladora.</summary>
    private static List<double> SerieEstacional(int semanas, int semilla = 7)
    {
        var azar = new Random(semilla);
        return Enumerable.Range(0, semanas).Select(t =>
            (2000 + 3 * t) * (1 + 0.3 * Math.Cos(2 * Math.PI * (t - 5) / 52.0)) * (1 + 0.05 * (azar.NextDouble() - 0.5))).ToList();
    }

    [Fact]
    public void Z_del_nivel_de_servicio_coincide_con_la_normal() 
    {
        Assert.Equal(1.6449, MetodosPronostico.ZDeNivelServicio(0.95), 3);
        Assert.Equal(1.8808, MetodosPronostico.ZDeNivelServicio(0.97), 3);
        Assert.Equal(2.3263, MetodosPronostico.ZDeNivelServicio(0.99), 3);
    }

    [Fact]
    public void Mape_ignora_semanas_sin_venta()
    {
        Assert.Equal(10, MetodosPronostico.Mape([100, 0, 200], [110, 50, 180])!.Value, 6);
        Assert.Null(MetodosPronostico.Mape([0, 0], [1, 1]));
    }

    [Fact]
    public void Holt_sigue_una_tendencia_lineal()
    {
        var y = Enumerable.Range(0, 30).Select(t => 100.0 + 10 * t).ToList();
        var p = MetodosPronostico.Holt(y, 3);
        Assert.InRange(p.Valores[0], 395, 405);
        Assert.InRange(p.Valores[2], 415, 425);
    }

    [Fact]
    public void Promedio_movil_usa_las_ultimas_8_semanas()
    {
        var y = Enumerable.Repeat(50.0, 20).Concat(Enumerable.Repeat(100.0, 8)).ToList();
        Assert.All(MetodosPronostico.PromedioMovil(y, 4).Valores, v => Assert.Equal(100, v, 6));
    }

    [Fact]
    public void Ssa_de_MLNET_aprende_la_estacionalidad()
    {
        var y = SerieEstacional(130);
        var entrenamiento = y.Take(122).ToList();
        var p = MetodosPronostico.Ssa(entrenamiento, 8);
        var mape = MetodosPronostico.Mape(y.Skip(122).ToList(), p.Valores)!.Value;
        Assert.True(mape < 15, $"MAPE de SSA = {mape:0.0} %");
        Assert.All(p.Valores, v => Assert.True(v >= 0));
        Assert.All(Enumerable.Range(0, 8), h => Assert.True(p.Minimos[h] <= p.Valores[h] && p.Valores[h] <= p.Maximos[h] + 1e-6));
    }

    [Fact]
    public void Motor_elige_el_metodo_de_menor_error_y_pronostica_el_horizonte()
    {
        var r = MotorPronostico.Pronosticar(SerieEstacional(130), 12, 8, 12);
        Assert.NotEqual(MetodoPronostico.SinHistorial, r.Metodo);
        Assert.Equal(12, r.Pronostico.Valores.Length);
        Assert.NotNull(r.Mape);
        var minimo = r.MapePorMetodo.Values.Where(v => v is not null).Min();
        Assert.Equal(minimo, r.Mape);
        Assert.True(r.Mape < 15, $"MAPE elegido = {r.Mape:0.0} %");
    }

    [Fact]
    public void Motor_con_poca_historia_usa_el_promedio_y_lo_avisa()
    {
        var r = MotorPronostico.Pronosticar([100, 120, 110], 4, 8, 12);
        Assert.Equal(MetodoPronostico.SinHistorial, r.Metodo);
        Assert.Equal(4, r.Pronostico.Valores.Length);
        Assert.NotEmpty(r.Notas);
    }

    [Fact]
    public void Serie_semanal_rellena_con_cero_las_semanas_sin_venta()
    {
        var lunes = new DateOnly(2026, 9, 7);
        var ventas = new List<(int, DateOnly, decimal)> { (1, lunes, 10), (1, lunes.AddDays(2), 5), (1, lunes.AddDays(14), 7) };
        var serie = PrediccionService.SerieSemanal(ventas, lunes.AddDays(14));
        Assert.Equal([15d, 0d, 7d], serie.Select(s => s.Paquetes).ToArray());
    }

    [Fact]
    public void Reentrenamiento_semanal_toca_cada_lunes_a_la_hora_configurada()
    {
        var c = new ConfiguracionEmpresa { Frecuencia = FrecuenciaReentrenamiento.Semanal, HoraReentrenamiento = 6 };
        Assert.Equal(new DateTime(2026, 10, 19, 6, 0, 0), PrediccionService.UltimaMarca(c, new DateTime(2026, 10, 21, 19, 0, 0)));
        Assert.Equal(new DateTime(2026, 10, 12, 6, 0, 0), PrediccionService.UltimaMarca(c, new DateTime(2026, 10, 19, 5, 0, 0)));
        Assert.Equal(new DateTime(2026, 10, 26, 6, 0, 0), PrediccionService.ProximaMarca(c, new DateTime(2026, 10, 21, 19, 0, 0)));
    }

    [Fact]
    public void Demanda_de_demostracion_tiene_verano_alto_e_invierno_bajo()
    {
        var modelo = new ModeloDemanda(new DateOnly(2024, 4, 1), 1);
        Assert.True(modelo.Factor(new DateOnly(2026, 2, 11)) > modelo.Factor(new DateOnly(2026, 8, 12)) * 1.5);
        Assert.True(modelo.Factor(new DateOnly(2025, 7, 26)) > modelo.Factor(new DateOnly(2025, 7, 9)));
    }
}

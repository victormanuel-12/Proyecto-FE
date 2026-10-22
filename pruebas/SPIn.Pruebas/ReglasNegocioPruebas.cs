using SPIn.Common;
using SPIn.Services.Importacion;

namespace SPIn.Pruebas;

/// <summary>Validaciones generales: RUC, lectura de números y fechas de los archivos importados.</summary>
public class ReglasNegocioPruebas
{
    [Theory]
    [InlineData("20123456786", true)]   // RUC de la empresa de ejemplo
    [InlineData("20100047218", true)]
    [InlineData("20123456787", false)]  // dígito verificador incorrecto
    [InlineData("30123456786", false)]  // prefijo inválido
    [InlineData("2012345678", false)]   // 10 dígitos
    [InlineData(null, false)]
    public void Ruc_se_valida_con_el_digito_verificador(string? ruc, bool valido) =>
        Assert.Equal(valido, Validaciones.RucValido(ruc));

    [Theory]
    [InlineData("1234.5", 1234.5)]
    [InlineData("1234,5", 1234.5)]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("1,234.56", 1234.56)]
    [InlineData(" 15 % ", 15)]
    public void Importacion_acepta_punto_o_coma_decimal(string texto, double esperado)
    {
        Assert.True(ImportadorBase.TryDecimal(texto, out var valor));
        Assert.Equal((decimal)esperado, valor);
    }

    [Theory]
    [InlineData("15/03/2026")]
    [InlineData("2026-03-15")]
    [InlineData("46096")] // número de serie de Excel
    public void Importacion_lee_fechas_en_varios_formatos(string texto)
    {
        Assert.True(ImportadorBase.TryFecha(texto, out var fecha));
        Assert.Equal(new DateOnly(2026, 3, 15), fecha);
    }

    [Theory]
    [InlineData("Categoría", "categoria")]
    [InlineData("CO₂", "co2")]
    [InlineData("Unidad compra", "unidadcompra")]
    public void Encabezados_se_comparan_sin_tildes_ni_espacios(string texto, string clave) =>
        Assert.Equal(clave, ImportadorBase.Clave(texto));

    [Fact]
    public void Reloj_simulado_solo_afecta_al_flujo_que_lo_activa()
    {
        var antes = Reloj.Hoy;
        using (Reloj.Simular(new DateTime(2025, 1, 15, 8, 0, 0)))
        {
            Assert.Equal(new DateOnly(2025, 1, 15), Reloj.Hoy);
        }
        Assert.Equal(antes, Reloj.Hoy);
    }
}

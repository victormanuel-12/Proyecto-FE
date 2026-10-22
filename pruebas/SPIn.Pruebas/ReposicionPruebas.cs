using SPIn.Models;
using SPIn.Repository;
using SPIn.Services.ServiceImpl;

namespace SPIn.Pruebas;

/// <summary>Política de reposición: stock de seguridad, punto de reorden, estado y cantidad sugerida.</summary>
public class ReposicionPruebas
{
    private static readonly DateOnly Hoy = new(2026, 10, 21);

    private static InsumoReposicionDatos Insumo(decimal stock) => new()
    {
        Id = 1, Codigo = "PRE-500", Nombre = "Preforma", UnidadConsumoCodigo = "UND", UnidadCompraCodigo = "MIL",
        FactorConversion = 1000, NivelServicio = 97, TipoConsumo = TipoConsumo.PorProduccion, ConsumoEstimadoDiario = 1000,
        StockMinimo = 0, StockMaximo = 100000, Stock = stock,
        Condicion = new CondicionReposicion
        {
            ProveedorId = 9, ProveedorNombre = "Proveedor", TiempoEntregaDias = 7, VariacionEntregaDias = 2,
            PedidoMinimo = 5, MultiploCompra = 5, PrecioUnitario = 90, Moneda = Moneda.PEN
        }
    };

    private static readonly ConfiguracionEmpresa Config = new() { DiasRevision = 7, DiasEntregaPorDefecto = 7 };

    [Fact]
    public void Calcula_seguridad_y_punto_de_reorden_con_la_formula()
    {
        // Sin pronóstico: d = consumo estimado (1000/día) y σd = 25 % de d.
        var r = ReposicionService.Calcular(Insumo(50000), Config, Hoy, 0, 0, null, []);
        var z = 1.8808;
        var ss = z * Math.Sqrt(7 * 250.0 * 250 + 1000.0 * 1000 * 2 * 2);
        Assert.Equal(1000, r.ConsumoDiario);
        Assert.Equal((decimal)Math.Round(ss, 2), r.StockSeguridad, 0);
        Assert.Equal((decimal)Math.Round(7000 + ss, 2), r.PuntoReorden, 0);
        Assert.Equal("ok", r.Estado);
        Assert.Equal(50, r.DiasCobertura);
    }

    [Fact]
    public void Bajo_el_punto_de_reorden_pide_redondeando_al_multiplo()
    {
        var r = ReposicionService.Calcular(Insumo(9000), Config, Hoy, 0, 0, null, []);
        Assert.Equal("pedir", r.Estado);
        Assert.Equal(Hoy, r.FechaLimitePedido);
        Assert.Equal(0, r.CantidadSugerida % 5);                 // múltiplo de 5 millares
        Assert.True(r.CantidadSugeridaConsumo + 9000 >= r.PuntoReorden + 7 * 1000 - 0.01m);
    }

    [Fact]
    public void Si_no_alcanza_para_el_tiempo_de_entrega_es_urgente()
    {
        var r = ReposicionService.Calcular(Insumo(3000), Config, Hoy, 0, 0, null, []);
        Assert.Equal("urgente", r.Estado);
        Assert.Equal(Hoy.AddDays(3), r.FechaAgotamiento);
    }

    [Fact]
    public void Lo_que_esta_en_camino_cuenta_en_la_posicion()
    {
        var r = ReposicionService.Calcular(Insumo(3000), Config, Hoy, 60000, 0, null, []);
        Assert.Equal("ok", r.Estado);
        Assert.Equal(60000, r.EnCamino);
    }

    [Fact]
    public void Sin_proveedor_se_marca_y_usa_la_entrega_por_defecto()
    {
        var i = Insumo(3000);
        i.Condicion = null;
        var r = ReposicionService.Calcular(i, Config, Hoy, 0, 0, null, []);
        Assert.Equal("sin-proveedor", r.Estado);
        Assert.Equal(7, r.TiempoEntrega);
    }

    [Fact]
    public void Sin_demanda_no_sugiere_compras()
    {
        var i = Insumo(3000);
        i.ConsumoEstimadoDiario = null;
        var r = ReposicionService.Calcular(i, Config, Hoy, 0, 0, null, []);
        Assert.Equal("sin-demanda", r.Estado);
        Assert.Equal(0, r.CantidadSugerida);
    }
}

namespace SPIn.Models;

public enum TipoAlmacen
{
    Insumos = 1,
    ProductoTerminado = 2,
    Cuarentena = 3
}

public enum TipoEnvase
{
    Pet = 1,
    VidrioRetornable = 2,
    Lata = 3
}

public static class Etiquetas
{
    public static string De(TipoAlmacen tipo) => tipo switch
    {
        TipoAlmacen.Insumos => "Insumos",
        TipoAlmacen.ProductoTerminado => "Producto terminado",
        TipoAlmacen.Cuarentena => "Cuarentena",
        _ => tipo.ToString()
    };

    public static string De(TipoEnvase tipo) => tipo switch
    {
        TipoEnvase.Pet => "PET",
        TipoEnvase.VidrioRetornable => "Vidrio retornable",
        TipoEnvase.Lata => "Lata",
        _ => tipo.ToString()
    };
}

public enum TipoConsumo
{
    /// <summary>Se calcula con la lista de materiales: por botella, paquete o pallet producido.</summary>
    PorProduccion = 1,
    /// <summary>Depende de las horas de operación de la línea (lubricante, soda cáustica).</summary>
    PorOperacion = 2,
    /// <summary>Se registra a mano.</summary>
    Manual = 3
}

public enum Moneda
{
    PEN = 1,
    USD = 2
}

public static class EtiquetasCatalogo
{
    public static string De(TipoConsumo tipo) => tipo switch
    {
        TipoConsumo.PorProduccion => "Por producción",
        TipoConsumo.PorOperacion => "Por horas de operación",
        TipoConsumo.Manual => "Manual",
        _ => tipo.ToString()
    };

    public static string Simbolo(Moneda moneda) => moneda == Moneda.USD ? "US$" : "S/";
}

/// <summary>A qué se refiere la cantidad de una línea de receta.</summary>
public enum NivelReceta
{
    PorBotella = 1,
    PorPaquete = 2,
    PorPallet = 3
}

public static class EtiquetasProduccion
{
    public static string De(NivelReceta nivel) => nivel switch
    {
        NivelReceta.PorBotella => "Por botella",
        NivelReceta.PorPaquete => "Por paquete",
        NivelReceta.PorPallet => "Por pallet",
        _ => nivel.ToString()
    };
}

public enum TipoMovimiento
{
    StockInicial = 1,
    EntradaCompra = 2,
    SalidaProduccion = 3,
    SalidaConsumo = 4,
    AjustePositivo = 5,
    AjusteNegativo = 6,
    Merma = 7
}

public enum EstadoConteo
{
    Abierto = 1,
    Aprobado = 2,
    Anulado = 3
}

public static class EtiquetasInventario
{
    public static string De(TipoMovimiento tipo) => tipo switch
    {
        TipoMovimiento.StockInicial => "Stock inicial",
        TipoMovimiento.EntradaCompra => "Entrada por compra",
        TipoMovimiento.SalidaProduccion => "Salida a producción",
        TipoMovimiento.SalidaConsumo => "Salida por consumo",
        TipoMovimiento.AjustePositivo => "Ajuste positivo",
        TipoMovimiento.AjusteNegativo => "Ajuste negativo",
        TipoMovimiento.Merma => "Merma",
        _ => tipo.ToString()
    };

    /// <summary>+1 si el movimiento suma al stock, −1 si resta.</summary>
    public static int Signo(TipoMovimiento tipo) => tipo switch
    {
        TipoMovimiento.StockInicial or TipoMovimiento.EntradaCompra or TipoMovimiento.AjustePositivo => 1,
        _ => -1
    };

    public static string De(EstadoConteo estado) => estado switch
    {
        EstadoConteo.Abierto => "Abierto",
        EstadoConteo.Aprobado => "Aprobado",
        EstadoConteo.Anulado => "Anulado",
        _ => estado.ToString()
    };
}

public enum CanalVenta
{
    Mayorista = 1,
    CanalTradicional = 2,
    CanalModerno = 3,
    Distribuidor = 4,
    Otro = 5
}

public enum OrigenVenta
{
    Manual = 1,
    Importacion = 2
}

public enum TipoMovimientoProducto
{
    StockInicial = 1,
    EntradaProduccion = 2,
    SalidaVenta = 3,
    AjustePositivo = 4,
    AjusteNegativo = 5,
    Merma = 6
}

public static class EtiquetasVentas
{
    public static string De(CanalVenta canal) => canal switch
    {
        CanalVenta.Mayorista => "Mayorista",
        CanalVenta.CanalTradicional => "Canal tradicional (bodegas)",
        CanalVenta.CanalModerno => "Canal moderno (supermercados)",
        CanalVenta.Distribuidor => "Distribuidor",
        _ => "Otro"
    };

    public static string De(TipoMovimientoProducto tipo) => tipo switch
    {
        TipoMovimientoProducto.StockInicial => "Stock inicial",
        TipoMovimientoProducto.EntradaProduccion => "Entrada de producción",
        TipoMovimientoProducto.SalidaVenta => "Salida por venta",
        TipoMovimientoProducto.AjustePositivo => "Ajuste positivo",
        TipoMovimientoProducto.AjusteNegativo => "Ajuste negativo",
        TipoMovimientoProducto.Merma => "Merma",
        _ => tipo.ToString()
    };

    public static int Signo(TipoMovimientoProducto tipo) => tipo switch
    {
        TipoMovimientoProducto.StockInicial or TipoMovimientoProducto.EntradaProduccion or TipoMovimientoProducto.AjustePositivo => 1,
        _ => -1
    };
}

// ---------------------------------------------------------------- Entrega 6 y 7: producción y pedidos

public enum Turno
{
    Manana = 1,
    Tarde = 2,
    Noche = 3
}

public enum EstadoPlan
{
    Planificado = 1,
    Ejecutado = 2,
    Cancelado = 3
}

public enum EstadoParte
{
    Registrado = 1,
    Anulado = 2
}

public enum EstadoPedido
{
    /// <summary>Emitido al proveedor, aún no llega nada.</summary>
    Pendiente = 1,
    /// <summary>Llegó una parte.</summary>
    Parcial = 2,
    /// <summary>Llegó todo.</summary>
    Recibido = 3,
    /// <summary>No llegará (lo pendiente se descarta).</summary>
    Cancelado = 4
}

public static class EtiquetasOperacion
{
    public static string De(Turno t) => t switch { Turno.Manana => "Mañana", Turno.Tarde => "Tarde", Turno.Noche => "Noche", _ => t.ToString() };
    public static string De(EstadoPlan e) => e switch { EstadoPlan.Planificado => "Planificado", EstadoPlan.Ejecutado => "Ejecutado", EstadoPlan.Cancelado => "Cancelado", _ => e.ToString() };
    public static string De(EstadoParte e) => e == EstadoParte.Anulado ? "Anulado" : "Registrado";
    public static string De(EstadoPedido e) => e switch
    {
        EstadoPedido.Pendiente => "En camino",
        EstadoPedido.Parcial => "Recibido parcial",
        EstadoPedido.Recibido => "Recibido",
        EstadoPedido.Cancelado => "Cancelado",
        _ => e.ToString()
    };
}

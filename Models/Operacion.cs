namespace SPIn.Models;

/// <summary>Producción programada: qué producto, cuántos paquetes, en qué línea y día.</summary>
public class PlanProduccion : EntidadAuditable, IEntidadEmpresa
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public DateOnly Fecha { get; set; }
    public int LineaId { get; set; }
    public LineaProduccion? Linea { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public decimal Paquetes { get; set; }
    public EstadoPlan Estado { get; set; } = EstadoPlan.Planificado;
    public string? Comentario { get; set; }
    /// <summary>Parte de producción que lo ejecutó.</summary>
    public int? ParteId { get; set; }
}

/// <summary>
/// Parte de producción: lo que realmente se produjo. Al registrarlo descuenta los insumos según la receta vigente
/// (con merma) e ingresa los paquetes al almacén de producto terminado.
/// </summary>
public class ParteProduccion : IEntidadEmpresa
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int Numero { get; set; }
    public DateOnly Fecha { get; set; }
    public Turno Turno { get; set; } = Turno.Manana;
    public int LineaId { get; set; }
    public LineaProduccion? Linea { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public decimal Paquetes { get; set; }
    public decimal? HorasOperacion { get; set; }
    public int AlmacenInsumosId { get; set; }
    public Almacen? AlmacenInsumos { get; set; }
    public int AlmacenProductoId { get; set; }
    public Almacen? AlmacenProducto { get; set; }
    public int RecetaVersion { get; set; }
    public int? PlanId { get; set; }
    public EstadoParte Estado { get; set; } = EstadoParte.Registrado;
    public string? Observacion { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime CreadoEn { get; set; }
    public string CreadoPor { get; set; } = string.Empty;
    public ICollection<ParteConsumo> Consumos { get; set; } = new List<ParteConsumo>();
}

public class ParteConsumo
{
    public int Id { get; set; }
    public int ParteId { get; set; }
    public ParteProduccion? Parte { get; set; }
    public int InsumoId { get; set; }
    public Insumo? Insumo { get; set; }
    /// <summary>Cantidad descontada, en unidad de consumo (incluye merma).</summary>
    public decimal Cantidad { get; set; }
    public long MovimientoId { get; set; }
}

/// <summary>Pedido (orden de compra) a un proveedor. Mientras no llegue todo, cuenta como stock «en camino».</summary>
public class PedidoCompra : EntidadAuditable, IEntidadEmpresa
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public int ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }
    public DateOnly FechaPedido { get; set; }
    public DateOnly FechaEstimada { get; set; }
    public Moneda Moneda { get; set; } = Moneda.PEN;
    public EstadoPedido Estado { get; set; } = EstadoPedido.Pendiente;
    public string? DocumentoProveedor { get; set; }
    public string? Observacion { get; set; }
    public string? MotivoCancelacion { get; set; }
    public ICollection<PedidoDetalle> Detalles { get; set; } = new List<PedidoDetalle>();
    public ICollection<Recepcion> Recepciones { get; set; } = new List<Recepcion>();
}

public class PedidoDetalle
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public PedidoCompra? Pedido { get; set; }
    public int InsumoId { get; set; }
    public Insumo? Insumo { get; set; }
    /// <summary>En unidad de compra.</summary>
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    /// <summary>En unidad de compra.</summary>
    public decimal CantidadRecibida { get; set; }
}

/// <summary>Llegada (total o parcial) de un pedido. Genera entradas por compra en el kardex.</summary>
public class Recepcion : IEntidadEmpresa
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int PedidoId { get; set; }
    public PedidoCompra? Pedido { get; set; }
    public DateTime Fecha { get; set; }
    public int AlmacenId { get; set; }
    public Almacen? Almacen { get; set; }
    public string Guia { get; set; } = string.Empty;
    public string? Observacion { get; set; }
    public bool Anulada { get; set; }
    public string? MotivoAnulacion { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public ICollection<RecepcionDetalle> Detalles { get; set; } = new List<RecepcionDetalle>();
}

public class RecepcionDetalle
{
    public int Id { get; set; }
    public int RecepcionId { get; set; }
    public Recepcion? Recepcion { get; set; }
    public int PedidoDetalleId { get; set; }
    public PedidoDetalle? PedidoDetalle { get; set; }
    /// <summary>En unidad de compra.</summary>
    public decimal Cantidad { get; set; }
    public long MovimientoId { get; set; }
}

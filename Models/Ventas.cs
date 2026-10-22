namespace SPIn.Models;

/// <summary>
/// Venta o despacho de un producto en un día. Es el historial de demanda que usará el pronóstico (Entrega 8).
/// La cantidad está en paquetes (la unidad de venta del SKU; ej.: paquete x12).
/// </summary>
public class Venta : IEntidadEmpresa
{
    public long Id { get; set; }
    public int EmpresaId { get; set; }
    public DateOnly Fecha { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public CanalVenta Canal { get; set; } = CanalVenta.Otro;
    public decimal Paquetes { get; set; }
    public string? Documento { get; set; }
    public OrigenVenta Origen { get; set; } = OrigenVenta.Manual;

    /// <summary>true: se descontó del stock de producto terminado (despacho real). false: solo historial.</summary>
    public bool AfectaStock { get; set; }
    public int? AlmacenId { get; set; }
    public Almacen? Almacen { get; set; }

    public bool Anulada { get; set; }
    public string? MotivoAnulacion { get; set; }

    public DateTime CreadoEn { get; set; }
    public string CreadoPor { get; set; } = string.Empty;
}

/// <summary>Stock de producto terminado (en paquetes) por almacén de producto terminado.</summary>
public class StockProducto : IEntidadEmpresa
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int AlmacenId { get; set; }
    public Almacen? Almacen { get; set; }
    public decimal Paquetes { get; set; }
    public DateTime? UltimoMovimiento { get; set; }
}

/// <summary>Movimiento de producto terminado. Igual que los de insumos: no se edita ni se borra.</summary>
public class MovimientoProducto : IEntidadEmpresa
{
    public long Id { get; set; }
    public int EmpresaId { get; set; }
    public DateTime Fecha { get; set; }
    public TipoMovimientoProducto Tipo { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int AlmacenId { get; set; }
    public Almacen? Almacen { get; set; }
    public decimal Paquetes { get; set; }
    public decimal SaldoAlmacen { get; set; }
    public string? Documento { get; set; }
    public string? Motivo { get; set; }
    public string? Referencia { get; set; }
    public long? VentaId { get; set; }
    public int? ParteProduccionId { get; set; }
    public long? RevierteId { get; set; }
    public bool Revertido { get; set; }
    public string Usuario { get; set; } = string.Empty;
}

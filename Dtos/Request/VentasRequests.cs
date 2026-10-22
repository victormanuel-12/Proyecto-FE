using System.ComponentModel.DataAnnotations;
using SPIn.Models;

namespace SPIn.Dtos.Request;

public class FiltroVentaRequest
{
    public int? ProductoId { get; set; }

    [EnumDataType(typeof(CanalVenta), ErrorMessage = "El canal no es válido.")]
    public CanalVenta? Canal { get; set; }

    [EnumDataType(typeof(OrigenVenta), ErrorMessage = "El origen no es válido.")]
    public OrigenVenta? Origen { get; set; }

    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }

    /// <summary>true: incluir anuladas.</summary>
    public bool? Anuladas { get; set; }

    [Range(1, 100000)]
    public int Pagina { get; set; } = 1;

    [Range(5, 100)]
    public int TamanoPagina { get; set; } = 20;
}

public class VentaRequest
{
    public DateOnly Fecha { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el producto.")]
    public int ProductoId { get; set; }

    [EnumDataType(typeof(CanalVenta), ErrorMessage = "Seleccione el canal.")]
    public CanalVenta Canal { get; set; }

    [Range(typeof(decimal), "0.01", "100000000", ErrorMessage = "La cantidad debe ser mayor que 0.")]
    public decimal Cantidad { get; set; }

    /// <summary>true: la cantidad está en botellas y se convierte a paquetes.</summary>
    public bool EnBotellas { get; set; }

    [StringLength(60, ErrorMessage = "El documento admite hasta 60 caracteres.")]
    public string? Documento { get; set; }

    /// <summary>true: descontar del stock de producto terminado del almacén indicado.</summary>
    public bool AfectaStock { get; set; }

    public int? AlmacenId { get; set; }
}

public class AnularVentaRequest
{
    [Required(ErrorMessage = "Indique el motivo de la anulación.")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 200 caracteres.")]
    public string Motivo { get; set; } = string.Empty;
}

public class FiltroStockProductoRequest : FiltroListadoRequest
{
    public int? AlmacenId { get; set; }
}

public class MovimientoProductoRequest
{
    [EnumDataType(typeof(TipoMovimientoProducto), ErrorMessage = "Seleccione el tipo de movimiento.")]
    public TipoMovimientoProducto Tipo { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el almacén.")]
    public int AlmacenId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el producto.")]
    public int ProductoId { get; set; }

    [Range(typeof(decimal), "0.01", "100000000", ErrorMessage = "La cantidad debe ser mayor que 0.")]
    public decimal Paquetes { get; set; }

    [StringLength(60, ErrorMessage = "El documento admite hasta 60 caracteres.")]
    public string? Documento { get; set; }

    [StringLength(200, ErrorMessage = "El motivo admite hasta 200 caracteres.")]
    public string? Motivo { get; set; }
}

using System.ComponentModel.DataAnnotations;
using SPIn.Models;

namespace SPIn.Dtos.Request;

public class MotivoRequest
{
    [Required(ErrorMessage = "Indique el motivo.")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 200 caracteres.")]
    public string Motivo { get; set; } = string.Empty;
}

// ------------------------------------------------------------------ producción

public class FiltroPlanRequest
{
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public int? LineaId { get; set; }
    public int? ProductoId { get; set; }

    [EnumDataType(typeof(EstadoPlan), ErrorMessage = "El estado no es válido.")]
    public EstadoPlan? Estado { get; set; }

    [Range(1, 100000)] public int Pagina { get; set; } = 1;
    [Range(5, 100)] public int TamanoPagina { get; set; } = 20;
}

public class PlanRequest
{
    public DateOnly Fecha { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la línea.")]
    public int LineaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el producto.")]
    public int ProductoId { get; set; }

    [Range(typeof(decimal), "1", "10000000", ErrorMessage = "Los paquetes deben ser mayores que 0.")]
    public decimal Paquetes { get; set; }

    [StringLength(200, ErrorMessage = "El comentario admite hasta 200 caracteres.")]
    public string? Comentario { get; set; }
}

public class FiltroParteRequest
{
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public int? LineaId { get; set; }
    public int? ProductoId { get; set; }
    /// <summary>true: incluir anulados.</summary>
    public bool? Anulados { get; set; }

    [Range(1, 100000)] public int Pagina { get; set; } = 1;
    [Range(5, 100)] public int TamanoPagina { get; set; } = 20;
}

public class ParteRequest
{
    public DateOnly Fecha { get; set; }

    [EnumDataType(typeof(Turno), ErrorMessage = "Seleccione el turno.")]
    public Turno Turno { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la línea.")]
    public int LineaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el producto.")]
    public int ProductoId { get; set; }

    [Range(typeof(decimal), "1", "10000000", ErrorMessage = "Los paquetes deben ser mayores que 0.")]
    public decimal Paquetes { get; set; }

    [Range(typeof(decimal), "0.1", "24", ErrorMessage = "Las horas deben estar entre 0.1 y 24.")]
    public decimal? HorasOperacion { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el almacén de insumos.")]
    public int AlmacenInsumosId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el almacén de producto terminado.")]
    public int AlmacenProductoId { get; set; }

    public int? PlanId { get; set; }

    [StringLength(200, ErrorMessage = "La observación admite hasta 200 caracteres.")]
    public string? Observacion { get; set; }
}

/// <summary>Para calcular, antes de registrar, cuánto insumo se necesita y si alcanza.</summary>
public class RequerimientoRequest
{
    public DateOnly? Fecha { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el producto.")]
    public int ProductoId { get; set; }

    [Range(typeof(decimal), "1", "10000000", ErrorMessage = "Los paquetes deben ser mayores que 0.")]
    public decimal Paquetes { get; set; }

    /// <summary>Almacén de insumos donde se mira el stock. Vacío: todos los almacenes de insumos.</summary>
    public int? AlmacenInsumosId { get; set; }
}

// ------------------------------------------------------------------ pedidos

public class FiltroPedidoRequest
{
    [StringLength(100)]
    public string? Buscar { get; set; }
    public int? ProveedorId { get; set; }
    public int? InsumoId { get; set; }

    /// <summary>"encamino" (pendientes y parciales), "atrasados", "recibidos", "cancelados" o vacío (todos).</summary>
    [RegularExpression("^(encamino|atrasados|recibidos|cancelados)?$", ErrorMessage = "El estado no es válido.")]
    public string? Estado { get; set; }

    [Range(1, 100000)] public int Pagina { get; set; } = 1;
    [Range(5, 100)] public int TamanoPagina { get; set; } = 20;
}

public class PedidoDetalleRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el insumo.")]
    public int InsumoId { get; set; }

    [Range(typeof(decimal), "0.0001", "1000000000", ErrorMessage = "La cantidad debe ser mayor que 0.")]
    public decimal Cantidad { get; set; }

    [Range(typeof(decimal), "0", "100000000", ErrorMessage = "El precio no puede ser negativo.")]
    public decimal PrecioUnitario { get; set; }
}

public class PedidoRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el proveedor.")]
    public int ProveedorId { get; set; }

    public DateOnly FechaPedido { get; set; }

    /// <summary>Si no se indica, se calcula con el mayor tiempo de entrega de las condiciones de compra.</summary>
    public DateOnly? FechaEstimada { get; set; }

    [EnumDataType(typeof(Moneda), ErrorMessage = "Seleccione la moneda.")]
    public Moneda Moneda { get; set; } = Moneda.PEN;

    [StringLength(60, ErrorMessage = "El documento admite hasta 60 caracteres.")]
    public string? DocumentoProveedor { get; set; }

    [StringLength(200, ErrorMessage = "La observación admite hasta 200 caracteres.")]
    public string? Observacion { get; set; }

    [MinLength(1, ErrorMessage = "Agregue al menos un insumo.")]
    public List<PedidoDetalleRequest> Detalles { get; set; } = [];
}

public class RecepcionDetalleRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Línea no válida.")]
    public int PedidoDetalleId { get; set; }

    [Range(typeof(decimal), "0", "1000000000", ErrorMessage = "La cantidad no puede ser negativa.")]
    public decimal Cantidad { get; set; }
}

public class RecepcionRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el almacén.")]
    public int AlmacenId { get; set; }

    [Required(ErrorMessage = "Indique la guía de remisión.")]
    [StringLength(60, MinimumLength = 3, ErrorMessage = "La guía debe tener entre 3 y 60 caracteres.")]
    public string Guia { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "La observación admite hasta 200 caracteres.")]
    public string? Observacion { get; set; }

    [MinLength(1, ErrorMessage = "Indique lo recibido.")]
    public List<RecepcionDetalleRequest> Detalles { get; set; } = [];
}

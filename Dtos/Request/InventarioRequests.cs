using System.ComponentModel.DataAnnotations;
using SPIn.Models;

namespace SPIn.Dtos.Request;

public class FiltroStockRequest : FiltroListadoRequest
{
    public int? AlmacenId { get; set; }
    public int? CategoriaId { get; set; }
    public bool? Criticos { get; set; }

    /// <summary>"bajo" (bajo el mínimo o sin stock), "sobre" (sobre el máximo), "sin" (sin stock).</summary>
    [RegularExpression("^(bajo|sobre|sin)?$", ErrorMessage = "La situación no es válida.")]
    public string? Situacion { get; set; }
}

public class FiltroMovimientoRequest
{
    [StringLength(100)]
    public string? Buscar { get; set; }
    public int? InsumoId { get; set; }
    public int? AlmacenId { get; set; }

    [EnumDataType(typeof(TipoMovimiento), ErrorMessage = "El tipo de movimiento no es válido.")]
    public TipoMovimiento? Tipo { get; set; }

    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }

    [Range(1, 100000)]
    public int Pagina { get; set; } = 1;

    [Range(5, 100)]
    public int TamanoPagina { get; set; } = 20;
}

public class KardexRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el insumo.")]
    public int InsumoId { get; set; }

    /// <summary>Vacío = todos los almacenes.</summary>
    public int? AlmacenId { get; set; }

    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }

    [Range(1, 100000)]
    public int Pagina { get; set; } = 1;

    [Range(10, 500)]
    public int TamanoPagina { get; set; } = 50;
}

public class MovimientoRequest
{
    [EnumDataType(typeof(TipoMovimiento), ErrorMessage = "Seleccione el tipo de movimiento.")]
    public TipoMovimiento Tipo { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el almacén.")]
    public int AlmacenId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el insumo.")]
    public int InsumoId { get; set; }

    [Range(typeof(decimal), "0.000001", "1000000000000", ErrorMessage = "La cantidad debe ser mayor que 0.")]
    public decimal Cantidad { get; set; }

    /// <summary>true: la cantidad está en unidad de compra (cajas, rollos) y se convierte con el factor del insumo.</summary>
    public bool EnUnidadCompra { get; set; }

    [StringLength(60, ErrorMessage = "El documento admite hasta 60 caracteres.")]
    public string? Documento { get; set; }

    [StringLength(200, ErrorMessage = "El motivo admite hasta 200 caracteres.")]
    public string? Motivo { get; set; }
}

public class RevertirMovimientoRequest
{
    [Required(ErrorMessage = "Indique el motivo de la reversión.")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 200 caracteres.")]
    public string Motivo { get; set; } = string.Empty;
}

public class FiltroConteoRequest
{
    public int? AlmacenId { get; set; }

    [EnumDataType(typeof(EstadoConteo))]
    public EstadoConteo? Estado { get; set; }

    [Range(1, 100000)]
    public int Pagina { get; set; } = 1;

    [Range(5, 100)]
    public int TamanoPagina { get; set; } = 10;
}

public class ConteoCrearRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el almacén.")]
    public int AlmacenId { get; set; }

    /// <summary>Vacío = todos los insumos activos.</summary>
    public int? CategoriaId { get; set; }

    /// <summary>true: solo insumos que hoy tienen stock en el almacén.</summary>
    public bool SoloConStock { get; set; }

    [StringLength(200, ErrorMessage = "La observación admite hasta 200 caracteres.")]
    public string? Observacion { get; set; }
}

public class ConteoCantidadRequest
{
    [Range(1, int.MaxValue)]
    public int InsumoId { get; set; }

    [Range(typeof(decimal), "0", "1000000000000", ErrorMessage = "La cantidad contada no puede ser negativa.")]
    public decimal? CantidadContada { get; set; }
}

public class ConteoGuardarRequest
{
    [Required]
    public List<ConteoCantidadRequest> Cantidades { get; set; } = [];
}

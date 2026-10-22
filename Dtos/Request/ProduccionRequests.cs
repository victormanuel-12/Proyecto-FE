using System.ComponentModel.DataAnnotations;
using SPIn.Models;

namespace SPIn.Dtos.Request;

public class FiltroProductoRequest : FiltroListadoRequest
{
    public int? FormatoId { get; set; }

    [EnumDataType(typeof(TipoEnvase), ErrorMessage = "El tipo de envase no es válido.")]
    public TipoEnvase? TipoEnvase { get; set; }

    /// <summary>true: solo productos sin receta vigente.</summary>
    public bool? SinReceta { get; set; }
}

public class ProductoRequest
{
    [Required(ErrorMessage = "El código SKU es obligatorio.")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "El código debe tener entre 2 y 20 caracteres.")]
    [RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "El código solo admite letras, números y guiones.")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 120 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(40, ErrorMessage = "El sabor admite hasta 40 caracteres.")]
    public string? Sabor { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el formato.")]
    public int FormatoId { get; set; }

    [Range(1, 1000, ErrorMessage = "Las botellas por paquete deben estar entre 1 y 1 000.")]
    public int UnidadesPorPaquete { get; set; }

    [Range(1, 1000, ErrorMessage = "Los paquetes por pallet deben estar entre 1 y 1 000.")]
    public int PaquetesPorPallet { get; set; }
}

public class RecetaDetalleRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el insumo.")]
    public int InsumoId { get; set; }

    [Range(typeof(decimal), "0.000001", "1000000", ErrorMessage = "La cantidad debe ser mayor que 0.")]
    public decimal Cantidad { get; set; }

    [EnumDataType(typeof(NivelReceta), ErrorMessage = "Seleccione por botella, paquete o pallet.")]
    public NivelReceta Nivel { get; set; }

    [Range(typeof(decimal), "0", "50", ErrorMessage = "La merma debe estar entre 0 % y 50 %.")]
    public decimal MermaPorcentaje { get; set; }
}

public class RecetaRequest
{
    public DateOnly VigenteDesde { get; set; }

    [StringLength(200, ErrorMessage = "El comentario admite hasta 200 caracteres.")]
    public string? Comentario { get; set; }

    [Required(ErrorMessage = "Agregue al menos un insumo.")]
    [MinLength(1, ErrorMessage = "Agregue al menos un insumo.")]
    public List<RecetaDetalleRequest> Detalles { get; set; } = [];
}

public enum UnidadExplosion
{
    Botellas = 1,
    Paquetes = 2,
    Pallets = 3
}

public class ExplosionRequest
{
    [Range(typeof(decimal), "0.01", "100000000", ErrorMessage = "Ingrese una cantidad mayor que 0.")]
    public decimal Cantidad { get; set; }

    [EnumDataType(typeof(UnidadExplosion), ErrorMessage = "Elija botellas, paquetes o pallets.")]
    public UnidadExplosion Unidad { get; set; } = UnidadExplosion.Botellas;

    /// <summary>Fecha de la producción; define qué versión de la receta se usa. Vacío = hoy.</summary>
    public DateOnly? Fecha { get; set; }
}

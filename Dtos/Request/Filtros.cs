using System.ComponentModel.DataAnnotations;
using SPIn.Models;

namespace SPIn.Dtos.Request;

public class FiltroListadoRequest
{
    [StringLength(100, ErrorMessage = "La búsqueda admite hasta 100 caracteres.")]
    public string? Buscar { get; set; }

    [RegularExpression("^(activos|inactivos)?$", ErrorMessage = "El estado debe ser 'activos' o 'inactivos'.")]
    public string? Estado { get; set; }

    [Range(1, 100000, ErrorMessage = "La página no es válida.")]
    public int Pagina { get; set; } = 1;

    [Range(5, 100, ErrorMessage = "El tamaño de página debe estar entre 5 y 100.")]
    public int TamanoPagina { get; set; } = 10;
}

public class FiltroAlmacenRequest : FiltroListadoRequest
{
    public int? PlantaId { get; set; }

    [EnumDataType(typeof(TipoAlmacen), ErrorMessage = "El tipo de almacén no es válido.")]
    public TipoAlmacen? Tipo { get; set; }
}

public class FiltroLineaRequest : FiltroListadoRequest
{
    public int? PlantaId { get; set; }
}

public class FiltroUsuarioRequest : FiltroListadoRequest
{
    [StringLength(30)]
    public string? Rol { get; set; }

    public int? EmpresaId { get; set; }
}

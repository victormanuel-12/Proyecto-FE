using System.ComponentModel.DataAnnotations;
using SPIn.Models;

namespace SPIn.Dtos.Request;

public class CambiarEstadoRequest
{
    public bool Activo { get; set; }
}

public class EmpresaRequest
{
    [Required(ErrorMessage = "El RUC es obligatorio.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "El RUC debe tener 11 dígitos.")]
    public string Ruc { get; set; } = string.Empty;

    [Required(ErrorMessage = "La razón social es obligatoria.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "La razón social debe tener entre 3 y 150 caracteres.")]
    public string RazonSocial { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "El nombre comercial admite hasta 100 caracteres.")]
    public string? NombreComercial { get; set; }
}

public class PlantaRequest
{
    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(10, MinimumLength = 2, ErrorMessage = "El código debe tener entre 2 y 10 caracteres.")]
    [RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "El código solo admite letras, números y guiones.")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "La dirección admite hasta 200 caracteres.")]
    public string? Direccion { get; set; }
}

public class AlmacenRequest
{
    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(10, MinimumLength = 2, ErrorMessage = "El código debe tener entre 2 y 10 caracteres.")]
    [RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "El código solo admite letras, números y guiones.")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la planta.")]
    public int PlantaId { get; set; }

    [EnumDataType(typeof(TipoAlmacen), ErrorMessage = "Seleccione el tipo de almacén.")]
    public TipoAlmacen Tipo { get; set; }
}

public class LineaRequest
{
    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(10, MinimumLength = 2, ErrorMessage = "El código debe tener entre 2 y 10 caracteres.")]
    [RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "El código solo admite letras, números y guiones.")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la planta.")]
    public int PlantaId { get; set; }

    [EnumDataType(typeof(TipoEnvase), ErrorMessage = "Seleccione el tipo de envase.")]
    public TipoEnvase TipoEnvase { get; set; }

    [Range(1, 200000, ErrorMessage = "La velocidad debe estar entre 1 y 200 000 botellas por hora.")]
    public int VelocidadNominal { get; set; }

    [Required(ErrorMessage = "Seleccione al menos un formato.")]
    [MinLength(1, ErrorMessage = "Seleccione al menos un formato.")]
    public List<int> FormatoIds { get; set; } = [];
}

public class UsuarioCrearRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 120 caracteres.")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(256, ErrorMessage = "El correo admite hasta 256 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione el rol.")]
    public string Rol { get; set; } = string.Empty;

    public int? EmpresaId { get; set; }

    [Required(ErrorMessage = "La contraseña temporal es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string ClaveTemporal { get; set; } = string.Empty;
}

public class UsuarioActualizarRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 120 caracteres.")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione el rol.")]
    public string Rol { get; set; } = string.Empty;
}

public class RestablecerClaveRequest
{
    [Required(ErrorMessage = "La contraseña temporal es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string ClaveTemporal { get; set; } = string.Empty;
}

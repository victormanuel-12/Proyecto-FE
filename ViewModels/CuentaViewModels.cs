using System.ComponentModel.DataAnnotations;

namespace SPIn.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingrese su correo.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [Display(Name = "Correo")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese su contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Clave { get; set; } = string.Empty;

    [Display(Name = "Mantener la sesión iniciada")]
    public bool Recordarme { get; set; }

    public string? ReturnUrl { get; set; }
}

public class CambiarClaveViewModel
{
    [Required(ErrorMessage = "Ingrese su contraseña actual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña actual")]
    public string ClaveActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese la nueva contraseña.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string ClaveNueva { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme la nueva contraseña.")]
    [Compare(nameof(ClaveNueva), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar nueva contraseña")]
    public string ConfirmarClave { get; set; } = string.Empty;
}

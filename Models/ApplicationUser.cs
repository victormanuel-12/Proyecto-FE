using Microsoft.AspNetCore.Identity;

namespace SPIn.Models;

public class ApplicationUser : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;
    public int EmpresaId { get; set; }
    public Empresa? Empresa { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreadoEn { get; set; }
}

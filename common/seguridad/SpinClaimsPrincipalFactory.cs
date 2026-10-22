using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SPIn.Data;
using SPIn.Models;

namespace SPIn.Common.Seguridad;

/// <summary>Agrega a la cookie de sesión la empresa y el nombre del usuario.</summary>
public class SpinClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> options,
    ApplicationDbContext db)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        var empresa = await db.Empresas.AsNoTracking()
            .Where(e => e.Id == user.EmpresaId)
            .Select(e => e.NombreComercial ?? e.RazonSocial)
            .FirstOrDefaultAsync();

        identity.AddClaim(new Claim(ClaimsSpin.EmpresaId, user.EmpresaId.ToString()));
        identity.AddClaim(new Claim(ClaimsSpin.EmpresaNombre, empresa ?? string.Empty));
        identity.AddClaim(new Claim(ClaimsSpin.NombreCompleto, user.NombreCompleto));
        return identity;
    }
}

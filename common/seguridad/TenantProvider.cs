using System.Security.Claims;

namespace SPIn.Common.Seguridad;

/// <summary>Datos del usuario que hace la solicitud: su empresa, su id y sus roles.</summary>
public interface ITenantProvider
{
    int? EmpresaId { get; }
    string? UsuarioId { get; }
    string? UsuarioNombre { get; }
    bool EsSuperAdmin { get; }
}

public class TenantProvider(IHttpContextAccessor accessor) : ITenantProvider
{
    private ClaimsPrincipal? Usuario => accessor.HttpContext?.User;
    private int? _empresaFija;
    private string? _usuarioFijo;

    /// <summary>
    /// Fija la empresa para trabajos sin solicitud HTTP (reentrenamiento programado, Entrega 8).
    /// Solo lo usa el servicio en segundo plano, dentro de su propio scope.
    /// </summary>
    public void Establecer(int empresaId, string usuario)
    {
        _empresaFija = empresaId;
        _usuarioFijo = usuario;
    }

    public int? EmpresaId => _empresaFija ??
        (int.TryParse(Usuario?.FindFirst(ClaimsSpin.EmpresaId)?.Value, out var id) ? id : null);

    public string? UsuarioId => Usuario?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public string? UsuarioNombre => _usuarioFijo ?? (Usuario?.Identity?.IsAuthenticated == true ? Usuario.Identity.Name : null);

    public bool EsSuperAdmin => Usuario?.IsInRole(Roles.SuperAdmin) ?? false;
}

/// <summary>Se usa fuera de una solicitud HTTP (migraciones y carga inicial).</summary>
public class TenantSistema : ITenantProvider
{
    public int? EmpresaId => null;
    public string? UsuarioId => null;
    public string? UsuarioNombre => "sistema";
    public bool EsSuperAdmin => false;
}

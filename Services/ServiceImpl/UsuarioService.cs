using Microsoft.AspNetCore.Identity;
using SPIn.Common.Excepciones;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;

namespace SPIn.Services.ServiceImpl;

/// <summary>
/// Gestión de usuarios.
/// - El SuperAdmin ve y gestiona usuarios de todas las empresas.
/// - El Administrador solo los de su empresa y no puede asignar ni modificar superadministradores.
/// - Nadie puede desactivarse ni cambiarse el rol a sí mismo (evita quedarse sin acceso).
/// </summary>
public class UsuarioService(
    UserManager<ApplicationUser> userManager,
    IUsuarioRepository repo,
    IEmpresaRepository empresas,
    ITenantProvider tenant,
    ILogger<UsuarioService> logger) : IUsuarioService
{
    public async Task<GenericResponse<ListadoResponse<UsuarioResponse>>> ListarAsync(FiltroUsuarioRequest filtro)
    {
        int? empresaId = tenant.EsSuperAdmin ? filtro.EmpresaId : tenant.EmpresaId;
        var resultado = await repo.ListarAsync(filtro, empresaId);
        foreach (var u in resultado.Items) Completar(u);
        return new GenericResponse<ListadoResponse<UsuarioResponse>>(resultado);
    }

    public async Task<GenericResponse<UsuarioResponse>> ObtenerAsync(string id)
    {
        await ObtenerGestionableAsync(id);
        return new GenericResponse<UsuarioResponse>(await ResumenAsync(id));
    }

    public async Task<GenericResponse<UsuarioResponse>> CrearAsync(UsuarioCrearRequest request)
    {
        ValidarRolAsignable(request.Rol);

        int empresaId;
        if (tenant.EsSuperAdmin)
        {
            empresaId = request.EmpresaId ?? throw new GenericException("Seleccione la empresa del usuario.");
            var empresa = await empresas.ObtenerAsync(empresaId) ?? throw new GenericException("La empresa seleccionada no existe.");
            if (!empresa.Activo) throw new GenericException("La empresa seleccionada está inactiva.");
        }
        else
        {
            empresaId = tenant.EmpresaId ?? throw GenericException.Prohibido("No se pudo determinar su empresa.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(email) is not null)
            throw GenericException.Conflicto("Ya existe un usuario con ese correo.");

        var usuario = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            NombreCompleto = request.NombreCompleto.Trim(),
            EmpresaId = empresaId,
            Activo = true,
            CreadoEn = DateTime.UtcNow
        };

        var creado = await userManager.CreateAsync(usuario, request.ClaveTemporal);
        if (!creado.Succeeded) throw new GenericException(Errores(creado));

        var conRol = await userManager.AddToRoleAsync(usuario, request.Rol);
        if (!conRol.Succeeded)
        {
            await userManager.DeleteAsync(usuario);
            throw new GenericException(Errores(conRol));
        }

        logger.LogInformation("Usuario {UsuarioId} creado con rol {Rol} por {Autor}", usuario.Id, request.Rol, tenant.UsuarioNombre);
        return new GenericResponse<UsuarioResponse>(true, await ResumenAsync(usuario.Id),
            "Usuario creado. Entréguele la contraseña temporal para su primer ingreso.");
    }

    public async Task<GenericResponse<UsuarioResponse>> ActualizarAsync(string id, UsuarioActualizarRequest request)
    {
        var usuario = await ObtenerGestionableAsync(id);
        ValidarRolAsignable(request.Rol);

        var rolesActuales = await userManager.GetRolesAsync(usuario);
        var cambiaRol = !(rolesActuales.Count == 1 && rolesActuales[0] == request.Rol);
        if (cambiaRol && id == tenant.UsuarioId)
            throw new GenericException("No puede cambiar su propio rol. Pídaselo a otro administrador.");

        usuario.NombreCompleto = request.NombreCompleto.Trim();
        var actualizado = await userManager.UpdateAsync(usuario);
        if (!actualizado.Succeeded) throw new GenericException(Errores(actualizado));

        if (cambiaRol)
        {
            if (rolesActuales.Count > 0)
            {
                var quitados = await userManager.RemoveFromRolesAsync(usuario, rolesActuales);
                if (!quitados.Succeeded) throw new GenericException(Errores(quitados));
            }
            var agregado = await userManager.AddToRoleAsync(usuario, request.Rol);
            if (!agregado.Succeeded) throw new GenericException(Errores(agregado));
            // Invalida la sesión abierta para que el nuevo rol se aplique en su próximo ingreso.
            await userManager.UpdateSecurityStampAsync(usuario);
            logger.LogInformation("Rol del usuario {UsuarioId} cambiado a {Rol} por {Autor}", id, request.Rol, tenant.UsuarioNombre);
        }

        return new GenericResponse<UsuarioResponse>(true, await ResumenAsync(id), "Usuario actualizado.");
    }

    public async Task<GenericResponse<UsuarioResponse>> CambiarEstadoAsync(string id, bool activo)
    {
        var usuario = await ObtenerGestionableAsync(id);
        if (!activo && id == tenant.UsuarioId)
            throw new GenericException("No puede desactivar su propio usuario.");

        usuario.Activo = activo;
        var resultado = await userManager.UpdateAsync(usuario);
        if (!resultado.Succeeded) throw new GenericException(Errores(resultado));
        if (!activo) await userManager.UpdateSecurityStampAsync(usuario);

        logger.LogInformation("Usuario {UsuarioId} {Estado} por {Autor}", id, activo ? "activado" : "desactivado", tenant.UsuarioNombre);
        return new GenericResponse<UsuarioResponse>(true, await ResumenAsync(id),
            activo ? "Usuario activado." : "Usuario desactivado. Su sesión se cerrará en un minuto como máximo.");
    }

    public async Task<GenericResponse<object>> RestablecerClaveAsync(string id, RestablecerClaveRequest request)
    {
        var usuario = await ObtenerGestionableAsync(id);

        var token = await userManager.GeneratePasswordResetTokenAsync(usuario);
        var resultado = await userManager.ResetPasswordAsync(usuario, token, request.ClaveTemporal);
        if (!resultado.Succeeded) throw new GenericException(Errores(resultado));

        await userManager.SetLockoutEndDateAsync(usuario, null);
        await userManager.ResetAccessFailedCountAsync(usuario);

        logger.LogInformation("Contraseña del usuario {UsuarioId} restablecida por {Autor}", id, tenant.UsuarioNombre);
        return new GenericResponse<object>(true, "Contraseña restablecida y usuario desbloqueado.");
    }

    // ---------------------------------------------------------------- auxiliares

    private async Task<ApplicationUser> ObtenerGestionableAsync(string id)
    {
        var usuario = await userManager.FindByIdAsync(id)
            ?? throw GenericException.NoEncontrado("El usuario no existe.");

        if (!tenant.EsSuperAdmin)
        {
            if (usuario.EmpresaId != tenant.EmpresaId)
                throw GenericException.NoEncontrado("El usuario no existe.");
            if (await userManager.IsInRoleAsync(usuario, Roles.SuperAdmin))
                throw GenericException.Prohibido("No tiene permiso para modificar a un superadministrador.");
        }
        return usuario;
    }

    private void ValidarRolAsignable(string rol)
    {
        if (!Roles.Asignables(tenant.EsSuperAdmin).Contains(rol))
            throw new GenericException("El rol seleccionado no es válido.");
    }

    private async Task<UsuarioResponse> ResumenAsync(string id)
    {
        var u = await repo.ObtenerResumenAsync(id) ?? throw GenericException.NoEncontrado("El usuario no existe.");
        Completar(u);
        return u;
    }

    private void Completar(UsuarioResponse u)
    {
        u.RolNombre = Roles.Etiqueta(u.Rol);
        u.Bloqueado = u.BloqueoHasta.HasValue && u.BloqueoHasta.Value > DateTimeOffset.UtcNow;
        u.EsUsuarioActual = u.Id == tenant.UsuarioId;
    }

    private static string Errores(IdentityResult resultado) =>
        string.Join(" ", resultado.Errors.Select(e => e.Description));
}

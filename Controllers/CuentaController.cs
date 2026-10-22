using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIn.Data;
using SPIn.Models;
using SPIn.ViewModels;

namespace SPIn.Controllers;

[Route("cuenta")]
public class CuentaController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db,
    ILogger<CuentaController> logger) : Controller
{
    private const string MensajeCredenciales = "Correo o contraseña incorrectos.";

    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return LocalRedirect(UrlSegura(returnUrl));
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        var usuario = await userManager.FindByEmailAsync(modelo.Email.Trim());
        if (usuario is null)
        {
            ModelState.AddModelError(string.Empty, MensajeCredenciales);
            return View(modelo);
        }

        var empresaActiva = await db.Empresas.AsNoTracking().AnyAsync(e => e.Id == usuario.EmpresaId && e.Activo);
        if (!usuario.Activo || !empresaActiva)
        {
            logger.LogWarning("Intento de ingreso de usuario inactivo {UsuarioId}", usuario.Id);
            ModelState.AddModelError(string.Empty, "Su usuario está inactivo. Comuníquese con el administrador.");
            return View(modelo);
        }

        var resultado = await signInManager.PasswordSignInAsync(usuario, modelo.Clave, modelo.Recordarme, lockoutOnFailure: true);
        if (resultado.Succeeded)
        {
            logger.LogInformation("Ingreso de {UsuarioId}", usuario.Id);
            return LocalRedirect(UrlSegura(modelo.ReturnUrl));
        }

        if (resultado.IsLockedOut)
        {
            logger.LogWarning("Usuario {UsuarioId} bloqueado por intentos fallidos", usuario.Id);
            ModelState.AddModelError(string.Empty, "Su usuario se bloqueó por varios intentos fallidos. Espere 10 minutos o pida al administrador que restablezca su contraseña.");
            return View(modelo);
        }

        ModelState.AddModelError(string.Empty, MensajeCredenciales);
        return View(modelo);
    }

    [Authorize]
    [HttpPost("salir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet("acceso-denegado")]
    public IActionResult AccesoDenegado() => View();

    [Authorize]
    [HttpGet("cambiar-clave")]
    public IActionResult CambiarClave() => View(new CambiarClaveViewModel());

    [Authorize]
    [HttpPost("cambiar-clave")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarClave(CambiarClaveViewModel modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        var usuario = await userManager.GetUserAsync(User);
        if (usuario is null) return RedirectToAction(nameof(Login));

        var resultado = await userManager.ChangePasswordAsync(usuario, modelo.ClaveActual, modelo.ClaveNueva);
        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                var campo = error.Code == nameof(IdentityErrorDescriber.PasswordMismatch)
                    ? nameof(CambiarClaveViewModel.ClaveActual)
                    : nameof(CambiarClaveViewModel.ClaveNueva);
                ModelState.AddModelError(campo, error.Description);
            }
            return View(modelo);
        }

        await signInManager.RefreshSignInAsync(usuario);
        TempData["Mensaje"] = "Su contraseña se cambió correctamente.";
        return RedirectToAction(nameof(CambiarClave));
    }

    private string UrlSegura(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
}

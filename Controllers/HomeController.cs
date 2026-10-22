using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SPIn.Controllers;

public class HomeController : Controller
{
    [Authorize]
    [HttpGet("/")]
    public IActionResult Index() => View();

    [AllowAnonymous]
    [HttpGet("/error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ViewData["Codigo"] = 500;
        ViewData["IdSolicitud"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return View("Estado");
    }

    [AllowAnonymous]
    [HttpGet("/estado/{codigo:int}")]
    public IActionResult Estado(int codigo)
    {
        ViewData["Codigo"] = codigo;
        return View("Estado");
    }
}

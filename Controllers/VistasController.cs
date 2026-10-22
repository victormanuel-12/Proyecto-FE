using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIn.Common.Seguridad;

namespace SPIn.Controllers;

// Controladores de vistas: solo devuelven la página. Los datos se cargan con fetch desde /api/...

[Authorize(Roles = Roles.Administracion)]
[Route("plantas")]
public class PlantaController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}

[Authorize(Roles = Roles.Administracion)]
[Route("almacenes")]
public class AlmacenController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}

[Authorize(Roles = Roles.Administracion)]
[Route("lineas")]
public class LineaController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}

[Authorize(Roles = Roles.Administracion)]
[Route("usuarios")]
public class UsuarioController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}

[Authorize(Roles = Roles.SuperAdmin)]
[Route("empresas")]
public class EmpresaController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}

// ---------------- Entrega 2: catálogo de insumos y abastecimiento (todos los roles pueden consultar)

[Authorize]
[Route("insumos")]
public class InsumoController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}

[Authorize]
[Route("categorias-unidades")]
public class CatalogoController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}

[Authorize]
[Route("proveedores")]
public class ProveedorController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}

// ---------------- Entrega 3: productos y recetas (todos consultan; editan SuperAdmin, Administrador y Planificador)

[Authorize]
[Route("productos")]
public class ProductoController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}

// ---------------- Entrega 4: stock, movimientos, kardex y conteos (todos consultan; registra el almacén)

[Authorize]
[Route("stock")]
public class StockController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}

[Authorize]
[Route("movimientos")]
public class MovimientoController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}

[Authorize]
[Route("kardex")]
public class KardexController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}

[Authorize]
[Route("conteos")]
public class ConteoController(SPIn.Services.IConteoService conteos) : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpGet("{id:int}")]
    public IActionResult Detalle(int id)
    {
        ViewData["ConteoId"] = id;
        return View();
    }

    /// <summary>Hoja de conteo para imprimir y llenar a mano en el almacén.</summary>
    [HttpGet("{id:int}/hoja")]
    public async Task<IActionResult> Hoja(int id)
    {
        try
        {
            var conteo = await conteos.ObtenerAsync(id);
            return View(conteo.Data);
        }
        catch (SPIn.Common.Excepciones.GenericException)
        {
            return NotFound();
        }
    }
}

// ---------------- Entrega 5: ventas, producto terminado e importación

[Authorize]
[Route("ventas")]
public class VentaController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}

[Authorize]
[Route("producto-terminado")]
public class ProductoTerminadoController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}

[Authorize(Roles = Roles.Administracion)]
[Route("importar")]
public class ImportacionController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}

// ---------------- Entrega 6 y 7: producción y pedidos en camino

[Authorize]
[Route("produccion")]
public class ProduccionController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}

[Authorize]
[Route("pedidos")]
public class PedidoController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}

// ---------------- Entrega 8: motor predictivo y configuración

[Authorize]
[Route("pronostico")]
public class PronosticoController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}

[Authorize]
[Route("reposicion")]
public class ReposicionController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}

[Authorize(Roles = Roles.Administracion)]
[Route("configuracion")]
public class ConfiguracionController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}

// ---------------- Entrega 9: alertas

[Authorize]
[Route("alertas")]
public class AlertaController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}

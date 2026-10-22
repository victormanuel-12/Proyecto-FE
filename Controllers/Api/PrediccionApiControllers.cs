using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Services;

namespace SPIn.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/pronostico")]
public class PronosticoApiController(IPrediccionService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GenericResponse<PanelPronosticoResponse>>> Panel() => Ok(await service.PanelAsync());

    [HttpGet("productos/{productoId:int}")]
    public async Task<ActionResult<GenericResponse<DetallePronosticoResponse>>> Detalle(int productoId) => Ok(await service.DetalleAsync(productoId));

    [HttpGet("ejecuciones")]
    public async Task<ActionResult<GenericResponse<List<EjecucionResponse>>>> Ejecuciones() => Ok(await service.EjecucionesAsync());

    /// <summary>Botón «Recalcular pronóstico».</summary>
    [HttpPost("recalcular"), Authorize(Roles = Roles.Pronostico)]
    public async Task<ActionResult<GenericResponse<EjecucionResponse>>> Recalcular()
    {
        var r = await service.EntrenarAsync(OrigenEjecucion.Manual);
        return r.Success ? Ok(r) : StatusCode(StatusCodes.Status500InternalServerError, r);
    }
}

[ApiController]
[Authorize]
[Route("api/reposicion")]
public class ReposicionApiController(IReposicionService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GenericResponse<ReposicionPaginaResponse>>> Listar([FromQuery] FiltroReposicionRequest filtro) =>
        Ok(await service.ListarAsync(filtro));

    [HttpGet("{insumoId:int}")]
    public async Task<ActionResult<GenericResponse<ReposicionInsumoResponse>>> Detalle(int insumoId) => Ok(await service.DetalleAsync(insumoId));
}

[ApiController]
[Authorize]
[Route("api/configuracion")]
public class ConfiguracionApiController(IPrediccionService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GenericResponse<ConfiguracionResponse>>> Obtener() => Ok(await service.ConfiguracionAsync());

    [HttpPut, Authorize(Roles = Roles.Administracion)]
    public async Task<ActionResult<GenericResponse<ConfiguracionResponse>>> Guardar([FromBody] ConfiguracionRequest request) =>
        Ok(await service.GuardarConfiguracionAsync(request));
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Services;

namespace SPIn.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/alertas")]
public class AlertaApiController(IAlertaService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GenericResponse<AlertasPaginaResponse>>> Listar([FromQuery] FiltroAlertaRequest filtro) => Ok(await service.ListarAsync(filtro));

    /// <summary>Conteo por severidad para la campana de la cabecera.</summary>
    [HttpGet("resumen")]
    public async Task<ActionResult<GenericResponse<ResumenAlertasResponse>>> Resumen() => Ok(await service.ResumenAsync());

    [HttpPost("{id:int}/descartar")]
    public async Task<ActionResult<GenericResponse<AlertaResponse>>> Descartar(int id, [FromBody] DescartarAlertaRequest request) =>
        Ok(await service.DescartarAsync(id, request));

    [HttpPost("{id:int}/reactivar")]
    public async Task<ActionResult<GenericResponse<AlertaResponse>>> Reactivar(int id) => Ok(await service.ReactivarAsync(id));
}

[ApiController]
[Authorize]
[Route("api/panel")]
public class PanelApiController(IPanelService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GenericResponse<PanelResponse>>> Panel() => Ok(await service.PanelAsync());
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIn.Dtos.Response;
using SPIn.Services;

namespace SPIn.Controllers.Api;

[ApiController]
[Route("api/inicio")]
[Authorize]
public class InicioApiController(IInicioService service) : ControllerBase
{
    [HttpGet("resumen")]
    public async Task<ActionResult<GenericResponse<ResumenInicioResponse>>> Resumen() =>
        Ok(await service.ResumenAsync());
}

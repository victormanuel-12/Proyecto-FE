using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Services;

namespace SPIn.Controllers.Api;

[ApiController]
[Route("api/lineas")]
[Authorize(Roles = Roles.Administracion)]
public class LineaApiController(ILineaService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GenericResponse<ListadoResponse<LineaResponse>>>> Listar([FromQuery] FiltroLineaRequest filtro) =>
        Ok(await service.ListarAsync(filtro));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GenericResponse<LineaResponse>>> Obtener(int id) =>
        Ok(await service.ObtenerAsync(id));

    [HttpPost]
    public async Task<ActionResult<GenericResponse<LineaResponse>>> Crear([FromBody] LineaRequest request) =>
        StatusCode(StatusCodes.Status201Created, await service.CrearAsync(request));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<GenericResponse<LineaResponse>>> Actualizar(int id, [FromBody] LineaRequest request) =>
        Ok(await service.ActualizarAsync(id, request));

    [HttpPatch("{id:int}/estado")]
    public async Task<ActionResult<GenericResponse<LineaResponse>>> CambiarEstado(int id, [FromBody] CambiarEstadoRequest request) =>
        Ok(await service.CambiarEstadoAsync(id, request.Activo));

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<GenericResponse<object>>> Eliminar(int id) =>
        Ok(await service.EliminarAsync(id));
}

[ApiController]
[Route("api/formatos")]
[Authorize]
public class FormatoApiController(ILineaService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GenericResponse<List<FormatoResponse>>>> Listar() =>
        Ok(await service.FormatosAsync());
}

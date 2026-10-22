using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Services;

namespace SPIn.Controllers.Api;

[ApiController]
[Route("api/plantas")]
[Authorize]
public class PlantaApiController(IPlantaService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = Roles.Administracion)]
    public async Task<ActionResult<GenericResponse<ListadoResponse<PlantaResponse>>>> Listar([FromQuery] FiltroListadoRequest filtro) =>
        Ok(await service.ListarAsync(filtro));

    /// <summary>Lista corta para selectores. Disponible para cualquier usuario autenticado de la empresa.</summary>
    [HttpGet("opciones")]
    public async Task<ActionResult<GenericResponse<List<OpcionResponse>>>> Opciones([FromQuery] bool soloActivas = true) =>
        Ok(await service.OpcionesAsync(soloActivas));

    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.Administracion)]
    public async Task<ActionResult<GenericResponse<PlantaResponse>>> Obtener(int id) =>
        Ok(await service.ObtenerAsync(id));

    [HttpPost]
    [Authorize(Roles = Roles.Administracion)]
    public async Task<ActionResult<GenericResponse<PlantaResponse>>> Crear([FromBody] PlantaRequest request) =>
        StatusCode(StatusCodes.Status201Created, await service.CrearAsync(request));

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Administracion)]
    public async Task<ActionResult<GenericResponse<PlantaResponse>>> Actualizar(int id, [FromBody] PlantaRequest request) =>
        Ok(await service.ActualizarAsync(id, request));

    [HttpPatch("{id:int}/estado")]
    [Authorize(Roles = Roles.Administracion)]
    public async Task<ActionResult<GenericResponse<PlantaResponse>>> CambiarEstado(int id, [FromBody] CambiarEstadoRequest request) =>
        Ok(await service.CambiarEstadoAsync(id, request.Activo));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administracion)]
    public async Task<ActionResult<GenericResponse<object>>> Eliminar(int id) =>
        Ok(await service.EliminarAsync(id));
}

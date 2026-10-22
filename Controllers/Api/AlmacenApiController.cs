using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Services;

namespace SPIn.Controllers.Api;

[ApiController]
[Route("api/almacenes")]
[Authorize(Roles = Roles.Administracion)]
public class AlmacenApiController(IAlmacenService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GenericResponse<ListadoResponse<AlmacenResponse>>>> Listar([FromQuery] FiltroAlmacenRequest filtro) =>
        Ok(await service.ListarAsync(filtro));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GenericResponse<AlmacenResponse>>> Obtener(int id) =>
        Ok(await service.ObtenerAsync(id));

    [HttpPost]
    public async Task<ActionResult<GenericResponse<AlmacenResponse>>> Crear([FromBody] AlmacenRequest request) =>
        StatusCode(StatusCodes.Status201Created, await service.CrearAsync(request));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<GenericResponse<AlmacenResponse>>> Actualizar(int id, [FromBody] AlmacenRequest request) =>
        Ok(await service.ActualizarAsync(id, request));

    [HttpPatch("{id:int}/estado")]
    public async Task<ActionResult<GenericResponse<AlmacenResponse>>> CambiarEstado(int id, [FromBody] CambiarEstadoRequest request) =>
        Ok(await service.CambiarEstadoAsync(id, request.Activo));

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<GenericResponse<object>>> Eliminar(int id) =>
        Ok(await service.EliminarAsync(id));
}

/// <summary>Lista corta de almacenes para selectores; disponible para cualquier usuario de la empresa.</summary>
[ApiController]
[Route("api/almacenes/opciones")]
[Authorize]
public class AlmacenOpcionesApiController(IAlmacenService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GenericResponse<List<OpcionResponse>>>> Opciones(
        [FromQuery] bool soloActivos = false, [FromQuery] bool soloInsumos = true, [FromQuery] bool soloProductoTerminado = false) =>
        Ok(await service.OpcionesAsync(soloActivos, soloInsumos, soloProductoTerminado));
}

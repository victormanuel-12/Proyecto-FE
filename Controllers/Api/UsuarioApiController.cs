using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Services;

namespace SPIn.Controllers.Api;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = Roles.Administracion)]
public class UsuarioApiController(IUsuarioService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GenericResponse<ListadoResponse<UsuarioResponse>>>> Listar([FromQuery] FiltroUsuarioRequest filtro) =>
        Ok(await service.ListarAsync(filtro));

    [HttpGet("{id}")]
    public async Task<ActionResult<GenericResponse<UsuarioResponse>>> Obtener(string id) =>
        Ok(await service.ObtenerAsync(id));

    [HttpPost]
    public async Task<ActionResult<GenericResponse<UsuarioResponse>>> Crear([FromBody] UsuarioCrearRequest request) =>
        StatusCode(StatusCodes.Status201Created, await service.CrearAsync(request));

    [HttpPut("{id}")]
    public async Task<ActionResult<GenericResponse<UsuarioResponse>>> Actualizar(string id, [FromBody] UsuarioActualizarRequest request) =>
        Ok(await service.ActualizarAsync(id, request));

    [HttpPatch("{id}/estado")]
    public async Task<ActionResult<GenericResponse<UsuarioResponse>>> CambiarEstado(string id, [FromBody] CambiarEstadoRequest request) =>
        Ok(await service.CambiarEstadoAsync(id, request.Activo));

    [HttpPost("{id}/restablecer-clave")]
    public async Task<ActionResult<GenericResponse<object>>> RestablecerClave(string id, [FromBody] RestablecerClaveRequest request) =>
        Ok(await service.RestablecerClaveAsync(id, request));
}

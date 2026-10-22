using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Services;

namespace SPIn.Controllers.Api;

[ApiController]
[Route("api/empresas")]
[Authorize(Roles = Roles.SuperAdmin)]
public class EmpresaApiController(IEmpresaService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GenericResponse<ListadoResponse<EmpresaResponse>>>> Listar([FromQuery] FiltroListadoRequest filtro) =>
        Ok(await service.ListarAsync(filtro));

    [HttpGet("opciones")]
    public async Task<ActionResult<GenericResponse<List<OpcionResponse>>>> Opciones() =>
        Ok(await service.OpcionesAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GenericResponse<EmpresaResponse>>> Obtener(int id) =>
        Ok(await service.ObtenerAsync(id));

    [HttpPost]
    public async Task<ActionResult<GenericResponse<EmpresaResponse>>> Crear([FromBody] EmpresaRequest request) =>
        StatusCode(StatusCodes.Status201Created, await service.CrearAsync(request));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<GenericResponse<EmpresaResponse>>> Actualizar(int id, [FromBody] EmpresaRequest request) =>
        Ok(await service.ActualizarAsync(id, request));

    [HttpPatch("{id:int}/estado")]
    public async Task<ActionResult<GenericResponse<EmpresaResponse>>> CambiarEstado(int id, [FromBody] CambiarEstadoRequest request) =>
        Ok(await service.CambiarEstadoAsync(id, request.Activo));
}

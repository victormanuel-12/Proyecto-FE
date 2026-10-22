using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Services;

namespace SPIn.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/produccion")]
public class ProduccionApiController(IProduccionService service) : ControllerBase
{
    [HttpGet("opciones")]
    public async Task<ActionResult<GenericResponse<OpcionesProduccionResponse>>> Opciones() => Ok(await service.OpcionesAsync());

    [HttpGet("planes")]
    public async Task<ActionResult<GenericResponse<PaginaResponse<PlanResponse>>>> Planes([FromQuery] FiltroPlanRequest filtro) =>
        Ok(await service.PlanesAsync(filtro));

    [HttpPost("planes"), Authorize(Roles = Roles.Produccion)]
    public async Task<ActionResult<GenericResponse<PlanResponse>>> CrearPlan([FromBody] PlanRequest request) =>
        StatusCode(StatusCodes.Status201Created, await service.CrearPlanAsync(request));

    [HttpPut("planes/{id:int}"), Authorize(Roles = Roles.Produccion)]
    public async Task<ActionResult<GenericResponse<PlanResponse>>> ActualizarPlan(int id, [FromBody] PlanRequest request) =>
        Ok(await service.ActualizarPlanAsync(id, request));

    [HttpPost("planes/{id:int}/cancelar"), Authorize(Roles = Roles.Produccion)]
    public async Task<ActionResult<GenericResponse<PlanResponse>>> CancelarPlan(int id) => Ok(await service.CancelarPlanAsync(id));

    [HttpGet("partes")]
    public async Task<ActionResult<GenericResponse<PaginaResponse<ParteResponse>>>> Partes([FromQuery] FiltroParteRequest filtro) =>
        Ok(await service.PartesAsync(filtro));

    [HttpGet("partes/{id:int}")]
    public async Task<ActionResult<GenericResponse<ParteResponse>>> Parte(int id) => Ok(await service.ParteAsync(id));

    /// <summary>Insumos necesarios para producir N paquetes y si alcanza el stock.</summary>
    [HttpPost("requerimiento")]
    public async Task<ActionResult<GenericResponse<RequerimientoResponse>>> Requerimiento([FromBody] RequerimientoRequest request) =>
        Ok(await service.RequerimientoAsync(request));

    [HttpPost("partes"), Authorize(Roles = Roles.PartesProduccion)]
    public async Task<ActionResult<GenericResponse<ParteResponse>>> RegistrarParte([FromBody] ParteRequest request) =>
        StatusCode(StatusCodes.Status201Created, await service.RegistrarParteAsync(request));

    [HttpPost("partes/{id:int}/anular"), Authorize(Roles = Roles.Produccion)]
    public async Task<ActionResult<GenericResponse<ParteResponse>>> AnularParte(int id, [FromBody] MotivoRequest request) =>
        Ok(await service.AnularParteAsync(id, request));
}

[ApiController]
[Authorize]
[Route("api/pedidos")]
public class PedidoApiController(IPedidoService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GenericResponse<PedidosPaginaResponse>>> Listar([FromQuery] FiltroPedidoRequest filtro) =>
        Ok(await service.ListarAsync(filtro));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GenericResponse<PedidoResponse>>> Obtener(int id) => Ok(await service.ObtenerAsync(id));

    /// <summary>Insumos que vende el proveedor, con precio, entrega, pedido mínimo y múltiplo.</summary>
    [HttpGet("condiciones/{proveedorId:int}")]
    public async Task<ActionResult<GenericResponse<List<CondicionProveedorResponse>>>> Condiciones(int proveedorId) =>
        Ok(await service.CondicionesAsync(proveedorId));

    [HttpPost, Authorize(Roles = Roles.Abastecimiento)]
    public async Task<ActionResult<GenericResponse<PedidoResponse>>> Crear([FromBody] PedidoRequest request) =>
        StatusCode(StatusCodes.Status201Created, await service.CrearAsync(request));

    [HttpPut("{id:int}"), Authorize(Roles = Roles.Abastecimiento)]
    public async Task<ActionResult<GenericResponse<PedidoResponse>>> Actualizar(int id, [FromBody] PedidoRequest request) =>
        Ok(await service.ActualizarAsync(id, request));

    [HttpPost("{id:int}/cancelar"), Authorize(Roles = Roles.Abastecimiento)]
    public async Task<ActionResult<GenericResponse<PedidoResponse>>> Cancelar(int id, [FromBody] MotivoRequest request) =>
        Ok(await service.CancelarAsync(id, request));

    [HttpPost("{id:int}/recepciones"), Authorize(Roles = Roles.Recepcion)]
    public async Task<ActionResult<GenericResponse<PedidoResponse>>> Recibir(int id, [FromBody] RecepcionRequest request) =>
        Ok(await service.RecibirAsync(id, request));

    [HttpPost("recepciones/{id:int}/anular"), Authorize(Roles = Roles.Recepcion)]
    public async Task<ActionResult<GenericResponse<PedidoResponse>>> AnularRecepcion(int id, [FromBody] MotivoRequest request) =>
        Ok(await service.AnularRecepcionAsync(id, request));
}

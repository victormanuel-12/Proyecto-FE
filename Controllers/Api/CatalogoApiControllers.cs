using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Services;

namespace SPIn.Controllers.Api;

// Lectura: cualquier usuario autenticado de la empresa. Escritura: según el rol indicado en cada acción.

[ApiController]
[Route("api/categorias")]
[Authorize]
public class CategoriaApiController(ICategoriaService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GenericResponse<ListadoResponse<CategoriaResponse>>>> Listar([FromQuery] FiltroListadoRequest filtro) =>
        Ok(await service.ListarAsync(filtro));

    [HttpGet("opciones")]
    public async Task<ActionResult<GenericResponse<List<OpcionResponse>>>> Opciones([FromQuery] bool soloActivas = true) =>
        Ok(await service.OpcionesAsync(soloActivas));

    [HttpPost, Authorize(Roles = Roles.Catalogo)]
    public async Task<ActionResult<GenericResponse<CategoriaResponse>>> Crear([FromBody] CategoriaRequest request) =>
        StatusCode(StatusCodes.Status201Created, await service.CrearAsync(request));

    [HttpPut("{id:int}"), Authorize(Roles = Roles.Catalogo)]
    public async Task<ActionResult<GenericResponse<CategoriaResponse>>> Actualizar(int id, [FromBody] CategoriaRequest request) =>
        Ok(await service.ActualizarAsync(id, request));

    [HttpPatch("{id:int}/estado"), Authorize(Roles = Roles.Catalogo)]
    public async Task<ActionResult<GenericResponse<CategoriaResponse>>> CambiarEstado(int id, [FromBody] CambiarEstadoRequest request) =>
        Ok(await service.CambiarEstadoAsync(id, request.Activo));

    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Catalogo)]
    public async Task<ActionResult<GenericResponse<object>>> Eliminar(int id) =>
        Ok(await service.EliminarAsync(id));
}

[ApiController]
[Route("api/unidades")]
[Authorize]
public class UnidadApiController(IUnidadService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GenericResponse<ListadoResponse<UnidadResponse>>>> Listar([FromQuery] FiltroListadoRequest filtro) =>
        Ok(await service.ListarAsync(filtro));

    [HttpGet("opciones")]
    public async Task<ActionResult<GenericResponse<List<OpcionResponse>>>> Opciones([FromQuery] bool soloActivas = true) =>
        Ok(await service.OpcionesAsync(soloActivas));

    [HttpPost, Authorize(Roles = Roles.Catalogo)]
    public async Task<ActionResult<GenericResponse<UnidadResponse>>> Crear([FromBody] UnidadRequest request) =>
        StatusCode(StatusCodes.Status201Created, await service.CrearAsync(request));

    [HttpPut("{id:int}"), Authorize(Roles = Roles.Catalogo)]
    public async Task<ActionResult<GenericResponse<UnidadResponse>>> Actualizar(int id, [FromBody] UnidadRequest request) =>
        Ok(await service.ActualizarAsync(id, request));

    [HttpPatch("{id:int}/estado"), Authorize(Roles = Roles.Catalogo)]
    public async Task<ActionResult<GenericResponse<UnidadResponse>>> CambiarEstado(int id, [FromBody] CambiarEstadoRequest request) =>
        Ok(await service.CambiarEstadoAsync(id, request.Activo));

    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Catalogo)]
    public async Task<ActionResult<GenericResponse<object>>> Eliminar(int id) =>
        Ok(await service.EliminarAsync(id));
}

[ApiController]
[Route("api/insumos")]
[Authorize]
public class InsumoApiController(IInsumoService service, ICondicionCompraService condiciones) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GenericResponse<ListadoResponse<InsumoResponse>>>> Listar([FromQuery] FiltroInsumoRequest filtro) =>
        Ok(await service.ListarAsync(filtro));

    [HttpGet("opciones")]
    public async Task<ActionResult<GenericResponse<List<OpcionResponse>>>> Opciones([FromQuery] bool soloActivos = true) =>
        Ok(await service.OpcionesAsync(soloActivos));

    /// <summary>Insumo con sus condiciones de compra (panel de detalle).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<GenericResponse<InsumoResponse>>> Obtener(int id) =>
        Ok(await service.ObtenerAsync(id));

    [HttpGet("{id:int}/condiciones")]
    public async Task<ActionResult<GenericResponse<List<CondicionCompraResponse>>>> Condiciones(int id) =>
        Ok(await condiciones.ListarPorInsumoAsync(id));

    [HttpPost, Authorize(Roles = Roles.Catalogo)]
    public async Task<ActionResult<GenericResponse<InsumoResponse>>> Crear([FromBody] InsumoRequest request) =>
        StatusCode(StatusCodes.Status201Created, await service.CrearAsync(request));

    [HttpPut("{id:int}"), Authorize(Roles = Roles.Catalogo)]
    public async Task<ActionResult<GenericResponse<InsumoResponse>>> Actualizar(int id, [FromBody] InsumoRequest request) =>
        Ok(await service.ActualizarAsync(id, request));

    [HttpPatch("{id:int}/estado"), Authorize(Roles = Roles.Catalogo)]
    public async Task<ActionResult<GenericResponse<InsumoResponse>>> CambiarEstado(int id, [FromBody] CambiarEstadoRequest request) =>
        Ok(await service.CambiarEstadoAsync(id, request.Activo));

    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Catalogo)]
    public async Task<ActionResult<GenericResponse<object>>> Eliminar(int id) =>
        Ok(await service.EliminarAsync(id));
}

[ApiController]
[Route("api/proveedores")]
[Authorize]
public class ProveedorApiController(IProveedorService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GenericResponse<ListadoResponse<ProveedorResponse>>>> Listar([FromQuery] FiltroProveedorRequest filtro) =>
        Ok(await service.ListarAsync(filtro));

    [HttpGet("opciones")]
    public async Task<ActionResult<GenericResponse<List<OpcionResponse>>>> Opciones([FromQuery] bool soloActivos = true) =>
        Ok(await service.OpcionesAsync(soloActivos));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GenericResponse<ProveedorResponse>>> Obtener(int id) =>
        Ok(await service.ObtenerAsync(id));

    [HttpPost, Authorize(Roles = Roles.Abastecimiento)]
    public async Task<ActionResult<GenericResponse<ProveedorResponse>>> Crear([FromBody] ProveedorRequest request) =>
        StatusCode(StatusCodes.Status201Created, await service.CrearAsync(request));

    [HttpPut("{id:int}"), Authorize(Roles = Roles.Abastecimiento)]
    public async Task<ActionResult<GenericResponse<ProveedorResponse>>> Actualizar(int id, [FromBody] ProveedorRequest request) =>
        Ok(await service.ActualizarAsync(id, request));

    [HttpPatch("{id:int}/estado"), Authorize(Roles = Roles.Abastecimiento)]
    public async Task<ActionResult<GenericResponse<ProveedorResponse>>> CambiarEstado(int id, [FromBody] CambiarEstadoRequest request) =>
        Ok(await service.CambiarEstadoAsync(id, request.Activo));

    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Abastecimiento)]
    public async Task<ActionResult<GenericResponse<object>>> Eliminar(int id) =>
        Ok(await service.EliminarAsync(id));
}

[ApiController]
[Route("api/condiciones")]
[Authorize(Roles = Roles.Abastecimiento)]
public class CondicionCompraApiController(ICondicionCompraService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<GenericResponse<List<CondicionCompraResponse>>>> Crear([FromBody] CondicionCompraRequest request) =>
        StatusCode(StatusCodes.Status201Created, await service.CrearAsync(request));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<GenericResponse<List<CondicionCompraResponse>>>> Actualizar(int id, [FromBody] CondicionCompraRequest request) =>
        Ok(await service.ActualizarAsync(id, request));

    [HttpPost("{id:int}/principal")]
    public async Task<ActionResult<GenericResponse<List<CondicionCompraResponse>>>> MarcarPrincipal(int id) =>
        Ok(await service.MarcarPrincipalAsync(id));

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<GenericResponse<List<CondicionCompraResponse>>>> Eliminar(int id) =>
        Ok(await service.EliminarAsync(id));
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Services;

namespace SPIn.Controllers.Api;

[ApiController]
[Route("api/productos")]
[Authorize]
public class ProductoApiController(IProductoService productos, IRecetaService recetas) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GenericResponse<ListadoResponse<ProductoResponse>>>> Listar([FromQuery] FiltroProductoRequest filtro) =>
        Ok(await productos.ListarAsync(filtro));

    /// <summary>Productos para listas desplegables, con botellas por paquete.</summary>
    [HttpGet("opciones")]
    public async Task<ActionResult<GenericResponse<List<ProductoOpcionResponse>>>> Opciones([FromQuery] bool soloActivos = true) =>
        Ok(await productos.OpcionesAsync(soloActivos));

    /// <summary>Productos con receta vigente (para copiar su receta).</summary>
    [HttpGet("con-receta")]
    public async Task<ActionResult<GenericResponse<List<OpcionResponse>>>> ConReceta([FromQuery] int? excluir) =>
        Ok(await productos.OpcionesConRecetaAsync(excluir));

    /// <summary>Insumos que se pueden usar en una receta (activos y de consumo por producción).</summary>
    [HttpGet("insumos-receta")]
    public async Task<ActionResult<GenericResponse<List<InsumoRecetaOpcion>>>> InsumosReceta() =>
        Ok(await recetas.InsumosDisponiblesAsync());

    /// <summary>Producto con su receta vigente, el historial de versiones y las líneas que pueden producirlo.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<GenericResponse<ProductoResponse>>> Obtener(int id) =>
        Ok(await productos.ObtenerAsync(id));

    [HttpGet("{id:int}/receta")]
    public async Task<ActionResult<GenericResponse<RecetaResponse>>> RecetaVigente(int id, [FromQuery] DateOnly? fecha) =>
        Ok(await recetas.VigenteAsync(id, fecha));

    [HttpGet("recetas/{recetaId:int}")]
    public async Task<ActionResult<GenericResponse<RecetaResponse>>> Receta(int recetaId) =>
        Ok(await recetas.ObtenerAsync(recetaId));

    /// <summary>Calcula el consumo de insumos para producir una cantidad (no guarda nada).</summary>
    [HttpPost("{id:int}/consumo")]
    public async Task<ActionResult<GenericResponse<ExplosionResponse>>> CalcularConsumo(int id, [FromBody] ExplosionRequest request) =>
        Ok(await recetas.ExplotarAsync(id, request));

    [HttpPost, Authorize(Roles = Roles.Produccion)]
    public async Task<ActionResult<GenericResponse<ProductoResponse>>> Crear([FromBody] ProductoRequest request) =>
        StatusCode(StatusCodes.Status201Created, await productos.CrearAsync(request));

    [HttpPut("{id:int}"), Authorize(Roles = Roles.Produccion)]
    public async Task<ActionResult<GenericResponse<ProductoResponse>>> Actualizar(int id, [FromBody] ProductoRequest request) =>
        Ok(await productos.ActualizarAsync(id, request));

    [HttpPut("{id:int}/receta"), Authorize(Roles = Roles.Produccion)]
    public async Task<ActionResult<GenericResponse<ProductoResponse>>> GuardarReceta(int id, [FromBody] RecetaRequest request) =>
        Ok(await recetas.GuardarAsync(id, request));

    [HttpPatch("{id:int}/estado"), Authorize(Roles = Roles.Produccion)]
    public async Task<ActionResult<GenericResponse<ProductoResponse>>> CambiarEstado(int id, [FromBody] CambiarEstadoRequest request) =>
        Ok(await productos.CambiarEstadoAsync(id, request.Activo));

    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Produccion)]
    public async Task<ActionResult<GenericResponse<object>>> Eliminar(int id) =>
        Ok(await productos.EliminarAsync(id));
}

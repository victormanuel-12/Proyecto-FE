using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIn.Common.Excepciones;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Services;
using SPIn.Services.Importacion;

namespace SPIn.Controllers.Api;

[ApiController]
[Authorize]
public class VentaApiController(IVentaService service) : ControllerBase
{
    [HttpGet("api/ventas")]
    public async Task<ActionResult<GenericResponse<VentasPaginaResponse>>> Listar([FromQuery] FiltroVentaRequest filtro) =>
        Ok(await service.ListarAsync(filtro));

    /// <summary>Paquetes vendidos por semana (lunes a domingo), para el gráfico de demanda.</summary>
    [HttpGet("api/ventas/semanal")]
    public async Task<ActionResult<GenericResponse<VentasSemanalesResponse>>> Semanal([FromQuery] int? productoId, [FromQuery] int semanas = 26) =>
        Ok(await service.SemanalAsync(productoId, semanas));

    [HttpPost("api/ventas"), Authorize(Roles = Roles.Ventas)]
    public async Task<ActionResult<GenericResponse<VentaResponse>>> Registrar([FromBody] VentaRequest request) =>
        StatusCode(StatusCodes.Status201Created, await service.RegistrarAsync(request));

    [HttpPost("api/ventas/{id:long}/anular"), Authorize(Roles = Roles.Ventas)]
    public async Task<ActionResult<GenericResponse<VentaResponse>>> Anular(long id, [FromBody] AnularVentaRequest request) =>
        Ok(await service.AnularAsync(id, request));
}

[ApiController]
[Authorize]
public class ProductoTerminadoApiController(IProductoTerminadoService service) : ControllerBase
{
    [HttpGet("api/producto-terminado")]
    public async Task<ActionResult<GenericResponse<PaginaResponse<StockProductoResponse>>>> Stock([FromQuery] FiltroStockProductoRequest filtro) =>
        Ok(await service.StockAsync(filtro));

    [HttpGet("api/producto-terminado/cantidad")]
    public async Task<ActionResult<GenericResponse<decimal>>> Cantidad([FromQuery] int productoId, [FromQuery] int almacenId) =>
        Ok(await service.PaquetesAsync(productoId, almacenId));

    [HttpGet("api/producto-terminado/movimientos")]
    public async Task<ActionResult<GenericResponse<PaginaResponse<MovimientoProductoResponse>>>> Movimientos(
        [FromQuery] int? productoId, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20) =>
        Ok(await service.MovimientosAsync(productoId, pagina, tamanoPagina));

    [HttpPost("api/producto-terminado/movimientos"), Authorize(Roles = Roles.Almacen)]
    public async Task<ActionResult<GenericResponse<MovimientoProductoResponse>>> Registrar([FromBody] MovimientoProductoRequest request) =>
        StatusCode(StatusCodes.Status201Created, await service.RegistrarAsync(request));
}

[ApiController]
[Authorize(Roles = Roles.Administracion)]
public class ImportacionApiController(IImportacionService service) : ControllerBase
{
    [HttpGet("api/importar/tipos")]
    public ActionResult<GenericResponse<List<TipoImportacionResponse>>> Tipos() =>
        Ok(new GenericResponse<List<TipoImportacionResponse>>(service.Tipos()));

    [HttpGet("api/importar/{tipo}/plantilla")]
    public async Task<IActionResult> Plantilla(string tipo)
    {
        var (contenido, nombre) = await service.PlantillaAsync(tipo);
        return File(contenido, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombre);
    }

    /// <summary>Sube el archivo y devuelve la vista previa (no guarda nada).</summary>
    [HttpPost("api/importar/{tipo}/vista-previa")]
    [RequestSizeLimit(ImportacionService.TamanoMaximo + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImportacionService.TamanoMaximo + 64 * 1024)]
    public async Task<ActionResult<GenericResponse<VistaPreviaImportacionResponse>>> VistaPrevia(
        string tipo, IFormFile? archivo, [FromForm] string? modo, [FromForm] DateOnly? fecha)
    {
        if (archivo is null || archivo.Length == 0) throw GenericException.Campo("archivo", "Seleccione el archivo.");
        if (archivo.Length > ImportacionService.TamanoMaximo) throw GenericException.Campo("archivo", "El archivo supera los 5 MB.");
        await using var flujo = archivo.OpenReadStream();
        return Ok(await service.PrevisualizarAsync(tipo, flujo, Path.GetFileName(archivo.FileName), new OpcionesImportacion { Modo = modo, Fecha = fecha }));
    }

    [HttpPost("api/importar/confirmar/{token}")]
    public async Task<ActionResult<GenericResponse<ResultadoImportacionResponse>>> Confirmar(string token) =>
        Ok(await service.ConfirmarAsync(token));
}

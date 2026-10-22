using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Services;

namespace SPIn.Controllers.Api;

[ApiController]
[Authorize]
public class InventarioApiController(IInventarioService service) : ControllerBase
{
    [HttpGet("api/stock")]
    public async Task<ActionResult<GenericResponse<PaginaResponse<StockInsumoResponse>>>> Stock([FromQuery] FiltroStockRequest filtro) =>
        Ok(await service.StockAsync(filtro));

    [HttpGet("api/stock/resumen")]
    public async Task<ActionResult<GenericResponse<ResumenStockResponse>>> Resumen([FromQuery] int? almacenId) =>
        Ok(await service.ResumenStockAsync(almacenId));

    [HttpGet("api/stock/cantidad")]
    public async Task<ActionResult<GenericResponse<decimal>>> Cantidad([FromQuery] int insumoId, [FromQuery] int almacenId) =>
        Ok(await service.CantidadAsync(insumoId, almacenId));

    [HttpGet("api/movimientos")]
    public async Task<ActionResult<GenericResponse<PaginaResponse<MovimientoResponse>>>> Movimientos([FromQuery] FiltroMovimientoRequest filtro) =>
        Ok(await service.MovimientosAsync(filtro));

    [HttpPost("api/movimientos"), Authorize(Roles = Roles.Almacen)]
    public async Task<ActionResult<GenericResponse<MovimientoResponse>>> Registrar([FromBody] MovimientoRequest request) =>
        StatusCode(StatusCodes.Status201Created, await service.RegistrarAsync(request));

    [HttpPost("api/movimientos/{id:long}/revertir"), Authorize(Roles = Roles.Almacen)]
    public async Task<ActionResult<GenericResponse<MovimientoResponse>>> Revertir(long id, [FromBody] RevertirMovimientoRequest request) =>
        Ok(await service.RevertirAsync(id, request));

    [HttpGet("api/kardex")]
    public async Task<ActionResult<GenericResponse<KardexResponse>>> Kardex([FromQuery] KardexRequest request) =>
        Ok(await service.KardexAsync(request));

    /// <summary>Kardex completo del rango en CSV (separado por punto y coma; abre directo en Excel en español).</summary>
    [HttpGet("api/kardex/csv")]
    public async Task<IActionResult> KardexCsv([FromQuery] KardexRequest request)
    {
        var k = await service.KardexCompletoAsync(request);
        var c = CultureInfo.GetCultureInfo("es-PE");
        string Num(decimal? n) => n is null ? "" : n.Value.ToString("0.######", c);
        string Txt(string? t) => "\"" + (t ?? string.Empty).Replace("\"", "\"\"") + "\"";

        var sb = new StringBuilder();
        sb.AppendLine($"{Txt("Kardex")};{Txt($"{k.InsumoCodigo} · {k.InsumoNombre}")}");
        sb.AppendLine($"{Txt("Almacén")};{Txt(k.AlmacenNombre)}");
        sb.AppendLine($"{Txt("Periodo")};{Txt($"{k.Desde:dd/MM/yyyy} al {k.Hasta:dd/MM/yyyy}")}");
        sb.AppendLine($"{Txt("Unidad")};{Txt(k.UnidadConsumoCodigo)}");
        sb.AppendLine();
        sb.AppendLine("Fecha;Hora;N° mov.;Tipo;Almacén;Documento;Detalle;Entrada;Salida;Saldo;Usuario");
        sb.AppendLine($"{k.Desde:dd/MM/yyyy};;;{Txt("Saldo inicial")};;;;;;{Num(k.SaldoInicial)};");
        foreach (var l in k.Lineas.Items)
        {
            sb.AppendLine(string.Join(';',
                l.Fecha.ToString("dd/MM/yyyy"), l.Fecha.ToString("HH:mm"), l.Id, Txt(l.TipoNombre), Txt(l.AlmacenCodigo),
                Txt(l.Documento), Txt(l.Revertido ? $"{l.Detalle} (revertido)" : l.Detalle),
                Num(l.Entrada), Num(l.Salida), Num(l.Saldo), Txt(l.Usuario)));
        }
        sb.AppendLine($"{k.Hasta:dd/MM/yyyy};;;{Txt("Totales y saldo final")};;;;{Num(k.TotalEntradas)};{Num(k.TotalSalidas)};{Num(k.SaldoFinal)};");

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var nombre = $"kardex_{k.InsumoCodigo}_{k.Desde:yyyyMMdd}_{k.Hasta:yyyyMMdd}.csv";
        return File(bytes, "text/csv; charset=utf-8", nombre);
    }
}

[ApiController]
[Route("api/conteos")]
[Authorize]
public class ConteoApiController(IConteoService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GenericResponse<PaginaResponse<ConteoResponse>>>> Listar([FromQuery] FiltroConteoRequest filtro) =>
        Ok(await service.ListarAsync(filtro));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GenericResponse<ConteoDetalleCompletoResponse>>> Obtener(int id) =>
        Ok(await service.ObtenerAsync(id));

    [HttpPost, Authorize(Roles = Roles.Almacen)]
    public async Task<ActionResult<GenericResponse<ConteoDetalleCompletoResponse>>> Crear([FromBody] ConteoCrearRequest request) =>
        StatusCode(StatusCodes.Status201Created, await service.CrearAsync(request));

    [HttpPut("{id:int}"), Authorize(Roles = Roles.Almacen)]
    public async Task<ActionResult<GenericResponse<ConteoDetalleCompletoResponse>>> Guardar(int id, [FromBody] ConteoGuardarRequest request) =>
        Ok(await service.GuardarAsync(id, request));

    [HttpPost("{id:int}/aprobar"), Authorize(Roles = Roles.Almacen)]
    public async Task<ActionResult<GenericResponse<ConteoDetalleCompletoResponse>>> Aprobar(int id, [FromBody] ConteoGuardarRequest request) =>
        Ok(await service.AprobarAsync(id, request));

    [HttpPost("{id:int}/anular"), Authorize(Roles = Roles.Almacen)]
    public async Task<ActionResult<GenericResponse<ConteoDetalleCompletoResponse>>> Anular(int id) =>
        Ok(await service.AnularAsync(id));
}

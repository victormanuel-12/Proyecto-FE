using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Services;

public interface IVentaService
{
    Task<GenericResponse<VentasPaginaResponse>> ListarAsync(FiltroVentaRequest filtro);
    Task<GenericResponse<VentasSemanalesResponse>> SemanalAsync(int? productoId, int semanas);
    Task<GenericResponse<VentaResponse>> RegistrarAsync(VentaRequest request);
    Task<GenericResponse<VentaResponse>> AnularAsync(long id, AnularVentaRequest request);
}

public interface IProductoTerminadoService
{
    Task<GenericResponse<PaginaResponse<StockProductoResponse>>> StockAsync(FiltroStockProductoRequest filtro);
    Task<GenericResponse<decimal>> PaquetesAsync(int productoId, int almacenId);
    Task<GenericResponse<PaginaResponse<MovimientoProductoResponse>>> MovimientosAsync(int? productoId, int pagina, int tamanoPagina);
    Task<GenericResponse<MovimientoProductoResponse>> RegistrarAsync(MovimientoProductoRequest request);

    /// <summary>
    /// Único punto que modifica el stock de producto terminado (lo usan ventas y, desde la Entrega 6, producción).
    /// Debe llamarse dentro de una transacción. No permite stock negativo.
    /// </summary>
    Task<MovimientoProducto> AplicarAsync(int productoId, int almacenId, TipoMovimientoProducto tipo, decimal paquetes,
        string? documento = null, string? motivo = null, string? referencia = null, long? ventaId = null, long? revierteId = null,
        int? parteProduccionId = null);
}

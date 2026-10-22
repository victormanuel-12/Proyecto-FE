using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository;

public interface IVentaRepository
{
    // ---------------- ventas
    Task<VentasPaginaResponse> ListarAsync(FiltroVentaRequest filtro);
    Task<VentaResponse?> ResumenAsync(long id);
    Task<Venta?> ObtenerAsync(long id);
    /// <summary>Paquetes vendidos por día (no anuladas) entre dos fechas, para agrupar por semana en memoria.</summary>
    Task<List<(DateOnly Fecha, decimal Paquetes)>> VentasDiariasAsync(int? productoId, DateOnly desde, DateOnly hasta);
    Task<DateOnly?> PrimeraVentaAsync(int? productoId);
    Task<Dictionary<int, decimal>> VentaPromedioDiariaAsync(DateOnly desde, DateOnly hasta);
    Task<List<Venta>> HistorialMismaClaveAsync(DateOnly fecha, int productoId, CanalVenta canal);
    Task<int> ProductosConVentasAsync();
    Task<bool> ProductoTieneVentasAsync(int productoId);
    void Agregar(Venta venta);
    void Eliminar(Venta venta);

    // ---------------- producto terminado
    Task<List<StockProductoResponse>> StockAsync(FiltroStockProductoRequest filtro);
    Task<StockProducto?> StockDeAsync(int productoId, int almacenId);
    Task<decimal> PaquetesAsync(int productoId, int almacenId);
    Task<bool> HayMovimientosAsync(int productoId, int almacenId);
    Task<bool> AlmacenTieneMovimientosProductoAsync(int almacenId);
    Task<bool> AlmacenTieneStockProductoAsync(int almacenId);
    Task<bool> ProductoTieneMovimientosAsync(int productoId);
    Task<PaginaResponse<MovimientoProductoResponse>> MovimientosAsync(int? productoId, int pagina, int tamanoPagina);
    Task<MovimientoProducto?> MovimientoAsync(long id);
    Task<MovimientoProducto?> MovimientoDeVentaAsync(long ventaId);
    void AgregarStock(StockProducto stock);
    void AgregarMovimiento(MovimientoProducto movimiento);

    Task<T> EnTransaccionAsync<T>(Func<Task<T>> accion);
    Task GuardarAsync();
}

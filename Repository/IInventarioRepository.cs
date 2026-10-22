using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository;

public interface IInventarioRepository
{
    // ---------------- stock
    Task<Stock?> StockAsync(int insumoId, int almacenId);
    Task<decimal> CantidadAsync(int insumoId, int almacenId);
    Task<List<StockAlmacenResponse>> StockPorInsumoAsync(int insumoId);
    Task<List<StockInsumoResponse>> StockInsumosAsync(FiltroStockRequest filtro);
    Task<Dictionary<int, decimal>> StockDeAlmacenAsync(int almacenId);
    Task<bool> AlmacenTieneStockAsync(int almacenId);
    Task<int> InsumosConStockAsync();
    void AgregarStock(Stock stock);

    // ---------------- movimientos
    Task<PaginaResponse<MovimientoResponse>> ListarMovimientosAsync(FiltroMovimientoRequest filtro);
    Task<MovimientoStock?> MovimientoAsync(long id);
    Task<MovimientoResponse?> MovimientoResumenAsync(long id);
    Task<bool> HayMovimientosAsync(int insumoId, int almacenId);
    Task<bool> InsumoTieneMovimientosAsync(int insumoId);
    Task<bool> AlmacenTieneMovimientosAsync(int almacenId);
    /// <summary>Saldo del insumo al inicio de la fecha indicada (suma de todos los almacenes o de uno).</summary>
    Task<decimal> SaldoAntesDeAsync(int insumoId, int? almacenId, DateTime limite);
    Task<List<MovimientoStock>> MovimientosEntreAsync(int insumoId, int? almacenId, DateTime desde, DateTime hasta);
    void AgregarMovimiento(MovimientoStock movimiento);

    // ---------------- conteos
    Task<PaginaResponse<ConteoResponse>> ListarConteosAsync(FiltroConteoRequest filtro);
    Task<ConteoDetalleCompletoResponse?> ConteoCompletoAsync(int id);
    Task<ConteoFisico?> ConteoConDetallesAsync(int id);
    Task<bool> HayConteoAbiertoAsync(int almacenId);
    Task<int> SiguienteNumeroConteoAsync();
    Task<List<Insumo>> InsumosParaConteoAsync(int? categoriaId);
    void AgregarConteo(ConteoFisico conteo);

    // ---------------- general
    Task<T> EnTransaccionAsync<T>(Func<Task<T>> accion);
    Task GuardarAsync();
}

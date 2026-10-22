using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Services;

public interface IInventarioService
{
    Task<GenericResponse<PaginaResponse<StockInsumoResponse>>> StockAsync(FiltroStockRequest filtro);
    Task<GenericResponse<ResumenStockResponse>> ResumenStockAsync(int? almacenId);
    Task<GenericResponse<decimal>> CantidadAsync(int insumoId, int almacenId);
    Task<GenericResponse<PaginaResponse<MovimientoResponse>>> MovimientosAsync(FiltroMovimientoRequest filtro);
    Task<GenericResponse<MovimientoResponse>> RegistrarAsync(MovimientoRequest request);
    Task<GenericResponse<MovimientoResponse>> RevertirAsync(long id, RevertirMovimientoRequest request);
    Task<GenericResponse<KardexResponse>> KardexAsync(KardexRequest request);
    Task<KardexResponse> KardexCompletoAsync(KardexRequest request);

    /// <summary>
    /// Único punto que modifica el stock. Valida que no quede negativo y deja el movimiento listo para guardar.
    /// Debe llamarse dentro de una transacción (IInventarioRepository.EnTransaccionAsync).
    /// Lo usarán también producción (Entrega 6) y recepción de pedidos (Entrega 7).
    /// </summary>
    Task<MovimientoStock> AplicarAsync(int insumoId, int almacenId, TipoMovimiento tipo, decimal cantidad,
        string? documento = null, string? motivo = null, string? referencia = null, int? conteoId = null, long? revierteId = null,
        int? parteProduccionId = null, int? recepcionId = null);
}

public interface IConteoService
{
    Task<GenericResponse<PaginaResponse<ConteoResponse>>> ListarAsync(FiltroConteoRequest filtro);
    Task<GenericResponse<ConteoDetalleCompletoResponse>> ObtenerAsync(int id);
    Task<GenericResponse<ConteoDetalleCompletoResponse>> CrearAsync(ConteoCrearRequest request);
    Task<GenericResponse<ConteoDetalleCompletoResponse>> GuardarAsync(int id, ConteoGuardarRequest request);
    Task<GenericResponse<ConteoDetalleCompletoResponse>> AprobarAsync(int id, ConteoGuardarRequest request);
    Task<GenericResponse<ConteoDetalleCompletoResponse>> AnularAsync(int id);
}

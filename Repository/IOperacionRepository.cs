using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository;

/// <summary>Plan de producción y partes de producción (Entrega 6).</summary>
public interface IProduccionOperacionRepository
{
    Task<OpcionesProduccionResponse> OpcionesAsync(DateOnly hoy);
    Task<LineaProduccion?> LineaConFormatosAsync(int id);

    Task<PaginaResponse<PlanResponse>> PlanesAsync(FiltroPlanRequest filtro, DateOnly hoy);
    Task<PlanResponse?> PlanResumenAsync(int id, DateOnly hoy);
    Task<PlanProduccion?> PlanAsync(int id);
    /// <summary>Plan pendiente del mismo día, línea y producto (para enlazarlo al parte).</summary>
    Task<PlanProduccion?> PlanPendienteAsync(DateOnly fecha, int lineaId, int productoId);
    void AgregarPlan(PlanProduccion plan);

    Task<PaginaResponse<ParteResponse>> PartesAsync(FiltroParteRequest filtro);
    Task<ParteResponse?> ParteResumenAsync(int id);
    Task<ParteProduccion?> ParteConConsumosAsync(int id);
    Task<int> SiguienteNumeroParteAsync();
    void AgregarParte(ParteProduccion parte);

    /// <summary>Stock por insumo en almacenes de insumos (uno o todos), en unidad de consumo.</summary>
    Task<Dictionary<int, decimal>> StockInsumosAsync(IEnumerable<int> insumoIds, int? almacenId);
    Task<bool> ProductoUsadoAsync(int productoId);
    Task<bool> LineaUsadaAsync(int lineaId);
    Task GuardarAsync();
}

/// <summary>Pedidos a proveedores y sus recepciones (Entrega 7).</summary>
public interface IPedidoRepository
{
    Task<PedidosPaginaResponse> ListarAsync(FiltroPedidoRequest filtro, DateOnly hoy);
    Task<PedidoResponse?> DetalleAsync(int id, DateOnly hoy);
    Task<PedidoCompra?> ConDetallesAsync(int id);
    Task<Recepcion?> RecepcionAsync(int id);
    Task<List<CondicionProveedorResponse>> CondicionesAsync(int proveedorId);
    /// <summary>Cantidad pendiente de llegar por insumo, en unidad de consumo.</summary>
    Task<Dictionary<int, decimal>> EnCaminoPorInsumoAsync();
    Task<string> SiguienteNumeroAsync();
    Task<bool> ProveedorUsadoAsync(int proveedorId);
    Task<bool> InsumoUsadoAsync(int insumoId);
    void Agregar(PedidoCompra pedido);
    void QuitarDetalles(PedidoCompra pedido);
    void AgregarRecepcion(Recepcion recepcion);
    Task GuardarAsync();
}

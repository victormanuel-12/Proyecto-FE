using SPIn.Dtos.Request;
using SPIn.Dtos.Response;

namespace SPIn.Services;

public interface IProduccionService
{
    Task<GenericResponse<OpcionesProduccionResponse>> OpcionesAsync();
    Task<GenericResponse<PaginaResponse<PlanResponse>>> PlanesAsync(FiltroPlanRequest filtro);
    Task<GenericResponse<PlanResponse>> CrearPlanAsync(PlanRequest request);
    Task<GenericResponse<PlanResponse>> ActualizarPlanAsync(int id, PlanRequest request);
    Task<GenericResponse<PlanResponse>> CancelarPlanAsync(int id);
    Task<GenericResponse<PaginaResponse<ParteResponse>>> PartesAsync(FiltroParteRequest filtro);
    Task<GenericResponse<ParteResponse>> ParteAsync(int id);
    Task<GenericResponse<RequerimientoResponse>> RequerimientoAsync(RequerimientoRequest request);
    Task<GenericResponse<ParteResponse>> RegistrarParteAsync(ParteRequest request);
    Task<GenericResponse<ParteResponse>> AnularParteAsync(int id, MotivoRequest request);
}

public interface IPedidoService
{
    Task<GenericResponse<PedidosPaginaResponse>> ListarAsync(FiltroPedidoRequest filtro);
    Task<GenericResponse<PedidoResponse>> ObtenerAsync(int id);
    Task<GenericResponse<List<CondicionProveedorResponse>>> CondicionesAsync(int proveedorId);
    Task<GenericResponse<PedidoResponse>> CrearAsync(PedidoRequest request);
    Task<GenericResponse<PedidoResponse>> ActualizarAsync(int id, PedidoRequest request);
    Task<GenericResponse<PedidoResponse>> CancelarAsync(int id, MotivoRequest request);
    Task<GenericResponse<PedidoResponse>> RecibirAsync(int id, RecepcionRequest request);
    Task<GenericResponse<PedidoResponse>> AnularRecepcionAsync(int recepcionId, MotivoRequest request);
}

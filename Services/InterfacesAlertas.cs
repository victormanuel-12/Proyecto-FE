using SPIn.Dtos.Request;
using SPIn.Dtos.Response;

namespace SPIn.Services;

public interface IAlertaService
{
    /// <summary>Revisa el estado del sistema y actualiza las alertas (abre nuevas, actualiza y cierra las resueltas).</summary>
    Task SincronizarAsync(bool forzar = false);
    Task<GenericResponse<AlertasPaginaResponse>> ListarAsync(FiltroAlertaRequest filtro);
    Task<GenericResponse<ResumenAlertasResponse>> ResumenAsync();
    Task<GenericResponse<AlertaResponse>> DescartarAsync(int id, DescartarAlertaRequest request);
    Task<GenericResponse<AlertaResponse>> ReactivarAsync(int id);
}

public interface IPanelService
{
    Task<GenericResponse<PanelResponse>> PanelAsync();
}

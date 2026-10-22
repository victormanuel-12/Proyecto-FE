using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Services;

public interface IPrediccionService
{
    /// <summary>Entrena el motor con las ventas de la empresa actual y guarda el pronóstico.</summary>
    Task<GenericResponse<EjecucionResponse>> EntrenarAsync(OrigenEjecucion origen);
    /// <summary>Lo llama el servicio en segundo plano: entrena solo si corresponde según la configuración.</summary>
    Task<bool> EntrenarSiCorrespondeAsync(bool alIniciar);
    Task<GenericResponse<PanelPronosticoResponse>> PanelAsync();
    Task<GenericResponse<DetallePronosticoResponse>> DetalleAsync(int productoId);
    Task<GenericResponse<List<EjecucionResponse>>> EjecucionesAsync();
    Task<GenericResponse<ConfiguracionResponse>> ConfiguracionAsync();
    Task<GenericResponse<ConfiguracionResponse>> GuardarConfiguracionAsync(ConfiguracionRequest request);
}

public interface IReposicionService
{
    Task<GenericResponse<ReposicionPaginaResponse>> ListarAsync(FiltroReposicionRequest filtro);
    Task<GenericResponse<ReposicionInsumoResponse>> DetalleAsync(int insumoId);
    /// <summary>Todos los insumos activos con su cálculo (lo usan alertas y panel).</summary>
    Task<List<ReposicionInsumoResponse>> CalcularTodoAsync();
}

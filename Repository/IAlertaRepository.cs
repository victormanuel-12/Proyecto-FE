using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository;

public interface IAlertaRepository
{
    /// <summary>Alertas activas o descartadas (las que aún no se resolvieron), con seguimiento de cambios.</summary>
    Task<List<Alerta>> NoResueltasAsync();
    Task<PaginaResponse<AlertaResponse>> ListarAsync(FiltroAlertaRequest filtro, DateTime desdeResueltas);
    Task<ResumenAlertasResponse> ResumenAsync();
    Task<List<AlertaResponse>> PrincipalesAsync(int cantidad);
    Task<Alerta?> ObtenerAsync(int id);
    void Agregar(Alerta alerta);
    Task PodarResueltasAsync(DateTime antesDe);
    Task<List<(int Id, int Numero, string AlmacenCodigo, DateTime CreadoEn)>> ConteosAbiertosAsync();
    Task GuardarAsync();
}

using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository;

public interface ILineaRepository
{
    Task<ListadoResponse<LineaResponse>> ListarAsync(FiltroLineaRequest filtro);
    Task<LineaResponse?> ObtenerResumenAsync(int id);
    Task<LineaProduccion?> ObtenerConFormatosAsync(int id);
    Task<bool> ExisteCodigoAsync(string codigo, int? excluirId);
    Task<List<Formato>> FormatosPorIdsAsync(IEnumerable<int> ids);
    Task<List<FormatoResponse>> ListarFormatosAsync();
    Task<int> ContarAsync();
    void Agregar(LineaProduccion linea);
    void Eliminar(LineaProduccion linea);
    Task GuardarAsync();
}

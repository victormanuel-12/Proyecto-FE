using SPIn.Dtos.Request;
using SPIn.Dtos.Response;

namespace SPIn.Repository;

public interface IUsuarioRepository
{
    /// <summary>Lista usuarios. Si empresaId es null, lista todas las empresas (solo SuperAdmin).</summary>
    Task<ListadoResponse<UsuarioResponse>> ListarAsync(FiltroUsuarioRequest filtro, int? empresaId);
    Task<UsuarioResponse?> ObtenerResumenAsync(string id);
    Task<int> ContarActivosAsync(int empresaId);
}

using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository;

public interface IEmpresaRepository
{
    Task<ListadoResponse<EmpresaResponse>> ListarAsync(FiltroListadoRequest filtro);
    Task<List<OpcionResponse>> OpcionesAsync();
    Task<EmpresaResponse?> ObtenerResumenAsync(int id);
    Task<Empresa?> ObtenerAsync(int id);
    Task<bool> ExisteRucAsync(string ruc, int? excluirId);
    /// <summary>Crea las categorías y unidades sugeridas para una empresa nueva.</summary>
    Task CrearCatalogoInicialAsync(int empresaId);
    void Agregar(Empresa empresa);
    Task GuardarAsync();
}

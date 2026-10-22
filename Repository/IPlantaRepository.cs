using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository;

public interface IPlantaRepository
{
    Task<ListadoResponse<PlantaResponse>> ListarAsync(FiltroListadoRequest filtro);
    Task<List<OpcionResponse>> OpcionesAsync(bool soloActivas);
    Task<PlantaResponse?> ObtenerResumenAsync(int id);
    Task<Planta?> ObtenerAsync(int id);
    Task<bool> ExisteCodigoAsync(string codigo, int? excluirId);
    Task<bool> TieneElementosActivosAsync(int id);
    Task<bool> TieneDependenciasAsync(int id);
    Task<int> ContarAsync();
    void Agregar(Planta planta);
    void Eliminar(Planta planta);
    Task GuardarAsync();
}

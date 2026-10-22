using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository;

public interface IAlmacenRepository
{
    Task<ListadoResponse<AlmacenResponse>> ListarAsync(FiltroAlmacenRequest filtro);
    Task<AlmacenResponse?> ObtenerResumenAsync(int id);
    /// <summary>Almacenes para selectores. soloInsumos excluye los de producto terminado.</summary>
    Task<List<OpcionResponse>> OpcionesAsync(bool soloActivos, bool soloInsumos, bool soloProductoTerminado = false);
    Task<Almacen?> ObtenerAsync(int id);
    Task<bool> ExisteCodigoAsync(string codigo, int? excluirId);
    Task<int> ContarAsync();
    void Agregar(Almacen almacen);
    void Eliminar(Almacen almacen);
    Task GuardarAsync();
}

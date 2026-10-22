using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository;

public interface ICategoriaRepository
{
    Task<ListadoResponse<CategoriaResponse>> ListarAsync(FiltroListadoRequest filtro);
    Task<List<OpcionResponse>> OpcionesAsync(bool soloActivas);
    Task<CategoriaResponse?> ObtenerResumenAsync(int id);
    Task<CategoriaInsumo?> ObtenerAsync(int id);
    Task<bool> ExisteNombreAsync(string nombre, int? excluirId);
    Task<bool> TieneInsumosAsync(int id);
    void Agregar(CategoriaInsumo categoria);
    void Eliminar(CategoriaInsumo categoria);
    Task GuardarAsync();
}

public interface IUnidadRepository
{
    Task<ListadoResponse<UnidadResponse>> ListarAsync(FiltroListadoRequest filtro);
    Task<List<OpcionResponse>> OpcionesAsync(bool soloActivas);
    Task<UnidadResponse?> ObtenerResumenAsync(int id);
    Task<UnidadMedida?> ObtenerAsync(int id);
    Task<bool> ExisteCodigoAsync(string codigo, int? excluirId);
    Task<bool> TieneInsumosAsync(int id);
    void Agregar(UnidadMedida unidad);
    void Eliminar(UnidadMedida unidad);
    Task GuardarAsync();
}

public interface IInsumoRepository
{
    Task<ListadoResponse<InsumoResponse>> ListarAsync(FiltroInsumoRequest filtro);
    Task<List<OpcionResponse>> OpcionesAsync(bool soloActivos);
    Task<InsumoResponse?> ObtenerResumenAsync(int id);
    Task<Insumo?> ObtenerAsync(int id);
    Task<bool> ExisteCodigoAsync(string codigo, int? excluirId);
    Task<bool> TieneCondicionesAsync(int id);
    Task<int> ContarAsync();
    void Agregar(Insumo insumo);
    void Eliminar(Insumo insumo);
    Task GuardarAsync();
}

public interface IProveedorRepository
{
    Task<ListadoResponse<ProveedorResponse>> ListarAsync(FiltroProveedorRequest filtro);
    Task<List<OpcionResponse>> OpcionesAsync(bool soloActivos);
    Task<ProveedorResponse?> ObtenerResumenAsync(int id);
    Task<Proveedor?> ObtenerAsync(int id);
    Task<bool> ExisteRucAsync(string ruc, int? excluirId);
    Task<bool> TieneCondicionesAsync(int id);
    Task<int> ContarInsumosPrincipalAsync(int id);
    Task<int> ContarAsync();
    void Agregar(Proveedor proveedor);
    void Eliminar(Proveedor proveedor);
    Task GuardarAsync();
}

public interface ICondicionCompraRepository
{
    Task<List<CondicionCompraResponse>> ListarPorInsumoAsync(int insumoId);
    Task<List<ProveedorInsumo>> EntidadesPorInsumoAsync(int insumoId);
    Task<ProveedorInsumo?> ObtenerAsync(int id);
    Task<bool> ExisteParAsync(int proveedorId, int insumoId, int? excluirId);
    void Agregar(ProveedorInsumo condicion);
    void Eliminar(ProveedorInsumo condicion);
    Task GuardarAsync();
}

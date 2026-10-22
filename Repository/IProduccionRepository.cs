using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository;

public interface IProductoRepository
{
    Task<ListadoResponse<ProductoResponse>> ListarAsync(FiltroProductoRequest filtro, DateOnly hoy);
    Task<List<OpcionResponse>> OpcionesConRecetaAsync(DateOnly hoy, int? excluirId);
    Task<List<ProductoOpcionResponse>> OpcionesAsync(bool soloActivos);
    Task<ProductoResponse?> ObtenerResumenAsync(int id, DateOnly hoy);
    Task<Producto?> ObtenerAsync(int id);
    Task<bool> ExisteCodigoAsync(string codigo, int? excluirId);
    Task<bool> TieneRecetasAsync(int id);
    Task<int> ContarConRecetaAsync(DateOnly hoy);
    Task<List<string>> LineasCompatiblesAsync(int formatoId);
    Task<bool> FormatoExisteAsync(int formatoId);
    void Agregar(Producto producto);
    void Eliminar(Producto producto);
    Task GuardarAsync();
}

public interface IRecetaRepository
{
    /// <summary>Receta vigente en la fecha indicada, con sus líneas.</summary>
    Task<RecetaResponse?> VigenteAsync(int productoId, DateOnly fecha, DateOnly hoy);
    Task<List<RecetaVersionResponse>> HistorialAsync(int productoId, DateOnly hoy);
    Task<RecetaResponse?> ObtenerAsync(int recetaId, DateOnly hoy);
    /// <summary>Última versión registrada (la de fecha de inicio más reciente), para editarla o cerrarla.</summary>
    Task<Receta?> UltimaConDetallesAsync(int productoId);
    Task<List<InsumoRecetaOpcion>> InsumosDisponiblesAsync();
    Task<List<Insumo>> InsumosPorIdsAsync(IEnumerable<int> ids);
    Task<List<Insumo>> InsumosConUnidadesAsync(IEnumerable<int> ids);
    Task<int> ProductosQueUsanInsumoAsync(int insumoId, DateOnly hoy);
    Task<bool> InsumoEnAlgunaRecetaAsync(int insumoId);
    void Agregar(Receta receta);
    void QuitarDetalle(RecetaDetalle detalle);
    Task GuardarAsync();
}

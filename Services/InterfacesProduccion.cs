using SPIn.Dtos.Request;
using SPIn.Dtos.Response;

namespace SPIn.Services;

public interface IProductoService
{
    Task<GenericResponse<ListadoResponse<ProductoResponse>>> ListarAsync(FiltroProductoRequest filtro);
    Task<GenericResponse<List<OpcionResponse>>> OpcionesConRecetaAsync(int? excluirId);
    Task<GenericResponse<List<ProductoOpcionResponse>>> OpcionesAsync(bool soloActivos);
    Task<GenericResponse<ProductoResponse>> ObtenerAsync(int id);
    Task<GenericResponse<ProductoResponse>> CrearAsync(ProductoRequest request);
    Task<GenericResponse<ProductoResponse>> ActualizarAsync(int id, ProductoRequest request);
    Task<GenericResponse<ProductoResponse>> CambiarEstadoAsync(int id, bool activo);
    Task<GenericResponse<object>> EliminarAsync(int id);
}

public interface IRecetaService
{
    Task<GenericResponse<List<InsumoRecetaOpcion>>> InsumosDisponiblesAsync();
    Task<GenericResponse<RecetaResponse>> ObtenerAsync(int recetaId);
    Task<GenericResponse<RecetaResponse>> VigenteAsync(int productoId, DateOnly? fecha);
    Task<GenericResponse<ProductoResponse>> GuardarAsync(int productoId, RecetaRequest request);

    /// <summary>
    /// "Explosión de materiales": cuánto de cada insumo se consume al producir una cantidad del producto,
    /// con la receta vigente en la fecha indicada y la merma esperada. Lo usarán producción y pronóstico.
    /// </summary>
    Task<GenericResponse<ExplosionResponse>> ExplotarAsync(int productoId, ExplosionRequest request);
}

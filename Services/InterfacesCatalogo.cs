using SPIn.Dtos.Request;
using SPIn.Dtos.Response;

namespace SPIn.Services;

public interface ICategoriaService
{
    Task<GenericResponse<ListadoResponse<CategoriaResponse>>> ListarAsync(FiltroListadoRequest filtro);
    Task<GenericResponse<List<OpcionResponse>>> OpcionesAsync(bool soloActivas);
    Task<GenericResponse<CategoriaResponse>> CrearAsync(CategoriaRequest request);
    Task<GenericResponse<CategoriaResponse>> ActualizarAsync(int id, CategoriaRequest request);
    Task<GenericResponse<CategoriaResponse>> CambiarEstadoAsync(int id, bool activo);
    Task<GenericResponse<object>> EliminarAsync(int id);
}

public interface IUnidadService
{
    Task<GenericResponse<ListadoResponse<UnidadResponse>>> ListarAsync(FiltroListadoRequest filtro);
    Task<GenericResponse<List<OpcionResponse>>> OpcionesAsync(bool soloActivas);
    Task<GenericResponse<UnidadResponse>> CrearAsync(UnidadRequest request);
    Task<GenericResponse<UnidadResponse>> ActualizarAsync(int id, UnidadRequest request);
    Task<GenericResponse<UnidadResponse>> CambiarEstadoAsync(int id, bool activo);
    Task<GenericResponse<object>> EliminarAsync(int id);
}

public interface IInsumoService
{
    Task<GenericResponse<ListadoResponse<InsumoResponse>>> ListarAsync(FiltroInsumoRequest filtro);
    Task<GenericResponse<List<OpcionResponse>>> OpcionesAsync(bool soloActivos);
    Task<GenericResponse<InsumoResponse>> ObtenerAsync(int id);
    Task<GenericResponse<InsumoResponse>> CrearAsync(InsumoRequest request);
    Task<GenericResponse<InsumoResponse>> ActualizarAsync(int id, InsumoRequest request);
    Task<GenericResponse<InsumoResponse>> CambiarEstadoAsync(int id, bool activo);
    Task<GenericResponse<object>> EliminarAsync(int id);
}

public interface IProveedorService
{
    Task<GenericResponse<ListadoResponse<ProveedorResponse>>> ListarAsync(FiltroProveedorRequest filtro);
    Task<GenericResponse<List<OpcionResponse>>> OpcionesAsync(bool soloActivos);
    Task<GenericResponse<ProveedorResponse>> ObtenerAsync(int id);
    Task<GenericResponse<ProveedorResponse>> CrearAsync(ProveedorRequest request);
    Task<GenericResponse<ProveedorResponse>> ActualizarAsync(int id, ProveedorRequest request);
    Task<GenericResponse<ProveedorResponse>> CambiarEstadoAsync(int id, bool activo);
    Task<GenericResponse<object>> EliminarAsync(int id);
}

public interface ICondicionCompraService
{
    Task<GenericResponse<List<CondicionCompraResponse>>> ListarPorInsumoAsync(int insumoId);
    Task<GenericResponse<List<CondicionCompraResponse>>> CrearAsync(CondicionCompraRequest request);
    Task<GenericResponse<List<CondicionCompraResponse>>> ActualizarAsync(int id, CondicionCompraRequest request);
    Task<GenericResponse<List<CondicionCompraResponse>>> MarcarPrincipalAsync(int id);
    Task<GenericResponse<List<CondicionCompraResponse>>> EliminarAsync(int id);
}

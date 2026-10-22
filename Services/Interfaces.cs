using SPIn.Dtos.Request;
using SPIn.Dtos.Response;

namespace SPIn.Services;

public interface IEmpresaService
{
    Task<GenericResponse<ListadoResponse<EmpresaResponse>>> ListarAsync(FiltroListadoRequest filtro);
    Task<GenericResponse<List<OpcionResponse>>> OpcionesAsync();
    Task<GenericResponse<EmpresaResponse>> ObtenerAsync(int id);
    Task<GenericResponse<EmpresaResponse>> CrearAsync(EmpresaRequest request);
    Task<GenericResponse<EmpresaResponse>> ActualizarAsync(int id, EmpresaRequest request);
    Task<GenericResponse<EmpresaResponse>> CambiarEstadoAsync(int id, bool activo);
}

public interface IPlantaService
{
    Task<GenericResponse<ListadoResponse<PlantaResponse>>> ListarAsync(FiltroListadoRequest filtro);
    Task<GenericResponse<List<OpcionResponse>>> OpcionesAsync(bool soloActivas);
    Task<GenericResponse<PlantaResponse>> ObtenerAsync(int id);
    Task<GenericResponse<PlantaResponse>> CrearAsync(PlantaRequest request);
    Task<GenericResponse<PlantaResponse>> ActualizarAsync(int id, PlantaRequest request);
    Task<GenericResponse<PlantaResponse>> CambiarEstadoAsync(int id, bool activo);
    Task<GenericResponse<object>> EliminarAsync(int id);
}

public interface IAlmacenService
{
    Task<GenericResponse<ListadoResponse<AlmacenResponse>>> ListarAsync(FiltroAlmacenRequest filtro);
    Task<GenericResponse<AlmacenResponse>> ObtenerAsync(int id);
    Task<GenericResponse<List<OpcionResponse>>> OpcionesAsync(bool soloActivos, bool soloInsumos, bool soloProductoTerminado = false);
    Task<GenericResponse<AlmacenResponse>> CrearAsync(AlmacenRequest request);
    Task<GenericResponse<AlmacenResponse>> ActualizarAsync(int id, AlmacenRequest request);
    Task<GenericResponse<AlmacenResponse>> CambiarEstadoAsync(int id, bool activo);
    Task<GenericResponse<object>> EliminarAsync(int id);
}

public interface ILineaService
{
    Task<GenericResponse<ListadoResponse<LineaResponse>>> ListarAsync(FiltroLineaRequest filtro);
    Task<GenericResponse<List<FormatoResponse>>> FormatosAsync();
    Task<GenericResponse<LineaResponse>> ObtenerAsync(int id);
    Task<GenericResponse<LineaResponse>> CrearAsync(LineaRequest request);
    Task<GenericResponse<LineaResponse>> ActualizarAsync(int id, LineaRequest request);
    Task<GenericResponse<LineaResponse>> CambiarEstadoAsync(int id, bool activo);
    Task<GenericResponse<object>> EliminarAsync(int id);
}

public interface IUsuarioService
{
    Task<GenericResponse<ListadoResponse<UsuarioResponse>>> ListarAsync(FiltroUsuarioRequest filtro);
    Task<GenericResponse<UsuarioResponse>> ObtenerAsync(string id);
    Task<GenericResponse<UsuarioResponse>> CrearAsync(UsuarioCrearRequest request);
    Task<GenericResponse<UsuarioResponse>> ActualizarAsync(string id, UsuarioActualizarRequest request);
    Task<GenericResponse<UsuarioResponse>> CambiarEstadoAsync(string id, bool activo);
    Task<GenericResponse<object>> RestablecerClaveAsync(string id, RestablecerClaveRequest request);
}

public interface IInicioService
{
    Task<GenericResponse<ResumenInicioResponse>> ResumenAsync();
}

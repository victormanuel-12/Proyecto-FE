using SPIn.Common;
using SPIn.Common.Excepciones;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;

namespace SPIn.Services.ServiceImpl;

public class AlmacenService(IAlmacenRepository repo, IPlantaRepository plantas, IInventarioRepository inventario, IVentaRepository ventas, ILogger<AlmacenService> logger) : IAlmacenService
{
    public async Task<GenericResponse<ListadoResponse<AlmacenResponse>>> ListarAsync(FiltroAlmacenRequest filtro) =>
        new(await repo.ListarAsync(filtro));

    public async Task<GenericResponse<AlmacenResponse>> ObtenerAsync(int id) =>
        new(await repo.ObtenerResumenAsync(id) ?? throw GenericException.NoEncontrado("El almacén no existe."));

    public async Task<GenericResponse<List<OpcionResponse>>> OpcionesAsync(bool soloActivos, bool soloInsumos, bool soloProductoTerminado = false) =>
        new(await repo.OpcionesAsync(soloActivos, soloInsumos && !soloProductoTerminado, soloProductoTerminado));

    public async Task<GenericResponse<AlmacenResponse>> CrearAsync(AlmacenRequest request)
    {
        var codigo = Validaciones.NormalizarCodigo(request.Codigo);
        await ValidarPlantaActivaAsync(request.PlantaId);
        if (await repo.ExisteCodigoAsync(codigo, null))
            throw GenericException.Conflicto($"Ya existe un almacén con el código {codigo}.");

        var almacen = new Almacen
        {
            Codigo = codigo,
            Nombre = request.Nombre.Trim(),
            PlantaId = request.PlantaId,
            Tipo = request.Tipo,
            Activo = true
        };
        repo.Agregar(almacen);
        await repo.GuardarAsync();
        logger.LogInformation("Almacén {AlmacenId} ({Codigo}) creado", almacen.Id, almacen.Codigo);

        return new GenericResponse<AlmacenResponse>(true, (await repo.ObtenerResumenAsync(almacen.Id))!, "Almacén registrado.");
    }

    public async Task<GenericResponse<AlmacenResponse>> ActualizarAsync(int id, AlmacenRequest request)
    {
        var almacen = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("El almacén no existe.");
        var codigo = Validaciones.NormalizarCodigo(request.Codigo);
        if (almacen.PlantaId != request.PlantaId) await ValidarPlantaActivaAsync(request.PlantaId);
        if (await repo.ExisteCodigoAsync(codigo, id))
            throw GenericException.Conflicto($"Ya existe otro almacén con el código {codigo}.");

        if (almacen.Tipo != request.Tipo && request.Tipo == TipoAlmacen.ProductoTerminado &&
            await inventario.AlmacenTieneMovimientosAsync(id))
            throw GenericException.Campo("tipo", "El almacén ya tiene movimientos de insumos: no puede pasar a ser de producto terminado.");
        if (almacen.Tipo == TipoAlmacen.ProductoTerminado && request.Tipo != TipoAlmacen.ProductoTerminado &&
            await ventas.AlmacenTieneMovimientosProductoAsync(id))
            throw GenericException.Campo("tipo", "El almacén ya tiene movimientos de producto terminado: no puede cambiar de tipo.");

        almacen.Codigo = codigo;
        almacen.Nombre = request.Nombre.Trim();
        almacen.PlantaId = request.PlantaId;
        almacen.Tipo = request.Tipo;
        await repo.GuardarAsync();

        return new GenericResponse<AlmacenResponse>(true, (await repo.ObtenerResumenAsync(id))!, "Almacén actualizado.");
    }

    public async Task<GenericResponse<AlmacenResponse>> CambiarEstadoAsync(int id, bool activo)
    {
        var almacen = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("El almacén no existe.");
        if (activo) await ValidarPlantaActivaAsync(almacen.PlantaId);
        if (!activo && await inventario.AlmacenTieneStockAsync(id))
            throw GenericException.Conflicto("El almacén tiene stock. Traslade o ajuste ese stock antes de desactivarlo.");
        if (!activo && await ventas.AlmacenTieneStockProductoAsync(id))
            throw GenericException.Conflicto("El almacén tiene stock de producto terminado. Ajústelo antes de desactivarlo.");

        almacen.Activo = activo;
        await repo.GuardarAsync();
        return new GenericResponse<AlmacenResponse>(true, (await repo.ObtenerResumenAsync(id))!,
            activo ? "Almacén activado." : "Almacén desactivado.");
    }

    public async Task<GenericResponse<object>> EliminarAsync(int id)
    {
        var almacen = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("El almacén no existe.");
        if (await inventario.AlmacenTieneMovimientosAsync(id))
            throw GenericException.Conflicto("No se puede eliminar: el almacén tiene movimientos en su kardex. Puede desactivarlo.");
        if (await ventas.AlmacenTieneMovimientosProductoAsync(id))
            throw GenericException.Conflicto("No se puede eliminar: el almacén tiene movimientos de producto terminado. Puede desactivarlo.");
        repo.Eliminar(almacen);
        await repo.GuardarAsync();
        logger.LogInformation("Almacén {AlmacenId} ({Codigo}) eliminado", id, almacen.Codigo);
        return new GenericResponse<object>(true, "Almacén eliminado.");
    }

    private async Task ValidarPlantaActivaAsync(int plantaId)
    {
        var planta = await plantas.ObtenerAsync(plantaId)
            ?? throw new GenericException("La planta seleccionada no existe.");
        if (!planta.Activo)
            throw new GenericException("La planta seleccionada está inactiva.");
    }
}

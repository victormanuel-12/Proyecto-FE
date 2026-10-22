using SPIn.Common;
using SPIn.Common.Excepciones;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;

namespace SPIn.Services.ServiceImpl;

public class PlantaService(IPlantaRepository repo, ILogger<PlantaService> logger) : IPlantaService
{
    public async Task<GenericResponse<ListadoResponse<PlantaResponse>>> ListarAsync(FiltroListadoRequest filtro) =>
        new(await repo.ListarAsync(filtro));

    public async Task<GenericResponse<List<OpcionResponse>>> OpcionesAsync(bool soloActivas) =>
        new(await repo.OpcionesAsync(soloActivas));

    public async Task<GenericResponse<PlantaResponse>> ObtenerAsync(int id) =>
        new(await repo.ObtenerResumenAsync(id) ?? throw GenericException.NoEncontrado("La planta no existe."));

    public async Task<GenericResponse<PlantaResponse>> CrearAsync(PlantaRequest request)
    {
        var codigo = Validaciones.NormalizarCodigo(request.Codigo);
        if (await repo.ExisteCodigoAsync(codigo, null))
            throw GenericException.Conflicto($"Ya existe una planta con el código {codigo}.");

        var planta = new Planta
        {
            Codigo = codigo,
            Nombre = request.Nombre.Trim(),
            Direccion = Validaciones.TextoOpcional(request.Direccion),
            Activo = true
        };
        repo.Agregar(planta);
        await repo.GuardarAsync();
        logger.LogInformation("Planta {PlantaId} ({Codigo}) creada", planta.Id, planta.Codigo);

        return new GenericResponse<PlantaResponse>(true, (await repo.ObtenerResumenAsync(planta.Id))!, "Planta registrada.");
    }

    public async Task<GenericResponse<PlantaResponse>> ActualizarAsync(int id, PlantaRequest request)
    {
        var planta = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("La planta no existe.");
        var codigo = Validaciones.NormalizarCodigo(request.Codigo);
        if (await repo.ExisteCodigoAsync(codigo, id))
            throw GenericException.Conflicto($"Ya existe otra planta con el código {codigo}.");

        planta.Codigo = codigo;
        planta.Nombre = request.Nombre.Trim();
        planta.Direccion = Validaciones.TextoOpcional(request.Direccion);
        await repo.GuardarAsync();

        return new GenericResponse<PlantaResponse>(true, (await repo.ObtenerResumenAsync(id))!, "Planta actualizada.");
    }

    public async Task<GenericResponse<PlantaResponse>> CambiarEstadoAsync(int id, bool activo)
    {
        var planta = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("La planta no existe.");
        if (!activo && await repo.TieneElementosActivosAsync(id))
            throw GenericException.Conflicto("Desactive primero los almacenes y las líneas activas de esta planta.");

        planta.Activo = activo;
        await repo.GuardarAsync();
        return new GenericResponse<PlantaResponse>(true, (await repo.ObtenerResumenAsync(id))!,
            activo ? "Planta activada." : "Planta desactivada.");
    }

    public async Task<GenericResponse<object>> EliminarAsync(int id)
    {
        var planta = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("La planta no existe.");
        if (await repo.TieneDependenciasAsync(id))
            throw GenericException.Conflicto("No se puede eliminar: la planta tiene almacenes o líneas registrados. Puede desactivarla.");

        repo.Eliminar(planta);
        await repo.GuardarAsync();
        logger.LogInformation("Planta {PlantaId} ({Codigo}) eliminada", id, planta.Codigo);
        return new GenericResponse<object>(true, "Planta eliminada.");
    }
}

using SPIn.Common;
using SPIn.Common.Excepciones;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;

namespace SPIn.Services.ServiceImpl;

public class LineaService(ILineaRepository repo, IPlantaRepository plantas, IProduccionOperacionRepository produccion, ILogger<LineaService> logger) : ILineaService
{
    public async Task<GenericResponse<ListadoResponse<LineaResponse>>> ListarAsync(FiltroLineaRequest filtro) =>
        new(await repo.ListarAsync(filtro));

    public async Task<GenericResponse<List<FormatoResponse>>> FormatosAsync() =>
        new(await repo.ListarFormatosAsync());

    public async Task<GenericResponse<LineaResponse>> ObtenerAsync(int id) =>
        new(await repo.ObtenerResumenAsync(id) ?? throw GenericException.NoEncontrado("La línea no existe."));

    public async Task<GenericResponse<LineaResponse>> CrearAsync(LineaRequest request)
    {
        var codigo = Validaciones.NormalizarCodigo(request.Codigo);
        await ValidarPlantaActivaAsync(request.PlantaId);
        await ValidarFormatosAsync(request);
        if (await repo.ExisteCodigoAsync(codigo, null))
            throw GenericException.Conflicto($"Ya existe una línea con el código {codigo}.");

        var linea = new LineaProduccion
        {
            Codigo = codigo,
            Nombre = request.Nombre.Trim(),
            PlantaId = request.PlantaId,
            TipoEnvase = request.TipoEnvase,
            VelocidadNominal = request.VelocidadNominal,
            Activo = true
        };
        foreach (var formatoId in request.FormatoIds.Distinct())
            linea.Formatos.Add(new LineaFormato { FormatoId = formatoId });

        repo.Agregar(linea);
        await repo.GuardarAsync();
        logger.LogInformation("Línea {LineaId} ({Codigo}) creada", linea.Id, linea.Codigo);

        return new GenericResponse<LineaResponse>(true, (await repo.ObtenerResumenAsync(linea.Id))!, "Línea registrada.");
    }

    public async Task<GenericResponse<LineaResponse>> ActualizarAsync(int id, LineaRequest request)
    {
        var linea = await repo.ObtenerConFormatosAsync(id) ?? throw GenericException.NoEncontrado("La línea no existe.");
        var codigo = Validaciones.NormalizarCodigo(request.Codigo);
        if (linea.PlantaId != request.PlantaId) await ValidarPlantaActivaAsync(request.PlantaId);
        await ValidarFormatosAsync(request);
        if (await repo.ExisteCodigoAsync(codigo, id))
            throw GenericException.Conflicto($"Ya existe otra línea con el código {codigo}.");

        linea.Codigo = codigo;
        linea.Nombre = request.Nombre.Trim();
        linea.PlantaId = request.PlantaId;
        linea.TipoEnvase = request.TipoEnvase;
        linea.VelocidadNominal = request.VelocidadNominal;

        // Se actualizan solo las diferencias para no volver a insertar formatos que ya estaban.
        var nuevos = request.FormatoIds.Distinct().ToHashSet();
        foreach (var quitar in linea.Formatos.Where(f => !nuevos.Contains(f.FormatoId)).ToList())
            linea.Formatos.Remove(quitar);
        foreach (var agregar in nuevos.Where(fid => linea.Formatos.All(f => f.FormatoId != fid)))
            linea.Formatos.Add(new LineaFormato { LineaProduccionId = linea.Id, FormatoId = agregar });

        await repo.GuardarAsync();
        return new GenericResponse<LineaResponse>(true, (await repo.ObtenerResumenAsync(id))!, "Línea actualizada.");
    }

    public async Task<GenericResponse<LineaResponse>> CambiarEstadoAsync(int id, bool activo)
    {
        var linea = await repo.ObtenerConFormatosAsync(id) ?? throw GenericException.NoEncontrado("La línea no existe.");
        if (activo) await ValidarPlantaActivaAsync(linea.PlantaId);

        linea.Activo = activo;
        await repo.GuardarAsync();
        return new GenericResponse<LineaResponse>(true, (await repo.ObtenerResumenAsync(id))!,
            activo ? "Línea activada." : "Línea desactivada.");
    }

    public async Task<GenericResponse<object>> EliminarAsync(int id)
    {
        var linea = await repo.ObtenerConFormatosAsync(id) ?? throw GenericException.NoEncontrado("La línea no existe.");
        if (await produccion.LineaUsadaAsync(id))
            throw GenericException.Conflicto("No se puede eliminar: la línea tiene producción registrada o programada. Puede desactivarla.");
        repo.Eliminar(linea);
        await repo.GuardarAsync();
        logger.LogInformation("Línea {LineaId} ({Codigo}) eliminada", id, linea.Codigo);
        return new GenericResponse<object>(true, "Línea eliminada.");
    }

    private async Task ValidarPlantaActivaAsync(int plantaId)
    {
        var planta = await plantas.ObtenerAsync(plantaId)
            ?? throw new GenericException("La planta seleccionada no existe.");
        if (!planta.Activo)
            throw new GenericException("La planta seleccionada está inactiva.");
    }

    private async Task ValidarFormatosAsync(LineaRequest request)
    {
        var ids = request.FormatoIds.Distinct().ToList();
        var formatos = await repo.FormatosPorIdsAsync(ids);
        if (formatos.Count != ids.Count)
            throw new GenericException("Alguno de los formatos seleccionados no existe.");
        if (formatos.Any(f => f.TipoEnvase != request.TipoEnvase))
            throw new GenericException($"Todos los formatos deben ser de envase {Etiquetas.De(request.TipoEnvase)}.");
    }
}

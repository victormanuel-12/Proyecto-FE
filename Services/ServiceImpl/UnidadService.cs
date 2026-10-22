using SPIn.Common.Excepciones;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;

namespace SPIn.Services.ServiceImpl;

public class UnidadService(IUnidadRepository repo) : IUnidadService
{
    public async Task<GenericResponse<ListadoResponse<UnidadResponse>>> ListarAsync(FiltroListadoRequest filtro) =>
        new(await repo.ListarAsync(filtro));

    public async Task<GenericResponse<List<OpcionResponse>>> OpcionesAsync(bool soloActivas) =>
        new(await repo.OpcionesAsync(soloActivas));

    public async Task<GenericResponse<UnidadResponse>> CrearAsync(UnidadRequest request)
    {
        var codigo = request.Codigo.Trim().ToUpperInvariant();
        if (await repo.ExisteCodigoAsync(codigo, null))
            throw GenericException.Campo("codigo", $"Ya existe una unidad con el código {codigo}.");

        var unidad = new UnidadMedida { Codigo = codigo, Nombre = request.Nombre.Trim(), Activo = true };
        repo.Agregar(unidad);
        await repo.GuardarAsync();
        return new GenericResponse<UnidadResponse>(true, (await repo.ObtenerResumenAsync(unidad.Id))!, "Unidad registrada.");
    }

    public async Task<GenericResponse<UnidadResponse>> ActualizarAsync(int id, UnidadRequest request)
    {
        var unidad = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("La unidad no existe.");
        var codigo = request.Codigo.Trim().ToUpperInvariant();
        if (await repo.ExisteCodigoAsync(codigo, id))
            throw GenericException.Campo("codigo", $"Ya existe otra unidad con el código {codigo}.");

        unidad.Codigo = codigo;
        unidad.Nombre = request.Nombre.Trim();
        await repo.GuardarAsync();
        return new GenericResponse<UnidadResponse>(true, (await repo.ObtenerResumenAsync(id))!, "Unidad actualizada.");
    }

    public async Task<GenericResponse<UnidadResponse>> CambiarEstadoAsync(int id, bool activo)
    {
        var unidad = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("La unidad no existe.");
        unidad.Activo = activo;
        await repo.GuardarAsync();
        return new GenericResponse<UnidadResponse>(true, (await repo.ObtenerResumenAsync(id))!,
            activo ? "Unidad activada." : "Unidad desactivada. No se podrá elegir para insumos nuevos.");
    }

    public async Task<GenericResponse<object>> EliminarAsync(int id)
    {
        var unidad = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("La unidad no existe.");
        if (await repo.TieneInsumosAsync(id))
            throw GenericException.Conflicto("No se puede eliminar: hay insumos que usan esta unidad. Puede desactivarla.");
        repo.Eliminar(unidad);
        await repo.GuardarAsync();
        return new GenericResponse<object>(true, "Unidad eliminada.");
    }
}

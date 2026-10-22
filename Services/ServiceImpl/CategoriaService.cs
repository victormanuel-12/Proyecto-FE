using SPIn.Common;
using SPIn.Common.Excepciones;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;

namespace SPIn.Services.ServiceImpl;

public class CategoriaService(ICategoriaRepository repo) : ICategoriaService
{
    public async Task<GenericResponse<ListadoResponse<CategoriaResponse>>> ListarAsync(FiltroListadoRequest filtro) =>
        new(await repo.ListarAsync(filtro));

    public async Task<GenericResponse<List<OpcionResponse>>> OpcionesAsync(bool soloActivas) =>
        new(await repo.OpcionesAsync(soloActivas));

    public async Task<GenericResponse<CategoriaResponse>> CrearAsync(CategoriaRequest request)
    {
        var nombre = request.Nombre.Trim();
        if (await repo.ExisteNombreAsync(nombre, null))
            throw GenericException.Campo("nombre", "Ya existe una categoría con ese nombre.");

        var categoria = new CategoriaInsumo { Nombre = nombre, Descripcion = Validaciones.TextoOpcional(request.Descripcion), Activo = true };
        repo.Agregar(categoria);
        await repo.GuardarAsync();
        return new GenericResponse<CategoriaResponse>(true, (await repo.ObtenerResumenAsync(categoria.Id))!, "Categoría registrada.");
    }

    public async Task<GenericResponse<CategoriaResponse>> ActualizarAsync(int id, CategoriaRequest request)
    {
        var categoria = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("La categoría no existe.");
        var nombre = request.Nombre.Trim();
        if (await repo.ExisteNombreAsync(nombre, id))
            throw GenericException.Campo("nombre", "Ya existe otra categoría con ese nombre.");

        categoria.Nombre = nombre;
        categoria.Descripcion = Validaciones.TextoOpcional(request.Descripcion);
        await repo.GuardarAsync();
        return new GenericResponse<CategoriaResponse>(true, (await repo.ObtenerResumenAsync(id))!, "Categoría actualizada.");
    }

    public async Task<GenericResponse<CategoriaResponse>> CambiarEstadoAsync(int id, bool activo)
    {
        var categoria = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("La categoría no existe.");
        categoria.Activo = activo;
        await repo.GuardarAsync();
        return new GenericResponse<CategoriaResponse>(true, (await repo.ObtenerResumenAsync(id))!,
            activo ? "Categoría activada." : "Categoría desactivada. Sus insumos la conservan, pero no se podrá elegir para insumos nuevos.");
    }

    public async Task<GenericResponse<object>> EliminarAsync(int id)
    {
        var categoria = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("La categoría no existe.");
        if (await repo.TieneInsumosAsync(id))
            throw GenericException.Conflicto("No se puede eliminar: hay insumos en esta categoría. Puede desactivarla.");
        repo.Eliminar(categoria);
        await repo.GuardarAsync();
        return new GenericResponse<object>(true, "Categoría eliminada.");
    }
}

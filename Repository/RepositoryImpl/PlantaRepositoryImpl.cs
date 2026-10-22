using Microsoft.EntityFrameworkCore;
using SPIn.Data;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository.RepositoryImpl;

public class PlantaRepositoryImpl(ApplicationDbContext db) : IPlantaRepository
{
    private static IQueryable<PlantaResponse> Proyectar(IQueryable<Planta> consulta) =>
        consulta.Select(p => new PlantaResponse
        {
            Id = p.Id,
            Codigo = p.Codigo,
            Nombre = p.Nombre,
            Direccion = p.Direccion,
            Activo = p.Activo,
            Almacenes = p.Almacenes.Count,
            Lineas = p.Lineas.Count
        });

    public async Task<ListadoResponse<PlantaResponse>> ListarAsync(FiltroListadoRequest filtro)
    {
        var consulta = db.Plantas.AsNoTracking();

        var texto = ConsultaExtensions.TextoBusqueda(filtro.Buscar);
        if (texto is not null)
            consulta = consulta.Where(p => p.Codigo.ToLower().Contains(texto) || p.Nombre.ToLower().Contains(texto));

        var activos = await consulta.CountAsync(p => p.Activo);
        var inactivos = await consulta.CountAsync(p => !p.Activo);

        if (filtro.Estado == "activos") consulta = consulta.Where(p => p.Activo);
        else if (filtro.Estado == "inactivos") consulta = consulta.Where(p => !p.Activo);

        return await Proyectar(consulta.OrderBy(p => p.Nombre))
            .PaginarAsync(filtro.Pagina, filtro.TamanoPagina, activos, inactivos);
    }

    public Task<List<OpcionResponse>> OpcionesAsync(bool soloActivas) =>
        db.Plantas.AsNoTracking()
            .Where(p => !soloActivas || p.Activo)
            .OrderBy(p => p.Nombre)
            .Select(p => new OpcionResponse { Id = p.Id, Nombre = p.Nombre })
            .ToListAsync();

    public Task<PlantaResponse?> ObtenerResumenAsync(int id) =>
        Proyectar(db.Plantas.AsNoTracking().Where(p => p.Id == id)).FirstOrDefaultAsync();

    public Task<Planta?> ObtenerAsync(int id) => db.Plantas.FirstOrDefaultAsync(p => p.Id == id);

    public Task<bool> ExisteCodigoAsync(string codigo, int? excluirId) =>
        db.Plantas.AnyAsync(p => p.Codigo == codigo && (excluirId == null || p.Id != excluirId));

    public async Task<bool> TieneElementosActivosAsync(int id) =>
        await db.Almacenes.AnyAsync(a => a.PlantaId == id && a.Activo) ||
        await db.Lineas.AnyAsync(l => l.PlantaId == id && l.Activo);

    public async Task<bool> TieneDependenciasAsync(int id) =>
        await db.Almacenes.AnyAsync(a => a.PlantaId == id) ||
        await db.Lineas.AnyAsync(l => l.PlantaId == id);

    public Task<int> ContarAsync() => db.Plantas.CountAsync(p => p.Activo);

    public void Agregar(Planta planta) => db.Plantas.Add(planta);

    public void Eliminar(Planta planta) => db.Plantas.Remove(planta);

    public Task GuardarAsync() => db.SaveChangesAsync();
}

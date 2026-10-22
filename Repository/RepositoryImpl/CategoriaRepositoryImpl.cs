using Microsoft.EntityFrameworkCore;
using SPIn.Data;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository.RepositoryImpl;

public class CategoriaRepositoryImpl(ApplicationDbContext db) : ICategoriaRepository
{
    private IQueryable<CategoriaResponse> Proyectar(IQueryable<CategoriaInsumo> consulta) =>
        consulta.Select(c => new CategoriaResponse
        {
            Id = c.Id,
            Nombre = c.Nombre,
            Descripcion = c.Descripcion,
            Activo = c.Activo,
            Insumos = db.Insumos.Count(i => i.CategoriaId == c.Id)
        });

    public async Task<ListadoResponse<CategoriaResponse>> ListarAsync(FiltroListadoRequest filtro)
    {
        var consulta = db.Categorias.AsNoTracking();
        var texto = ConsultaExtensions.TextoBusqueda(filtro.Buscar);
        if (texto is not null) consulta = consulta.Where(c => c.Nombre.ToLower().Contains(texto));

        var activos = await consulta.CountAsync(c => c.Activo);
        var inactivos = await consulta.CountAsync(c => !c.Activo);
        if (filtro.Estado == "activos") consulta = consulta.Where(c => c.Activo);
        else if (filtro.Estado == "inactivos") consulta = consulta.Where(c => !c.Activo);

        return await Proyectar(consulta.OrderBy(c => c.Nombre))
            .PaginarAsync(filtro.Pagina, filtro.TamanoPagina, activos, inactivos);
    }

    public Task<List<OpcionResponse>> OpcionesAsync(bool soloActivas) =>
        db.Categorias.AsNoTracking()
            .Where(c => !soloActivas || c.Activo)
            .OrderBy(c => c.Nombre)
            .Select(c => new OpcionResponse { Id = c.Id, Nombre = c.Nombre })
            .ToListAsync();

    public Task<CategoriaResponse?> ObtenerResumenAsync(int id) =>
        Proyectar(db.Categorias.AsNoTracking().Where(c => c.Id == id)).FirstOrDefaultAsync();

    public Task<CategoriaInsumo?> ObtenerAsync(int id) => db.Categorias.FirstOrDefaultAsync(c => c.Id == id);

    public Task<bool> ExisteNombreAsync(string nombre, int? excluirId)
    {
        var buscado = nombre.ToLower();
        return db.Categorias.AnyAsync(c => c.Nombre.ToLower() == buscado && (excluirId == null || c.Id != excluirId));
    }

    public Task<bool> TieneInsumosAsync(int id) => db.Insumos.AnyAsync(i => i.CategoriaId == id);

    public void Agregar(CategoriaInsumo categoria) => db.Categorias.Add(categoria);
    public void Eliminar(CategoriaInsumo categoria) => db.Categorias.Remove(categoria);
    public Task GuardarAsync() => db.SaveChangesAsync();
}

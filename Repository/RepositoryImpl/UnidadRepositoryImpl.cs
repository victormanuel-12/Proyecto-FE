using Microsoft.EntityFrameworkCore;
using SPIn.Data;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository.RepositoryImpl;

public class UnidadRepositoryImpl(ApplicationDbContext db) : IUnidadRepository
{
    private IQueryable<UnidadResponse> Proyectar(IQueryable<UnidadMedida> consulta) =>
        consulta.Select(u => new UnidadResponse
        {
            Id = u.Id,
            Codigo = u.Codigo,
            Nombre = u.Nombre,
            Activo = u.Activo,
            Insumos = db.Insumos.Count(i => i.UnidadCompraId == u.Id || i.UnidadConsumoId == u.Id)
        });

    public async Task<ListadoResponse<UnidadResponse>> ListarAsync(FiltroListadoRequest filtro)
    {
        var consulta = db.Unidades.AsNoTracking();
        var texto = ConsultaExtensions.TextoBusqueda(filtro.Buscar);
        if (texto is not null)
            consulta = consulta.Where(u => u.Codigo.ToLower().Contains(texto) || u.Nombre.ToLower().Contains(texto));

        var activos = await consulta.CountAsync(u => u.Activo);
        var inactivos = await consulta.CountAsync(u => !u.Activo);
        if (filtro.Estado == "activos") consulta = consulta.Where(u => u.Activo);
        else if (filtro.Estado == "inactivos") consulta = consulta.Where(u => !u.Activo);

        return await Proyectar(consulta.OrderBy(u => u.Nombre))
            .PaginarAsync(filtro.Pagina, filtro.TamanoPagina, activos, inactivos);
    }

    public Task<List<OpcionResponse>> OpcionesAsync(bool soloActivas) =>
        db.Unidades.AsNoTracking()
            .Where(u => !soloActivas || u.Activo)
            .OrderBy(u => u.Nombre)
            .Select(u => new OpcionResponse { Id = u.Id, Nombre = u.Nombre + " (" + u.Codigo + ")" })
            .ToListAsync();

    public Task<UnidadResponse?> ObtenerResumenAsync(int id) =>
        Proyectar(db.Unidades.AsNoTracking().Where(u => u.Id == id)).FirstOrDefaultAsync();

    public Task<UnidadMedida?> ObtenerAsync(int id) => db.Unidades.FirstOrDefaultAsync(u => u.Id == id);

    public Task<bool> ExisteCodigoAsync(string codigo, int? excluirId) =>
        db.Unidades.AnyAsync(u => u.Codigo == codigo && (excluirId == null || u.Id != excluirId));

    public Task<bool> TieneInsumosAsync(int id) =>
        db.Insumos.AnyAsync(i => i.UnidadCompraId == id || i.UnidadConsumoId == id);

    public void Agregar(UnidadMedida unidad) => db.Unidades.Add(unidad);
    public void Eliminar(UnidadMedida unidad) => db.Unidades.Remove(unidad);
    public Task GuardarAsync() => db.SaveChangesAsync();
}

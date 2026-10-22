using Microsoft.EntityFrameworkCore;
using SPIn.Data;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository.RepositoryImpl;

public class EmpresaRepositoryImpl(ApplicationDbContext db) : IEmpresaRepository
{
    // Las empresas no tienen filtro por empresa; IgnoreQueryFilters permite contar plantas de todas.
    private IQueryable<EmpresaResponse> Proyectar(IQueryable<Empresa> consulta) =>
        consulta.Select(e => new EmpresaResponse
        {
            Id = e.Id,
            Ruc = e.Ruc,
            RazonSocial = e.RazonSocial,
            NombreComercial = e.NombreComercial,
            Activo = e.Activo,
            Plantas = db.Plantas.Count(p => p.EmpresaId == e.Id),
            Usuarios = db.Users.Count(u => u.EmpresaId == e.Id)
        });

    public async Task<ListadoResponse<EmpresaResponse>> ListarAsync(FiltroListadoRequest filtro)
    {
        var consulta = db.Empresas.IgnoreQueryFilters().AsNoTracking();

        var texto = ConsultaExtensions.TextoBusqueda(filtro.Buscar);
        if (texto is not null)
        {
            consulta = consulta.Where(e =>
                e.Ruc.Contains(texto) ||
                e.RazonSocial.ToLower().Contains(texto) ||
                (e.NombreComercial != null && e.NombreComercial.ToLower().Contains(texto)));
        }

        var activos = await consulta.CountAsync(e => e.Activo);
        var inactivos = await consulta.CountAsync(e => !e.Activo);

        if (filtro.Estado == "activos") consulta = consulta.Where(e => e.Activo);
        else if (filtro.Estado == "inactivos") consulta = consulta.Where(e => !e.Activo);

        return await Proyectar(consulta.OrderBy(e => e.RazonSocial))
            .PaginarAsync(filtro.Pagina, filtro.TamanoPagina, activos, inactivos);
    }

    public Task<List<OpcionResponse>> OpcionesAsync() =>
        db.Empresas.AsNoTracking()
            .Where(e => e.Activo)
            .OrderBy(e => e.RazonSocial)
            .Select(e => new OpcionResponse { Id = e.Id, Nombre = e.NombreComercial ?? e.RazonSocial })
            .ToListAsync();

    public Task<EmpresaResponse?> ObtenerResumenAsync(int id) =>
        Proyectar(db.Empresas.IgnoreQueryFilters().AsNoTracking().Where(e => e.Id == id)).FirstOrDefaultAsync();

    public Task<Empresa?> ObtenerAsync(int id) => db.Empresas.FirstOrDefaultAsync(e => e.Id == id);

    public Task<bool> ExisteRucAsync(string ruc, int? excluirId) =>
        db.Empresas.AnyAsync(e => e.Ruc == ruc && (excluirId == null || e.Id != excluirId));

    public async Task CrearCatalogoInicialAsync(int empresaId)
    {
        await CatalogoInicial.AgregarSiFaltaAsync(db, empresaId);
        await db.SaveChangesAsync();
    }

    public void Agregar(Empresa empresa) => db.Empresas.Add(empresa);

    public Task GuardarAsync() => db.SaveChangesAsync();
}

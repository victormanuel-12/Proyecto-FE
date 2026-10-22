using Microsoft.EntityFrameworkCore;
using SPIn.Data;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository.RepositoryImpl;

public class UsuarioRepositoryImpl(ApplicationDbContext db) : IUsuarioRepository
{
    private IQueryable<UsuarioResponse> Proyectar(IQueryable<ApplicationUser> consulta) =>
        consulta.Select(u => new UsuarioResponse
        {
            Id = u.Id,
            NombreCompleto = u.NombreCompleto,
            Email = u.Email ?? string.Empty,
            Rol = (from ur in db.UserRoles
                   join r in db.Roles on ur.RoleId equals r.Id
                   where ur.UserId == u.Id
                   select r.Name).FirstOrDefault() ?? string.Empty,
            EmpresaId = u.EmpresaId,
            EmpresaNombre = u.Empresa!.NombreComercial ?? u.Empresa.RazonSocial,
            Activo = u.Activo,
            BloqueoHasta = u.LockoutEnd
        });

    public async Task<ListadoResponse<UsuarioResponse>> ListarAsync(FiltroUsuarioRequest filtro, int? empresaId)
    {
        var consulta = db.Users.AsNoTracking();

        if (empresaId is int id) consulta = consulta.Where(u => u.EmpresaId == id);

        var texto = ConsultaExtensions.TextoBusqueda(filtro.Buscar);
        if (texto is not null)
        {
            consulta = consulta.Where(u =>
                u.NombreCompleto.ToLower().Contains(texto) ||
                (u.Email != null && u.Email.ToLower().Contains(texto)));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Rol))
        {
            var rol = filtro.Rol;
            consulta = consulta.Where(u => db.UserRoles.Any(ur =>
                ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == rol)));
        }

        var activos = await consulta.CountAsync(u => u.Activo);
        var inactivos = await consulta.CountAsync(u => !u.Activo);

        if (filtro.Estado == "activos") consulta = consulta.Where(u => u.Activo);
        else if (filtro.Estado == "inactivos") consulta = consulta.Where(u => !u.Activo);

        return await Proyectar(consulta.OrderBy(u => u.NombreCompleto))
            .PaginarAsync(filtro.Pagina, filtro.TamanoPagina, activos, inactivos);
    }

    public Task<UsuarioResponse?> ObtenerResumenAsync(string id) =>
        Proyectar(db.Users.AsNoTracking().Where(u => u.Id == id)).FirstOrDefaultAsync();

    public Task<int> ContarActivosAsync(int empresaId) =>
        db.Users.CountAsync(u => u.EmpresaId == empresaId && u.Activo);
}

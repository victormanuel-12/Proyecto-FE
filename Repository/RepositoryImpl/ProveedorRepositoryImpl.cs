using Microsoft.EntityFrameworkCore;
using SPIn.Data;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository.RepositoryImpl;

public class ProveedorRepositoryImpl(ApplicationDbContext db) : IProveedorRepository
{
    public const string PaisLocal = "Perú";

    private static IQueryable<ProveedorResponse> Proyectar(IQueryable<Proveedor> consulta) =>
        consulta.Select(p => new ProveedorResponse
        {
            Id = p.Id,
            Pais = p.Pais,
            Ruc = p.Ruc,
            RazonSocial = p.RazonSocial,
            NombreComercial = p.NombreComercial,
            Contacto = p.Contacto,
            Telefono = p.Telefono,
            Email = p.Email,
            EsExtranjero = p.Pais != PaisLocal,
            Activo = p.Activo,
            Insumos = p.Insumos.Count,
            InsumosPrincipal = p.Insumos.Count(c => c.EsPrincipal)
        });

    public async Task<ListadoResponse<ProveedorResponse>> ListarAsync(FiltroProveedorRequest filtro)
    {
        var consulta = db.Proveedores.AsNoTracking();

        var texto = ConsultaExtensions.TextoBusqueda(filtro.Buscar);
        if (texto is not null)
        {
            consulta = consulta.Where(p =>
                p.RazonSocial.ToLower().Contains(texto) ||
                (p.NombreComercial != null && p.NombreComercial.ToLower().Contains(texto)) ||
                (p.Ruc != null && p.Ruc.Contains(texto)));
        }
        if (filtro.Extranjeros == true) consulta = consulta.Where(p => p.Pais != PaisLocal);
        else if (filtro.Extranjeros == false) consulta = consulta.Where(p => p.Pais == PaisLocal);

        var activos = await consulta.CountAsync(p => p.Activo);
        var inactivos = await consulta.CountAsync(p => !p.Activo);
        if (filtro.Estado == "activos") consulta = consulta.Where(p => p.Activo);
        else if (filtro.Estado == "inactivos") consulta = consulta.Where(p => !p.Activo);

        return await Proyectar(consulta.OrderBy(p => p.RazonSocial))
            .PaginarAsync(filtro.Pagina, filtro.TamanoPagina, activos, inactivos);
    }

    public Task<List<OpcionResponse>> OpcionesAsync(bool soloActivos) =>
        db.Proveedores.AsNoTracking()
            .Where(p => !soloActivos || p.Activo)
            .OrderBy(p => p.RazonSocial)
            .Select(p => new OpcionResponse { Id = p.Id, Nombre = p.NombreComercial ?? p.RazonSocial })
            .ToListAsync();

    public Task<ProveedorResponse?> ObtenerResumenAsync(int id) =>
        Proyectar(db.Proveedores.AsNoTracking().Where(p => p.Id == id)).FirstOrDefaultAsync();

    public Task<Proveedor?> ObtenerAsync(int id) => db.Proveedores.FirstOrDefaultAsync(p => p.Id == id);

    public Task<bool> ExisteRucAsync(string ruc, int? excluirId) =>
        db.Proveedores.AnyAsync(p => p.Ruc == ruc && (excluirId == null || p.Id != excluirId));

    public Task<bool> TieneCondicionesAsync(int id) => db.ProveedorInsumos.AnyAsync(c => c.ProveedorId == id);

    public Task<int> ContarInsumosPrincipalAsync(int id) =>
        db.ProveedorInsumos.CountAsync(c => c.ProveedorId == id && c.EsPrincipal && c.Insumo!.Activo);

    public Task<int> ContarAsync() => db.Proveedores.CountAsync(p => p.Activo);

    public void Agregar(Proveedor proveedor) => db.Proveedores.Add(proveedor);
    public void Eliminar(Proveedor proveedor) => db.Proveedores.Remove(proveedor);
    public Task GuardarAsync() => db.SaveChangesAsync();
}

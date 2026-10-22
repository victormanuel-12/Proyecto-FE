using Microsoft.EntityFrameworkCore;
using SPIn.Data;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository.RepositoryImpl;

public class CondicionCompraRepositoryImpl(ApplicationDbContext db) : ICondicionCompraRepository
{
    public async Task<List<CondicionCompraResponse>> ListarPorInsumoAsync(int insumoId)
    {
        var lista = await db.ProveedorInsumos.AsNoTracking()
            .Where(c => c.InsumoId == insumoId)
            .OrderByDescending(c => c.EsPrincipal).ThenBy(c => c.TiempoEntregaDias)
            .Select(c => new CondicionCompraResponse
            {
                Id = c.Id,
                InsumoId = c.InsumoId,
                InsumoCodigo = c.Insumo!.Codigo,
                InsumoNombre = c.Insumo.Nombre,
                ProveedorId = c.ProveedorId,
                ProveedorNombre = c.Proveedor!.NombreComercial ?? c.Proveedor.RazonSocial,
                ProveedorPais = c.Proveedor.Pais,
                ProveedorActivo = c.Proveedor.Activo,
                Moneda = c.Moneda,
                PrecioUnitario = c.PrecioUnitario,
                UnidadCompraCodigo = c.Insumo.UnidadCompra!.Codigo,
                TiempoEntregaDias = c.TiempoEntregaDias,
                VariacionEntregaDias = c.VariacionEntregaDias,
                PedidoMinimo = c.PedidoMinimo,
                MultiploCompra = c.MultiploCompra,
                EsPrincipal = c.EsPrincipal
            })
            .ToListAsync();

        foreach (var c in lista) c.MonedaSimbolo = EtiquetasCatalogo.Simbolo(c.Moneda);
        return lista;
    }

    public Task<List<ProveedorInsumo>> EntidadesPorInsumoAsync(int insumoId) =>
        db.ProveedorInsumos.Include(c => c.Proveedor).Where(c => c.InsumoId == insumoId).ToListAsync();

    public Task<ProveedorInsumo?> ObtenerAsync(int id) => db.ProveedorInsumos.FirstOrDefaultAsync(c => c.Id == id);

    public Task<bool> ExisteParAsync(int proveedorId, int insumoId, int? excluirId) =>
        db.ProveedorInsumos.AnyAsync(c => c.ProveedorId == proveedorId && c.InsumoId == insumoId && (excluirId == null || c.Id != excluirId));

    public void Agregar(ProveedorInsumo condicion) => db.ProveedorInsumos.Add(condicion);
    public void Eliminar(ProveedorInsumo condicion) => db.ProveedorInsumos.Remove(condicion);
    public Task GuardarAsync() => db.SaveChangesAsync();
}

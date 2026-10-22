using Microsoft.EntityFrameworkCore;
using SPIn.Data;
using SPIn.Models;

namespace SPIn.Repository.RepositoryImpl;

public class ImportacionRepositoryImpl(ApplicationDbContext db) : IImportacionRepository
{
    public async Task<List<(int Id, string Nombre)>> CategoriasAsync() =>
        (await db.Categorias.AsNoTracking().Select(c => new { c.Id, c.Nombre }).ToListAsync())
        .Select(c => (c.Id, c.Nombre)).ToList();

    public async Task<List<(int Id, string Codigo, string Nombre)>> UnidadesAsync() =>
        (await db.Unidades.AsNoTracking().Select(u => new { u.Id, u.Codigo, u.Nombre }).ToListAsync())
        .Select(u => (u.Id, u.Codigo, u.Nombre)).ToList();

    public async Task<List<(int Id, string Codigo, string Nombre, bool Activo)>> InsumosAsync() =>
        (await db.Insumos.AsNoTracking().Select(i => new { i.Id, i.Codigo, i.Nombre, i.Activo }).ToListAsync())
        .Select(i => (i.Id, i.Codigo, i.Nombre, i.Activo)).ToList();

    public async Task<List<(int Id, string? Ruc, string RazonSocial, bool Activo)>> ProveedoresAsync() =>
        (await db.Proveedores.AsNoTracking().Select(p => new { p.Id, p.Ruc, p.RazonSocial, p.Activo }).ToListAsync())
        .Select(p => (p.Id, p.Ruc, p.RazonSocial, p.Activo)).ToList();

    public async Task<List<(int Id, int InsumoId, int ProveedorId)>> CondicionesAsync() =>
        (await db.ProveedorInsumos.AsNoTracking().Select(c => new { c.Id, c.InsumoId, c.ProveedorId }).ToListAsync())
        .Select(c => (c.Id, c.InsumoId, c.ProveedorId)).ToList();

    public async Task<List<(int Id, string Nombre)>> FormatosAsync() =>
        (await db.Formatos.AsNoTracking().Select(f => new { f.Id, f.Nombre }).ToListAsync())
        .Select(f => (f.Id, f.Nombre)).ToList();

    public async Task<List<(int Id, string Codigo, string Nombre, int UnidadesPorPaquete, bool Activo)>> ProductosAsync() =>
        (await db.Productos.AsNoTracking().Select(p => new { p.Id, p.Codigo, p.Nombre, p.UnidadesPorPaquete, p.Activo }).ToListAsync())
        .Select(p => (p.Id, p.Codigo, p.Nombre, p.UnidadesPorPaquete, p.Activo)).ToList();

    public async Task<List<(int Id, string Codigo, TipoAlmacen Tipo, bool Activo)>> AlmacenesAsync() =>
        (await db.Almacenes.AsNoTracking().Select(a => new { a.Id, a.Codigo, a.Tipo, a.Activo }).ToListAsync())
        .Select(a => (a.Id, a.Codigo, a.Tipo, a.Activo)).ToList();

    public async Task<HashSet<(int InsumoId, int AlmacenId)>> InsumosConMovimientosAsync() =>
        (await db.Movimientos.AsNoTracking().Select(m => new { m.InsumoId, m.AlmacenId }).Distinct().ToListAsync())
        .Select(m => (m.InsumoId, m.AlmacenId)).ToHashSet();

    public async Task<HashSet<(int ProductoId, int AlmacenId)>> ProductosConMovimientosAsync() =>
        (await db.MovimientosProducto.AsNoTracking().Select(m => new { m.ProductoId, m.AlmacenId }).Distinct().ToListAsync())
        .Select(m => (m.ProductoId, m.AlmacenId)).ToHashSet();

    public async Task<HashSet<int>> InsumosNoProduccionAsync() =>
        (await db.Insumos.AsNoTracking().Where(i => i.TipoConsumo != TipoConsumo.PorProduccion).Select(i => i.Id).ToListAsync()).ToHashSet();

    public async Task<HashSet<int>> ProductosConRecetaAsync() =>
        (await db.Recetas.AsNoTracking().Select(r => r.ProductoId).Distinct().ToListAsync()).ToHashSet();

    public async Task<HashSet<(DateOnly Fecha, int ProductoId, CanalVenta Canal)>> ClavesHistorialVentasAsync(DateOnly desde, DateOnly hasta) =>
        (await db.Ventas.AsNoTracking()
            .Where(v => v.Fecha >= desde && v.Fecha <= hasta && !v.AfectaStock && !v.Anulada)
            .Select(v => new { v.Fecha, v.ProductoId, v.Canal }).Distinct().ToListAsync())
        .Select(v => (v.Fecha, v.ProductoId, v.Canal)).ToHashSet();

    public async Task<T> EnTransaccionAsync<T>(Func<Task<T>> accion)
    {
        if (db.Database.CurrentTransaction is not null) return await accion();
        await using var transaccion = await db.Database.BeginTransactionAsync();
        var resultado = await accion();
        await transaccion.CommitAsync();
        return resultado;
    }
}

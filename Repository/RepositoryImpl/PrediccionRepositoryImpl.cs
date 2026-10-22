using Microsoft.EntityFrameworkCore;
using SPIn.Data;
using SPIn.Models;

namespace SPIn.Repository.RepositoryImpl;

public class PrediccionRepositoryImpl(ApplicationDbContext db) : IPrediccionRepository
{
    public async Task<ConfiguracionEmpresa> ConfiguracionAsync()
    {
        var c = await db.Configuraciones.FirstOrDefaultAsync();
        if (c is not null) return c;
        if (db.EmpresaActualId is null) return new ConfiguracionEmpresa(); // sin empresa: valores por defecto
        c = new ConfiguracionEmpresa();
        db.Configuraciones.Add(c);
        await db.SaveChangesAsync();
        return c;
    }

    public async Task MarcarReentrenoPendienteAsync()
    {
        var c = await ConfiguracionAsync();
        if (c.Id == 0) return;
        c.ReentrenarPendiente = true;
        await db.SaveChangesAsync();
    }

    public async Task<List<(int ProductoId, DateOnly Fecha, decimal Paquetes)>> VentasAsync() =>
        (await db.Ventas.AsNoTracking().Where(v => !v.Anulada)
            .Select(v => new { v.ProductoId, v.Fecha, v.Paquetes }).ToListAsync())
        .Select(v => (v.ProductoId, v.Fecha, v.Paquetes)).ToList();

    public async Task<List<(int Id, string Codigo, string Nombre, bool Activo)>> ProductosAsync() =>
        (await db.Productos.AsNoTracking().OrderBy(p => p.Codigo).Select(p => new { p.Id, p.Codigo, p.Nombre, p.Activo }).ToListAsync())
        .Select(p => (p.Id, p.Codigo, p.Nombre, p.Activo)).ToList();

    public void AgregarEjecucion(EjecucionPronostico ejecucion) => db.EjecucionesPronostico.Add(ejecucion);

    public Task<EjecucionPronostico?> UltimaEjecucionAsync(bool soloExitosas) =>
        db.EjecucionesPronostico.AsNoTracking().Where(e => !soloExitosas || e.Exitosa)
            .OrderByDescending(e => e.Fecha).ThenByDescending(e => e.Id).FirstOrDefaultAsync();

    public Task<List<EjecucionPronostico>> EjecucionesAsync(int cantidad) =>
        db.EjecucionesPronostico.AsNoTracking().OrderByDescending(e => e.Fecha).ThenByDescending(e => e.Id).Take(cantidad).ToListAsync();

    public Task<List<PronosticoProducto>> ResultadosAsync(int ejecucionId) =>
        db.PronosticosProducto.AsNoTracking().Where(r => r.EjecucionId == ejecucionId)
            .Include(r => r.Producto).Include(r => r.Semanas).AsSplitQuery().ToListAsync();

    public Task<PronosticoProducto?> ResultadoAsync(int ejecucionId, int productoId) =>
        db.PronosticosProducto.AsNoTracking().Where(r => r.EjecucionId == ejecucionId && r.ProductoId == productoId)
            .Include(r => r.Producto).Include(r => r.Semanas).AsSplitQuery().FirstOrDefaultAsync();

    public async Task PodarAsync(int conservar)
    {
        var viejas = await db.EjecucionesPronostico.OrderByDescending(e => e.Fecha).ThenByDescending(e => e.Id).Skip(conservar).ToListAsync();
        if (viejas.Count == 0) return;
        db.EjecucionesPronostico.RemoveRange(viejas);
        await db.SaveChangesAsync();
    }

    public async Task<List<InsumoReposicionDatos>> InsumosReposicionAsync()
    {
        var insumos = await db.Insumos.AsNoTracking().Where(i => i.Activo)
            .OrderBy(i => i.Codigo)
            .Select(i => new InsumoReposicionDatos
            {
                Id = i.Id, Codigo = i.Codigo, Nombre = i.Nombre, CategoriaId = i.CategoriaId, CategoriaNombre = i.Categoria!.Nombre,
                UnidadConsumoCodigo = i.UnidadConsumo!.Codigo, UnidadCompraCodigo = i.UnidadCompra!.Codigo,
                FactorConversion = i.FactorConversion, StockMinimo = i.StockMinimo, StockMaximo = i.StockMaximo,
                CapacidadMaxima = i.CapacidadMaxima, EsCritico = i.EsCritico, NivelServicio = i.NivelServicio,
                TipoConsumo = i.TipoConsumo, ConsumoEstimadoDiario = i.ConsumoEstimadoDiario
            }).ToListAsync();

        // Stock disponible: solo almacenes de insumos activos (la cuarentena no está disponible para producir).
        var stocks = await db.Stocks.AsNoTracking()
            .Where(s => s.Almacen!.Tipo == TipoAlmacen.Insumos && s.Almacen.Activo)
            .Select(s => new { s.InsumoId, s.Cantidad }).ToListAsync();
        var porInsumo = stocks.GroupBy(s => s.InsumoId).ToDictionary(g => g.Key, g => g.Sum(x => x.Cantidad));

        var condiciones = await db.ProveedorInsumos.AsNoTracking()
            .Where(c => c.Proveedor!.Activo)
            .Select(c => new
            {
                c.InsumoId,
                Datos = new CondicionReposicion
                {
                    ProveedorId = c.ProveedorId,
                    ProveedorNombre = c.Proveedor!.NombreComercial ?? c.Proveedor.RazonSocial,
                    TiempoEntregaDias = c.TiempoEntregaDias, VariacionEntregaDias = c.VariacionEntregaDias,
                    PedidoMinimo = c.PedidoMinimo, MultiploCompra = c.MultiploCompra, PrecioUnitario = c.PrecioUnitario,
                    Moneda = c.Moneda, EsPrincipal = c.EsPrincipal
                }
            }).ToListAsync();
        // Proveedor principal; si no hay, el de menor tiempo de entrega.
        var condicion = condiciones.GroupBy(c => c.InsumoId).ToDictionary(g => g.Key,
            g => g.OrderByDescending(x => x.Datos.EsPrincipal).ThenBy(x => x.Datos.TiempoEntregaDias).First().Datos);

        foreach (var i in insumos)
        {
            i.Stock = porInsumo.GetValueOrDefault(i.Id);
            i.Condicion = condicion.GetValueOrDefault(i.Id);
        }
        return insumos;
    }

    public async Task<Dictionary<int, decimal>> ConsumoHistoricoDiarioAsync(int dias, DateTime desde)
    {
        TipoMovimiento[] tipos = [TipoMovimiento.SalidaProduccion, TipoMovimiento.SalidaConsumo, TipoMovimiento.Merma];
        var filas = await db.Movimientos.AsNoTracking()
            .Where(m => m.Fecha >= desde && tipos.Contains(m.Tipo) && !m.Revertido)
            .Select(m => new { m.InsumoId, m.Cantidad }).ToListAsync();
        return filas.GroupBy(f => f.InsumoId).ToDictionary(g => g.Key, g => Math.Round(g.Sum(x => x.Cantidad) / dias, 6));
    }

    public Task<List<int>> EmpresasActivasAsync() =>
        db.Empresas.AsNoTracking().Where(e => e.Activo).Select(e => e.Id).ToListAsync();

    public Task GuardarAsync() => db.SaveChangesAsync();
}

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SPIn.Data;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository.RepositoryImpl;

public class VentaRepositoryImpl(ApplicationDbContext db) : IVentaRepository
{
    private static readonly Expression<Func<Venta, VentaResponse>> Proyeccion = v => new VentaResponse
    {
        Id = v.Id,
        Fecha = v.Fecha,
        ProductoId = v.ProductoId,
        ProductoCodigo = v.Producto!.Codigo,
        ProductoNombre = v.Producto.Nombre,
        UnidadesPorPaquete = v.Producto.UnidadesPorPaquete,
        Canal = v.Canal,
        Paquetes = v.Paquetes,
        Documento = v.Documento,
        Origen = v.Origen,
        AfectaStock = v.AfectaStock,
        AlmacenCodigo = v.Almacen != null ? v.Almacen.Codigo : null,
        Anulada = v.Anulada,
        MotivoAnulacion = v.MotivoAnulacion,
        CreadoPor = v.CreadoPor
    };

    private IQueryable<Venta> Filtrar(FiltroVentaRequest f)
    {
        var q = db.Ventas.AsNoTracking();
        if (f.ProductoId is int productoId) q = q.Where(v => v.ProductoId == productoId);
        if (f.Canal is CanalVenta canal) q = q.Where(v => v.Canal == canal);
        if (f.Origen is OrigenVenta origen) q = q.Where(v => v.Origen == origen);
        if (f.Desde is DateOnly desde) q = q.Where(v => v.Fecha >= desde);
        if (f.Hasta is DateOnly hasta) q = q.Where(v => v.Fecha <= hasta);
        if (f.Anuladas != true) q = q.Where(v => !v.Anulada);
        return q;
    }

    public async Task<VentasPaginaResponse> ListarAsync(FiltroVentaRequest filtro)
    {
        var consulta = Filtrar(filtro);
        var total = await consulta.CountAsync();
        var paginas = Math.Max(1, (int)Math.Ceiling(total / (double)filtro.TamanoPagina));
        var pagina = Math.Min(Math.Max(1, filtro.Pagina), paginas);

        var items = await consulta
            .OrderByDescending(v => v.Fecha).ThenByDescending(v => v.Id)
            .Skip((pagina - 1) * filtro.TamanoPagina).Take(filtro.TamanoPagina)
            .Select(Proyeccion)
            .ToListAsync();
        foreach (var v in items) v.CanalNombre = EtiquetasVentas.De(v.Canal);

        // Totales del filtro (sin anuladas) calculados en memoria: SQLite no suma decimales.
        var filas = await consulta.Where(v => !v.Anulada)
            .Select(v => new { v.Paquetes, v.Producto!.UnidadesPorPaquete, v.ProductoId, v.Fecha })
            .ToListAsync();

        return new VentasPaginaResponse
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = filtro.TamanoPagina,
            Resumen = new ResumenVentasResponse
            {
                Registros = filas.Count,
                Paquetes = filas.Sum(f => f.Paquetes),
                Botellas = filas.Sum(f => f.Paquetes * f.UnidadesPorPaquete),
                Productos = filas.Select(f => f.ProductoId).Distinct().Count(),
                Dias = filas.Select(f => f.Fecha).Distinct().Count()
            }
        };
    }

    public async Task<VentaResponse?> ResumenAsync(long id)
    {
        var v = await db.Ventas.AsNoTracking().Where(x => x.Id == id).Select(Proyeccion).FirstOrDefaultAsync();
        if (v is not null) v.CanalNombre = EtiquetasVentas.De(v.Canal);
        return v;
    }

    public Task<Venta?> ObtenerAsync(long id) => db.Ventas.FirstOrDefaultAsync(v => v.Id == id);

    public async Task<List<(DateOnly Fecha, decimal Paquetes)>> VentasDiariasAsync(int? productoId, DateOnly desde, DateOnly hasta)
    {
        var filas = await db.Ventas.AsNoTracking()
            .Where(v => !v.Anulada && v.Fecha >= desde && v.Fecha <= hasta && (productoId == null || v.ProductoId == productoId))
            .Select(v => new { v.Fecha, v.Paquetes })
            .ToListAsync();
        return filas.Select(f => (f.Fecha, f.Paquetes)).ToList();
    }

    public Task<DateOnly?> PrimeraVentaAsync(int? productoId) =>
        db.Ventas.AsNoTracking()
            .Where(v => !v.Anulada && (productoId == null || v.ProductoId == productoId))
            .OrderBy(v => v.Fecha)
            .Select(v => (DateOnly?)v.Fecha)
            .FirstOrDefaultAsync();

    public async Task<Dictionary<int, decimal>> VentaPromedioDiariaAsync(DateOnly desde, DateOnly hasta)
    {
        var dias = hasta.DayNumber - desde.DayNumber + 1;
        var filas = await db.Ventas.AsNoTracking()
            .Where(v => !v.Anulada && v.Fecha >= desde && v.Fecha <= hasta)
            .Select(v => new { v.ProductoId, v.Paquetes })
            .ToListAsync();
        return filas.GroupBy(f => f.ProductoId).ToDictionary(g => g.Key, g => Math.Round(g.Sum(x => x.Paquetes) / dias, 2));
    }

    public Task<List<Venta>> HistorialMismaClaveAsync(DateOnly fecha, int productoId, CanalVenta canal) =>
        db.Ventas.Where(v => v.Fecha == fecha && v.ProductoId == productoId && v.Canal == canal && !v.AfectaStock && !v.Anulada)
            .ToListAsync();

    public Task<int> ProductosConVentasAsync() =>
        db.Ventas.Where(v => !v.Anulada).Select(v => v.ProductoId).Distinct().CountAsync();

    public Task<bool> ProductoTieneVentasAsync(int productoId) => db.Ventas.AnyAsync(v => v.ProductoId == productoId);

    public void Agregar(Venta venta) => db.Ventas.Add(venta);
    public void Eliminar(Venta venta) => db.Ventas.Remove(venta);

    // ================================================================ producto terminado

    public async Task<List<StockProductoResponse>> StockAsync(FiltroStockProductoRequest filtro)
    {
        var productos = db.Productos.AsNoTracking().Where(p => p.Activo);
        var texto = ConsultaExtensions.TextoBusqueda(filtro.Buscar);
        if (texto is not null)
            productos = productos.Where(p => p.Codigo.ToLower().Contains(texto) || p.Nombre.ToLower().Contains(texto));

        var lista = await productos.OrderBy(p => p.Nombre)
            .Select(p => new StockProductoResponse
            {
                ProductoId = p.Id,
                Codigo = p.Codigo,
                Nombre = p.Nombre,
                FormatoNombre = p.Formato!.Nombre,
                UnidadesPorPaquete = p.UnidadesPorPaquete,
                PaquetesPorPallet = p.PaquetesPorPallet
            })
            .ToListAsync();

        var ids = lista.Select(p => p.ProductoId).ToList();
        var filas = db.StocksProducto.AsNoTracking().Where(s => ids.Contains(s.ProductoId));
        if (filtro.AlmacenId is int almacenId) filas = filas.Where(s => s.AlmacenId == almacenId);
        var stocks = await filas.Select(s => new
        {
            s.ProductoId,
            s.UltimoMovimiento,
            Fila = new StockAlmacenResponse { AlmacenId = s.AlmacenId, AlmacenCodigo = s.Almacen!.Codigo, AlmacenNombre = s.Almacen.Nombre, Cantidad = s.Paquetes }
        }).ToListAsync();

        var porProducto = stocks.GroupBy(s => s.ProductoId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var p in lista)
        {
            if (!porProducto.TryGetValue(p.ProductoId, out var suyos)) continue;
            p.Almacenes = suyos.Select(s => s.Fila).Where(f => f.Cantidad != 0).OrderBy(f => f.AlmacenCodigo).ToList();
            p.Paquetes = suyos.Sum(s => s.Fila.Cantidad);
            p.UltimoMovimiento = suyos.Max(s => s.UltimoMovimiento);
        }
        return lista;
    }

    public Task<StockProducto?> StockDeAsync(int productoId, int almacenId) =>
        db.StocksProducto.FirstOrDefaultAsync(s => s.ProductoId == productoId && s.AlmacenId == almacenId);

    public async Task<decimal> PaquetesAsync(int productoId, int almacenId) =>
        await db.StocksProducto.Where(s => s.ProductoId == productoId && s.AlmacenId == almacenId)
            .Select(s => (decimal?)s.Paquetes).FirstOrDefaultAsync() ?? 0;

    public Task<bool> HayMovimientosAsync(int productoId, int almacenId) =>
        db.MovimientosProducto.AnyAsync(m => m.ProductoId == productoId && m.AlmacenId == almacenId);

    public Task<bool> AlmacenTieneMovimientosProductoAsync(int almacenId) =>
        db.MovimientosProducto.AnyAsync(m => m.AlmacenId == almacenId);

    public async Task<bool> AlmacenTieneStockProductoAsync(int almacenId) =>
        (await db.StocksProducto.Where(s => s.AlmacenId == almacenId).Select(s => s.Paquetes).ToListAsync()).Any(p => p > 0);

    public Task<bool> ProductoTieneMovimientosAsync(int productoId) =>
        db.MovimientosProducto.AnyAsync(m => m.ProductoId == productoId);

    public async Task<PaginaResponse<MovimientoProductoResponse>> MovimientosAsync(int? productoId, int pagina, int tamanoPagina)
    {
        var q = db.MovimientosProducto.AsNoTracking().Where(m => productoId == null || m.ProductoId == productoId);
        var total = await q.CountAsync();
        var paginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanoPagina));
        var actual = Math.Min(Math.Max(1, pagina), paginas);
        var items = await q.OrderByDescending(m => m.Id)
            .Skip((actual - 1) * tamanoPagina).Take(tamanoPagina)
            .Select(m => new MovimientoProductoResponse
            {
                Id = m.Id,
                Fecha = m.Fecha,
                Tipo = m.Tipo,
                ProductoId = m.ProductoId,
                ProductoCodigo = m.Producto!.Codigo,
                ProductoNombre = m.Producto.Nombre,
                AlmacenCodigo = m.Almacen!.Codigo,
                Paquetes = m.Paquetes,
                SaldoAlmacen = m.SaldoAlmacen,
                Documento = m.Documento,
                Motivo = m.Motivo,
                Referencia = m.Referencia,
                Revertido = m.Revertido,
                Usuario = m.Usuario
            })
            .ToListAsync();
        foreach (var m in items)
        {
            m.TipoNombre = EtiquetasVentas.De(m.Tipo);
            m.Signo = EtiquetasVentas.Signo(m.Tipo);
        }
        return new PaginaResponse<MovimientoProductoResponse> { Items = items, Total = total, Pagina = actual, TamanoPagina = tamanoPagina };
    }

    public Task<MovimientoProducto?> MovimientoAsync(long id) => db.MovimientosProducto.FirstOrDefaultAsync(m => m.Id == id);

    public Task<MovimientoProducto?> MovimientoDeVentaAsync(long ventaId) =>
        db.MovimientosProducto.FirstOrDefaultAsync(m => m.VentaId == ventaId && m.RevierteId == null);

    public void AgregarStock(StockProducto stock) => db.StocksProducto.Add(stock);
    public void AgregarMovimiento(MovimientoProducto movimiento) => db.MovimientosProducto.Add(movimiento);

    public async Task<T> EnTransaccionAsync<T>(Func<Task<T>> accion)
    {
        // Si ya hay una transacción abierta (por ejemplo, una importación completa), se participa de ella.
        if (db.Database.CurrentTransaction is not null) return await accion();

        await using var transaccion = await db.Database.BeginTransactionAsync();
        var resultado = await accion();
        await transaccion.CommitAsync();
        return resultado;
    }

    public Task GuardarAsync() => db.SaveChangesAsync();
}

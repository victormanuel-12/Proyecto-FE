using Microsoft.EntityFrameworkCore;
using SPIn.Data;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository.RepositoryImpl;

/// <summary>
/// Acceso a stock, movimientos y conteos. Los totales se calculan en memoria porque SQLite
/// no suma columnas decimal; el volumen (insumos × almacenes) es pequeño.
/// </summary>
public class InventarioRepositoryImpl(ApplicationDbContext db) : IInventarioRepository
{
    // ================================================================ stock

    public Task<Stock?> StockAsync(int insumoId, int almacenId) =>
        db.Stocks.FirstOrDefaultAsync(s => s.InsumoId == insumoId && s.AlmacenId == almacenId);

    public async Task<decimal> CantidadAsync(int insumoId, int almacenId) =>
        await db.Stocks.Where(s => s.InsumoId == insumoId && s.AlmacenId == almacenId)
            .Select(s => (decimal?)s.Cantidad).FirstOrDefaultAsync() ?? 0;

    public Task<List<StockAlmacenResponse>> StockPorInsumoAsync(int insumoId) =>
        db.Stocks.AsNoTracking()
            .Where(s => s.InsumoId == insumoId)
            .OrderBy(s => s.Almacen!.Codigo)
            .Select(s => new StockAlmacenResponse
            {
                AlmacenId = s.AlmacenId,
                AlmacenCodigo = s.Almacen!.Codigo,
                AlmacenNombre = s.Almacen.Nombre,
                Cantidad = s.Cantidad
            })
            .ToListAsync();

    public async Task<List<StockInsumoResponse>> StockInsumosAsync(FiltroStockRequest filtro)
    {
        var insumos = db.Insumos.AsNoTracking().Where(i => i.Activo);
        var texto = ConsultaExtensions.TextoBusqueda(filtro.Buscar);
        if (texto is not null)
            insumos = insumos.Where(i => i.Codigo.ToLower().Contains(texto) || i.Nombre.ToLower().Contains(texto));
        if (filtro.CategoriaId is int categoriaId) insumos = insumos.Where(i => i.CategoriaId == categoriaId);
        if (filtro.Criticos == true) insumos = insumos.Where(i => i.EsCritico);

        var lista = await insumos
            .OrderByDescending(i => i.EsCritico).ThenBy(i => i.Codigo)
            .Select(i => new StockInsumoResponse
            {
                InsumoId = i.Id,
                Codigo = i.Codigo,
                Nombre = i.Nombre,
                CategoriaNombre = i.Categoria!.Nombre,
                UnidadConsumoCodigo = i.UnidadConsumo!.Codigo,
                UnidadCompraCodigo = i.UnidadCompra!.Codigo,
                FactorConversion = i.FactorConversion,
                EsCritico = i.EsCritico,
                StockMinimo = i.StockMinimo,
                StockMaximo = i.StockMaximo
            })
            .ToListAsync();

        var ids = lista.Select(i => i.InsumoId).ToList();
        var filas = db.Stocks.AsNoTracking().Where(s => ids.Contains(s.InsumoId));
        if (filtro.AlmacenId is int almacenId) filas = filas.Where(s => s.AlmacenId == almacenId);
        var stocks = await filas
            .Select(s => new
            {
                s.InsumoId,
                s.UltimoMovimiento,
                Fila = new StockAlmacenResponse
                {
                    AlmacenId = s.AlmacenId,
                    AlmacenCodigo = s.Almacen!.Codigo,
                    AlmacenNombre = s.Almacen.Nombre,
                    Cantidad = s.Cantidad
                }
            })
            .ToListAsync();

        var porInsumo = stocks.GroupBy(s => s.InsumoId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var item in lista)
        {
            if (porInsumo.TryGetValue(item.InsumoId, out var suyos))
            {
                item.Almacenes = suyos.Select(s => s.Fila).Where(f => f.Cantidad != 0).OrderBy(f => f.AlmacenCodigo).ToList();
                item.Total = suyos.Sum(s => s.Fila.Cantidad);
                item.UltimoMovimiento = suyos.Max(s => s.UltimoMovimiento);
            }
            item.Situacion = item.Total <= 0 ? "sin"
                : item.Total < item.StockMinimo ? "bajo"
                : item.Total > item.StockMaximo ? "sobre"
                : "normal";
        }
        return lista;
    }

    public async Task<Dictionary<int, decimal>> StockDeAlmacenAsync(int almacenId) =>
        (await db.Stocks.AsNoTracking().Where(s => s.AlmacenId == almacenId)
            .Select(s => new { s.InsumoId, s.Cantidad }).ToListAsync())
        .ToDictionary(s => s.InsumoId, s => s.Cantidad);

    public Task<bool> AlmacenTieneStockAsync(int almacenId) =>
        db.Stocks.AnyAsync(s => s.AlmacenId == almacenId && s.Cantidad > 0);

    public Task<int> InsumosConStockAsync() =>
        db.Stocks.Where(s => s.Cantidad > 0 && s.Insumo!.Activo).Select(s => s.InsumoId).Distinct().CountAsync();

    public void AgregarStock(Stock stock) => db.Stocks.Add(stock);

    // ================================================================ movimientos

    private static readonly System.Linq.Expressions.Expression<Func<MovimientoStock, MovimientoResponse>> ProyeccionMovimiento =
        m => new MovimientoResponse
            {
                Id = m.Id,
                Fecha = m.Fecha,
                Tipo = m.Tipo,
                InsumoId = m.InsumoId,
                InsumoCodigo = m.Insumo!.Codigo,
                InsumoNombre = m.Insumo.Nombre,
                UnidadConsumoCodigo = m.Insumo.UnidadConsumo!.Codigo,
                AlmacenId = m.AlmacenId,
                AlmacenCodigo = m.Almacen!.Codigo,
                Cantidad = m.Cantidad,
                SaldoAlmacen = m.SaldoAlmacen,
                Documento = m.Documento,
                Motivo = m.Motivo,
                Referencia = m.Referencia,
                Revertido = m.Revertido,
                RevierteId = m.RevierteId,
                Usuario = m.Usuario,
                Reversible = !m.Revertido && m.RevierteId == null && m.ConteoId == null && m.ParteProduccionId == null && m.RecepcionId == null
            };

    private static MovimientoResponse Completar(MovimientoResponse m)
    {
        m.TipoNombre = EtiquetasInventario.De(m.Tipo);
        m.Signo = EtiquetasInventario.Signo(m.Tipo);
        return m;
    }

    public async Task<MovimientoResponse?> MovimientoResumenAsync(long id)
    {
        var m = await db.Movimientos.AsNoTracking().Where(x => x.Id == id).Select(ProyeccionMovimiento).FirstOrDefaultAsync();
        return m is null ? null : Completar(m);
    }

    public async Task<PaginaResponse<MovimientoResponse>> ListarMovimientosAsync(FiltroMovimientoRequest filtro)
    {
        var consulta = db.Movimientos.AsNoTracking();
        var texto = ConsultaExtensions.TextoBusqueda(filtro.Buscar);
        if (texto is not null)
        {
            consulta = consulta.Where(m =>
                m.Insumo!.Codigo.ToLower().Contains(texto) || m.Insumo.Nombre.ToLower().Contains(texto) ||
                (m.Documento != null && m.Documento.ToLower().Contains(texto)));
        }
        if (filtro.InsumoId is int insumoId) consulta = consulta.Where(m => m.InsumoId == insumoId);
        if (filtro.AlmacenId is int almacenId) consulta = consulta.Where(m => m.AlmacenId == almacenId);
        if (filtro.Tipo is TipoMovimiento tipo) consulta = consulta.Where(m => m.Tipo == tipo);
        if (filtro.Desde is DateOnly desde)
        {
            var d = desde.ToDateTime(TimeOnly.MinValue);
            consulta = consulta.Where(m => m.Fecha >= d);
        }
        if (filtro.Hasta is DateOnly hasta)
        {
            var h = hasta.AddDays(1).ToDateTime(TimeOnly.MinValue);
            consulta = consulta.Where(m => m.Fecha < h);
        }

        var total = await consulta.CountAsync();
        var paginas = Math.Max(1, (int)Math.Ceiling(total / (double)filtro.TamanoPagina));
        var pagina = Math.Min(Math.Max(1, filtro.Pagina), paginas);

        var items = await consulta
            .OrderByDescending(m => m.Id)
            .Skip((pagina - 1) * filtro.TamanoPagina).Take(filtro.TamanoPagina)
            .Select(ProyeccionMovimiento)
            .ToListAsync();

        foreach (var m in items) Completar(m);
        return new PaginaResponse<MovimientoResponse> { Items = items, Total = total, Pagina = pagina, TamanoPagina = filtro.TamanoPagina };
    }

    public Task<MovimientoStock?> MovimientoAsync(long id) => db.Movimientos.FirstOrDefaultAsync(m => m.Id == id);

    public Task<bool> HayMovimientosAsync(int insumoId, int almacenId) =>
        db.Movimientos.AnyAsync(m => m.InsumoId == insumoId && m.AlmacenId == almacenId);

    public Task<bool> InsumoTieneMovimientosAsync(int insumoId) => db.Movimientos.AnyAsync(m => m.InsumoId == insumoId);

    public Task<bool> AlmacenTieneMovimientosAsync(int almacenId) => db.Movimientos.AnyAsync(m => m.AlmacenId == almacenId);

    public async Task<decimal> SaldoAntesDeAsync(int insumoId, int? almacenId, DateTime limite)
    {
        // El saldo de cada almacén es el del último movimiento anterior a la fecha límite.
        var almacenes = await db.Movimientos.AsNoTracking()
            .Where(m => m.InsumoId == insumoId && m.Fecha < limite && (almacenId == null || m.AlmacenId == almacenId))
            .Select(m => m.AlmacenId).Distinct().ToListAsync();

        decimal saldo = 0;
        foreach (var a in almacenes)
        {
            saldo += await db.Movimientos.AsNoTracking()
                .Where(m => m.InsumoId == insumoId && m.AlmacenId == a && m.Fecha < limite)
                .OrderByDescending(m => m.Id)
                .Select(m => m.SaldoAlmacen)
                .FirstAsync();
        }
        return saldo;
    }

    public Task<List<MovimientoStock>> MovimientosEntreAsync(int insumoId, int? almacenId, DateTime desde, DateTime hasta) =>
        db.Movimientos.AsNoTracking()
            .Include(m => m.Almacen)
            .Where(m => m.InsumoId == insumoId && m.Fecha >= desde && m.Fecha < hasta &&
                        (almacenId == null || m.AlmacenId == almacenId))
            .OrderBy(m => m.Id)
            .ToListAsync();

    public void AgregarMovimiento(MovimientoStock movimiento) => db.Movimientos.Add(movimiento);

    // ================================================================ conteos

    private static IQueryable<ConteoResponse> ProyectarConteo(IQueryable<ConteoFisico> consulta) =>
        consulta.Select(c => new ConteoResponse
        {
            Id = c.Id,
            Numero = c.Numero,
            AlmacenId = c.AlmacenId,
            AlmacenCodigo = c.Almacen!.Codigo,
            AlmacenNombre = c.Almacen.Nombre,
            CategoriaNombre = c.Categoria != null ? c.Categoria.Nombre : null,
            Estado = c.Estado,
            Observacion = c.Observacion,
            CreadoEn = c.CreadoEn,
            CreadoPor = c.CreadoPor,
            CerradoEn = c.CerradoEn,
            CerradoPor = c.CerradoPor,
            Insumos = c.Detalles.Count,
            Contados = c.Detalles.Count(d => d.CantidadContada != null),
            ConDiferencia = c.Detalles.Count(d => d.CantidadContada != null && d.CantidadContada != d.StockSistema)
        });

    public async Task<PaginaResponse<ConteoResponse>> ListarConteosAsync(FiltroConteoRequest filtro)
    {
        var consulta = db.Conteos.AsNoTracking();
        if (filtro.AlmacenId is int almacenId) consulta = consulta.Where(c => c.AlmacenId == almacenId);
        if (filtro.Estado is EstadoConteo estado) consulta = consulta.Where(c => c.Estado == estado);

        var total = await consulta.CountAsync();
        var paginas = Math.Max(1, (int)Math.Ceiling(total / (double)filtro.TamanoPagina));
        var pagina = Math.Min(Math.Max(1, filtro.Pagina), paginas);
        var items = await ProyectarConteo(consulta.OrderByDescending(c => c.Numero))
            .Skip((pagina - 1) * filtro.TamanoPagina).Take(filtro.TamanoPagina)
            .ToListAsync();
        foreach (var c in items) c.EstadoNombre = EtiquetasInventario.De(c.Estado);
        return new PaginaResponse<ConteoResponse> { Items = items, Total = total, Pagina = pagina, TamanoPagina = filtro.TamanoPagina };
    }

    public async Task<ConteoDetalleCompletoResponse?> ConteoCompletoAsync(int id)
    {
        var cabecera = await ProyectarConteo(db.Conteos.AsNoTracking().Where(c => c.Id == id)).FirstOrDefaultAsync();
        if (cabecera is null) return null;

        var detalles = await db.ConteoDetalles.AsNoTracking()
            .Where(d => d.ConteoId == id)
            .OrderBy(d => d.Insumo!.Categoria!.Nombre).ThenBy(d => d.Insumo!.Codigo)
            .Select(d => new ConteoDetalleResponse
            {
                InsumoId = d.InsumoId,
                InsumoCodigo = d.Insumo!.Codigo,
                InsumoNombre = d.Insumo.Nombre,
                CategoriaNombre = d.Insumo.Categoria!.Nombre,
                UnidadConsumoCodigo = d.Insumo.UnidadConsumo!.Codigo,
                StockSistema = d.StockSistema,
                CantidadContada = d.CantidadContada,
                StockAlAprobar = d.StockAlAprobar,
                AjusteAplicado = d.AjusteAplicado
            })
            .ToListAsync();

        if (cabecera.Estado == EstadoConteo.Abierto)
        {
            var actual = await StockDeAlmacenAsync(cabecera.AlmacenId);
            foreach (var d in detalles) d.StockActual = actual.GetValueOrDefault(d.InsumoId);
        }

        return new ConteoDetalleCompletoResponse
        {
            Id = cabecera.Id,
            Numero = cabecera.Numero,
            AlmacenId = cabecera.AlmacenId,
            AlmacenCodigo = cabecera.AlmacenCodigo,
            AlmacenNombre = cabecera.AlmacenNombre,
            CategoriaNombre = cabecera.CategoriaNombre,
            Estado = cabecera.Estado,
            EstadoNombre = EtiquetasInventario.De(cabecera.Estado),
            Observacion = cabecera.Observacion,
            CreadoEn = cabecera.CreadoEn,
            CreadoPor = cabecera.CreadoPor,
            CerradoEn = cabecera.CerradoEn,
            CerradoPor = cabecera.CerradoPor,
            Insumos = cabecera.Insumos,
            Contados = cabecera.Contados,
            ConDiferencia = cabecera.ConDiferencia,
            Detalles = detalles
        };
    }

    public Task<ConteoFisico?> ConteoConDetallesAsync(int id) =>
        db.Conteos.Include(c => c.Detalles).Include(c => c.Almacen).FirstOrDefaultAsync(c => c.Id == id);

    public Task<bool> HayConteoAbiertoAsync(int almacenId) =>
        db.Conteos.AnyAsync(c => c.AlmacenId == almacenId && c.Estado == EstadoConteo.Abierto);

    public async Task<int> SiguienteNumeroConteoAsync() =>
        (await db.Conteos.MaxAsync(c => (int?)c.Numero) ?? 0) + 1;

    public Task<List<Insumo>> InsumosParaConteoAsync(int? categoriaId) =>
        db.Insumos.AsNoTracking()
            .Where(i => i.Activo && (categoriaId == null || i.CategoriaId == categoriaId))
            .OrderBy(i => i.Codigo)
            .ToListAsync();

    public void AgregarConteo(ConteoFisico conteo) => db.Conteos.Add(conteo);

    // ================================================================ general

    public async Task<T> EnTransaccionAsync<T>(Func<Task<T>> accion)
    {
        // En SQLite la transacción se abre con bloqueo de escritura (BEGIN IMMEDIATE):
        // dos registros simultáneos sobre el mismo stock no pueden dejarlo negativo.
        // Si ya hay una transacción abierta (por ejemplo, una importación completa), se participa de ella.
        if (db.Database.CurrentTransaction is not null) return await accion();

        await using var transaccion = await db.Database.BeginTransactionAsync();
        var resultado = await accion();
        await transaccion.CommitAsync();
        return resultado;
    }

    public Task GuardarAsync() => db.SaveChangesAsync();
}

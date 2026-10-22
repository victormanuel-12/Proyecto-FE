using Microsoft.EntityFrameworkCore;
using SPIn.Data;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository.RepositoryImpl;

public class ProduccionOperacionRepositoryImpl(ApplicationDbContext db) : IProduccionOperacionRepository
{
    public async Task<OpcionesProduccionResponse> OpcionesAsync(DateOnly hoy)
    {
        var lineas = await db.Lineas.AsNoTracking().Where(l => l.Activo && l.Planta!.Activo)
            .OrderBy(l => l.Codigo)
            .Select(l => new LineaOpcionResponse
            {
                Id = l.Id, Codigo = l.Codigo, Nombre = l.Nombre, PlantaId = l.PlantaId, PlantaNombre = l.Planta!.Nombre,
                VelocidadNominal = l.VelocidadNominal, FormatoIds = l.Formatos.Select(f => f.FormatoId).ToList()
            }).ToListAsync();
        var almacenes = await db.Almacenes.AsNoTracking().Where(a => a.Activo)
            .OrderBy(a => a.Codigo)
            .Select(a => new AlmacenOpcionResponse { Id = a.Id, Codigo = a.Codigo, Nombre = a.Nombre, PlantaId = a.PlantaId, Tipo = a.Tipo })
            .ToListAsync();
        var productos = await db.Productos.AsNoTracking().Where(p => p.Activo)
            .OrderBy(p => p.Codigo)
            .Select(p => new ProductoProduccionOpcion
            {
                Id = p.Id, Codigo = p.Codigo, Nombre = p.Nombre, FormatoId = p.FormatoId, FormatoNombre = p.Formato!.Nombre,
                UnidadesPorPaquete = p.UnidadesPorPaquete, PaquetesPorPallet = p.PaquetesPorPallet,
                TieneReceta = p.Recetas.Any(r => r.VigenteHasta == null || r.VigenteHasta >= hoy)
            }).ToListAsync();
        return new OpcionesProduccionResponse { Lineas = lineas, Almacenes = almacenes, Productos = productos };
    }

    public Task<LineaProduccion?> LineaConFormatosAsync(int id) =>
        db.Lineas.Include(l => l.Formatos).FirstOrDefaultAsync(l => l.Id == id);

    // ---------------------------------------------------------------- plan
    public async Task<PaginaResponse<PlanResponse>> PlanesAsync(FiltroPlanRequest f, DateOnly hoy)
    {
        var q = db.PlanesProduccion.AsNoTracking().AsQueryable();
        if (f.Desde is DateOnly d) q = q.Where(p => p.Fecha >= d);
        if (f.Hasta is DateOnly h) q = q.Where(p => p.Fecha <= h);
        if (f.LineaId is int l) q = q.Where(p => p.LineaId == l);
        if (f.ProductoId is int pr) q = q.Where(p => p.ProductoId == pr);
        if (f.Estado is EstadoPlan e) q = q.Where(p => p.Estado == e);

        var total = await q.CountAsync();
        var paginas = Math.Max(1, (int)Math.Ceiling(total / (double)f.TamanoPagina));
        var pagina = Math.Min(Math.Max(1, f.Pagina), paginas);
        var items = await Proyectar(q.OrderBy(p => p.Fecha).ThenBy(p => p.LineaId).ThenBy(p => p.Id)
            .Skip((pagina - 1) * f.TamanoPagina).Take(f.TamanoPagina)).ToListAsync();
        Completar(items, hoy);
        return new PaginaResponse<PlanResponse> { Items = items, Total = total, Pagina = pagina, TamanoPagina = f.TamanoPagina };
    }

    public async Task<PlanResponse?> PlanResumenAsync(int id, DateOnly hoy)
    {
        var item = await Proyectar(db.PlanesProduccion.AsNoTracking().Where(p => p.Id == id)).FirstOrDefaultAsync();
        if (item is not null) Completar([item], hoy);
        return item;
    }

    private IQueryable<PlanResponse> Proyectar(IQueryable<PlanProduccion> q) => q.Select(p => new PlanResponse
    {
        Id = p.Id, Fecha = p.Fecha, LineaId = p.LineaId, LineaCodigo = p.Linea!.Codigo,
        ProductoId = p.ProductoId, ProductoCodigo = p.Producto!.Codigo, ProductoNombre = p.Producto.Nombre,
        UnidadesPorPaquete = p.Producto.UnidadesPorPaquete, Paquetes = p.Paquetes, Estado = p.Estado,
        Comentario = p.Comentario, ParteId = p.ParteId, VelocidadNominal = p.Linea.VelocidadNominal
    });

    private void Completar(List<PlanResponse> items, DateOnly hoy)
    {
        var partes = items.Where(i => i.ParteId != null).Select(i => i.ParteId!.Value).ToList();
        var numeros = partes.Count == 0 ? new Dictionary<int, int>()
            : db.PartesProduccion.AsNoTracking().Where(p => partes.Contains(p.Id)).ToDictionary(p => p.Id, p => p.Numero);
        foreach (var i in items)
        {
            i.EstadoNombre = EtiquetasOperacion.De(i.Estado);
            i.Vencido = i.Estado == EstadoPlan.Planificado && i.Fecha < hoy;
            i.HorasEstimadas = i.VelocidadNominal > 0 ? Math.Round(i.Botellas / i.VelocidadNominal, 1) : null;
            if (i.ParteId is int pid && numeros.TryGetValue(pid, out var n)) i.ParteNumero = n;
        }
    }

    public Task<PlanProduccion?> PlanAsync(int id) => db.PlanesProduccion.FirstOrDefaultAsync(p => p.Id == id);

    public Task<PlanProduccion?> PlanPendienteAsync(DateOnly fecha, int lineaId, int productoId) =>
        db.PlanesProduccion.Where(p => p.Fecha == fecha && p.LineaId == lineaId && p.ProductoId == productoId && p.Estado == EstadoPlan.Planificado)
            .OrderBy(p => p.Id).FirstOrDefaultAsync();

    public void AgregarPlan(PlanProduccion plan) => db.PlanesProduccion.Add(plan);

    // ---------------------------------------------------------------- partes
    public async Task<PaginaResponse<ParteResponse>> PartesAsync(FiltroParteRequest f)
    {
        var q = db.PartesProduccion.AsNoTracking().AsQueryable();
        if (f.Desde is DateOnly d) q = q.Where(p => p.Fecha >= d);
        if (f.Hasta is DateOnly h) q = q.Where(p => p.Fecha <= h);
        if (f.LineaId is int l) q = q.Where(p => p.LineaId == l);
        if (f.ProductoId is int pr) q = q.Where(p => p.ProductoId == pr);
        if (f.Anulados != true) q = q.Where(p => p.Estado == EstadoParte.Registrado);

        var total = await q.CountAsync();
        var paginas = Math.Max(1, (int)Math.Ceiling(total / (double)f.TamanoPagina));
        var pagina = Math.Min(Math.Max(1, f.Pagina), paginas);
        var items = await ProyectarParte(q.OrderByDescending(p => p.Fecha).ThenByDescending(p => p.Numero)
            .Skip((pagina - 1) * f.TamanoPagina).Take(f.TamanoPagina)).ToListAsync();
        foreach (var i in items) Nombres(i);
        return new PaginaResponse<ParteResponse> { Items = items, Total = total, Pagina = pagina, TamanoPagina = f.TamanoPagina };
    }

    public async Task<ParteResponse?> ParteResumenAsync(int id)
    {
        var item = await ProyectarParte(db.PartesProduccion.AsNoTracking().Where(p => p.Id == id)).FirstOrDefaultAsync();
        if (item is null) return null;
        Nombres(item);
        item.Consumos = await db.ParteConsumos.AsNoTracking().Where(c => c.ParteId == id)
            .OrderBy(c => c.Insumo!.Codigo)
            .Select(c => new ParteConsumoResponse
            {
                InsumoId = c.InsumoId, InsumoCodigo = c.Insumo!.Codigo, InsumoNombre = c.Insumo.Nombre,
                UnidadConsumoCodigo = c.Insumo.UnidadConsumo!.Codigo, Cantidad = c.Cantidad
            }).ToListAsync();
        return item;
    }

    private static IQueryable<ParteResponse> ProyectarParte(IQueryable<ParteProduccion> q) => q.Select(p => new ParteResponse
    {
        Id = p.Id, Numero = p.Numero, Fecha = p.Fecha, Turno = p.Turno,
        LineaId = p.LineaId, LineaCodigo = p.Linea!.Codigo, VelocidadNominal = p.Linea.VelocidadNominal,
        ProductoId = p.ProductoId, ProductoCodigo = p.Producto!.Codigo, ProductoNombre = p.Producto.Nombre,
        UnidadesPorPaquete = p.Producto.UnidadesPorPaquete, Paquetes = p.Paquetes, HorasOperacion = p.HorasOperacion,
        AlmacenInsumosCodigo = p.AlmacenInsumos!.Codigo, AlmacenProductoCodigo = p.AlmacenProducto!.Codigo,
        RecetaVersion = p.RecetaVersion, PlanId = p.PlanId, Estado = p.Estado, Observacion = p.Observacion,
        MotivoAnulacion = p.MotivoAnulacion, CreadoEn = p.CreadoEn, CreadoPor = p.CreadoPor
    });

    private static void Nombres(ParteResponse p)
    {
        p.TurnoNombre = EtiquetasOperacion.De(p.Turno);
        p.EstadoNombre = EtiquetasOperacion.De(p.Estado);
    }

    public Task<ParteProduccion?> ParteConConsumosAsync(int id) =>
        db.PartesProduccion.Include(p => p.Consumos).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<int> SiguienteNumeroParteAsync() =>
        (await db.PartesProduccion.Select(p => (int?)p.Numero).MaxAsync() ?? 0) + 1;

    public void AgregarParte(ParteProduccion parte) => db.PartesProduccion.Add(parte);

    public async Task<Dictionary<int, decimal>> StockInsumosAsync(IEnumerable<int> insumoIds, int? almacenId)
    {
        var ids = insumoIds.Distinct().ToList();
        var q = db.Stocks.AsNoTracking().Where(s => ids.Contains(s.InsumoId) && s.Almacen!.Tipo == TipoAlmacen.Insumos && s.Almacen.Activo);
        if (almacenId is int a) q = q.Where(s => s.AlmacenId == a);
        var filas = await q.Select(s => new { s.InsumoId, s.Cantidad }).ToListAsync();
        return ids.ToDictionary(i => i, i => filas.Where(f => f.InsumoId == i).Sum(f => f.Cantidad));
    }

    public async Task<bool> ProductoUsadoAsync(int productoId) =>
        await db.PartesProduccion.AnyAsync(p => p.ProductoId == productoId) || await db.PlanesProduccion.AnyAsync(p => p.ProductoId == productoId);

    public async Task<bool> LineaUsadaAsync(int lineaId) =>
        await db.PartesProduccion.AnyAsync(p => p.LineaId == lineaId) || await db.PlanesProduccion.AnyAsync(p => p.LineaId == lineaId);

    public Task GuardarAsync() => db.SaveChangesAsync();
}

using Microsoft.EntityFrameworkCore;
using SPIn.Data;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository.RepositoryImpl;

public class RecetaRepositoryImpl(ApplicationDbContext db) : IRecetaRepository
{
    private static IQueryable<RecetaResponse> ProyectarCompleta(IQueryable<Receta> consulta, DateOnly hoy) =>
        consulta.Select(r => new RecetaResponse
        {
            Id = r.Id,
            Version = r.Version,
            VigenteDesde = r.VigenteDesde,
            VigenteHasta = r.VigenteHasta,
            Comentario = r.Comentario,
            Insumos = r.Detalles.Count,
            CreadoPor = r.CreadoPor,
            EsVigente = r.VigenteDesde <= hoy && (r.VigenteHasta == null || r.VigenteHasta >= hoy),
            EsFutura = r.VigenteDesde > hoy,
            Detalles = r.Detalles
                .OrderBy(d => d.Nivel).ThenBy(d => d.Insumo!.Codigo)
                .Select(d => new RecetaDetalleResponse
                {
                    InsumoId = d.InsumoId,
                    InsumoCodigo = d.Insumo!.Codigo,
                    InsumoNombre = d.Insumo.Nombre,
                    UnidadConsumoCodigo = d.Insumo.UnidadConsumo!.Codigo,
                    InsumoActivo = d.Insumo.Activo,
                    Cantidad = d.Cantidad,
                    Nivel = d.Nivel,
                    MermaPorcentaje = d.MermaPorcentaje
                }).ToList()
        });

    private static RecetaResponse? Completar(RecetaResponse? receta)
    {
        if (receta is not null)
            foreach (var d in receta.Detalles) d.NivelNombre = EtiquetasProduccion.De(d.Nivel);
        return receta;
    }

    public async Task<RecetaResponse?> VigenteAsync(int productoId, DateOnly fecha, DateOnly hoy) =>
        Completar(await ProyectarCompleta(
                db.Recetas.AsNoTracking().Where(r =>
                    r.ProductoId == productoId && r.VigenteDesde <= fecha && (r.VigenteHasta == null || r.VigenteHasta >= fecha)), hoy)
            .FirstOrDefaultAsync());

    public Task<List<RecetaVersionResponse>> HistorialAsync(int productoId, DateOnly hoy) =>
        db.Recetas.AsNoTracking()
            .Where(r => r.ProductoId == productoId)
            .OrderByDescending(r => r.Version)
            .Select(r => new RecetaVersionResponse
            {
                Id = r.Id,
                Version = r.Version,
                VigenteDesde = r.VigenteDesde,
                VigenteHasta = r.VigenteHasta,
                Comentario = r.Comentario,
                Insumos = r.Detalles.Count,
                CreadoPor = r.CreadoPor,
                EsVigente = r.VigenteDesde <= hoy && (r.VigenteHasta == null || r.VigenteHasta >= hoy),
                EsFutura = r.VigenteDesde > hoy
            })
            .ToListAsync();

    public async Task<RecetaResponse?> ObtenerAsync(int recetaId, DateOnly hoy) =>
        Completar(await ProyectarCompleta(db.Recetas.AsNoTracking().Where(r => r.Id == recetaId), hoy).FirstOrDefaultAsync());

    public Task<Receta?> UltimaConDetallesAsync(int productoId) =>
        db.Recetas.Include(r => r.Detalles)
            .Where(r => r.ProductoId == productoId)
            .OrderByDescending(r => r.Version)
            .FirstOrDefaultAsync();

    public Task<List<InsumoRecetaOpcion>> InsumosDisponiblesAsync() =>
        db.Insumos.AsNoTracking()
            .Where(i => i.Activo && i.TipoConsumo == TipoConsumo.PorProduccion)
            .OrderBy(i => i.Categoria!.Nombre).ThenBy(i => i.Codigo)
            .Select(i => new InsumoRecetaOpcion
            {
                Id = i.Id,
                Codigo = i.Codigo,
                Nombre = i.Nombre,
                CategoriaNombre = i.Categoria!.Nombre,
                UnidadConsumoCodigo = i.UnidadConsumo!.Codigo
            })
            .ToListAsync();

    public Task<List<Insumo>> InsumosPorIdsAsync(IEnumerable<int> ids)
    {
        var lista = ids.Distinct().ToList();
        return db.Insumos.AsNoTracking().Where(i => lista.Contains(i.Id)).ToListAsync();
    }

    public Task<List<Insumo>> InsumosConUnidadesAsync(IEnumerable<int> ids)
    {
        var lista = ids.Distinct().ToList();
        return db.Insumos.AsNoTracking()
            .Include(i => i.UnidadCompra).Include(i => i.UnidadConsumo)
            .Where(i => lista.Contains(i.Id))
            .ToListAsync();
    }

    public Task<int> ProductosQueUsanInsumoAsync(int insumoId, DateOnly hoy) =>
        db.Recetas.AsNoTracking()
            .Where(r => r.VigenteDesde <= hoy && (r.VigenteHasta == null || r.VigenteHasta >= hoy) &&
                        r.Producto!.Activo && r.Detalles.Any(d => d.InsumoId == insumoId))
            .Select(r => r.ProductoId).Distinct().CountAsync();

    public Task<bool> InsumoEnAlgunaRecetaAsync(int insumoId) => db.RecetaDetalles.AnyAsync(d => d.InsumoId == insumoId);

    public void Agregar(Receta receta) => db.Recetas.Add(receta);
    public void QuitarDetalle(RecetaDetalle detalle) => db.RecetaDetalles.Remove(detalle);
    public Task GuardarAsync() => db.SaveChangesAsync();
}

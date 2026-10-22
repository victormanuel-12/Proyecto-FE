using Microsoft.EntityFrameworkCore;
using SPIn.Data;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository.RepositoryImpl;

public class ProductoRepositoryImpl(ApplicationDbContext db) : IProductoRepository
{
    private static IQueryable<ProductoResponse> Proyectar(IQueryable<Producto> consulta, DateOnly hoy) =>
        consulta.Select(p => new ProductoResponse
        {
            Id = p.Id,
            Codigo = p.Codigo,
            Nombre = p.Nombre,
            Sabor = p.Sabor,
            FormatoId = p.FormatoId,
            FormatoNombre = p.Formato!.Nombre,
            TipoEnvase = p.Formato.TipoEnvase,
            VolumenMl = p.Formato.VolumenMl,
            UnidadesPorPaquete = p.UnidadesPorPaquete,
            PaquetesPorPallet = p.PaquetesPorPallet,
            Activo = p.Activo,
            Versiones = p.Recetas.Count,
            RecetaVersion = p.Recetas
                .Where(r => r.VigenteDesde <= hoy && (r.VigenteHasta == null || r.VigenteHasta >= hoy))
                .Select(r => (int?)r.Version).FirstOrDefault(),
            RecetaInsumos = p.Recetas
                .Where(r => r.VigenteDesde <= hoy && (r.VigenteHasta == null || r.VigenteHasta >= hoy))
                .Select(r => r.Detalles.Count).FirstOrDefault()
        });

    public async Task<ListadoResponse<ProductoResponse>> ListarAsync(FiltroProductoRequest filtro, DateOnly hoy)
    {
        var consulta = db.Productos.AsNoTracking();

        var texto = ConsultaExtensions.TextoBusqueda(filtro.Buscar);
        if (texto is not null)
        {
            consulta = consulta.Where(p =>
                p.Codigo.ToLower().Contains(texto) || p.Nombre.ToLower().Contains(texto) ||
                (p.Sabor != null && p.Sabor.ToLower().Contains(texto)));
        }
        if (filtro.FormatoId is int formatoId) consulta = consulta.Where(p => p.FormatoId == formatoId);
        if (filtro.TipoEnvase is TipoEnvase envase) consulta = consulta.Where(p => p.Formato!.TipoEnvase == envase);
        if (filtro.SinReceta == true)
        {
            consulta = consulta.Where(p => !p.Recetas.Any(r => r.VigenteDesde <= hoy && (r.VigenteHasta == null || r.VigenteHasta >= hoy)));
        }

        var activos = await consulta.CountAsync(p => p.Activo);
        var inactivos = await consulta.CountAsync(p => !p.Activo);
        if (filtro.Estado == "activos") consulta = consulta.Where(p => p.Activo);
        else if (filtro.Estado == "inactivos") consulta = consulta.Where(p => !p.Activo);

        var resultado = await Proyectar(consulta.OrderBy(p => p.Nombre), hoy)
            .PaginarAsync(filtro.Pagina, filtro.TamanoPagina, activos, inactivos);
        foreach (var item in resultado.Items) item.TipoEnvaseNombre = Etiquetas.De(item.TipoEnvase);
        return resultado;
    }

    public Task<List<ProductoOpcionResponse>> OpcionesAsync(bool soloActivos) =>
        db.Productos.AsNoTracking()
            .Where(p => !soloActivos || p.Activo)
            .OrderBy(p => p.Codigo)
            .Select(p => new ProductoOpcionResponse
            {
                Id = p.Id, Nombre = p.Codigo + " · " + p.Nombre, UnidadesPorPaquete = p.UnidadesPorPaquete, Activo = p.Activo
            })
            .ToListAsync();

    public Task<List<OpcionResponse>> OpcionesConRecetaAsync(DateOnly hoy, int? excluirId) =>
        db.Productos.AsNoTracking()
            .Where(p => p.Id != excluirId &&
                        p.Recetas.Any(r => r.VigenteDesde <= hoy && (r.VigenteHasta == null || r.VigenteHasta >= hoy)))
            .OrderBy(p => p.Nombre)
            .Select(p => new OpcionResponse { Id = p.Id, Nombre = p.Codigo + " · " + p.Nombre })
            .ToListAsync();

    public async Task<ProductoResponse?> ObtenerResumenAsync(int id, DateOnly hoy)
    {
        var item = await Proyectar(db.Productos.AsNoTracking().Where(p => p.Id == id), hoy).FirstOrDefaultAsync();
        if (item is not null) item.TipoEnvaseNombre = Etiquetas.De(item.TipoEnvase);
        return item;
    }

    public Task<Producto?> ObtenerAsync(int id) => db.Productos.FirstOrDefaultAsync(p => p.Id == id);

    public Task<bool> ExisteCodigoAsync(string codigo, int? excluirId) =>
        db.Productos.AnyAsync(p => p.Codigo == codigo && (excluirId == null || p.Id != excluirId));

    public Task<bool> TieneRecetasAsync(int id) => db.Recetas.AnyAsync(r => r.ProductoId == id);

    public Task<int> ContarConRecetaAsync(DateOnly hoy) =>
        db.Productos.CountAsync(p => p.Activo &&
            p.Recetas.Any(r => r.VigenteDesde <= hoy && (r.VigenteHasta == null || r.VigenteHasta >= hoy)));

    public Task<List<string>> LineasCompatiblesAsync(int formatoId) =>
        db.Lineas.AsNoTracking()
            .Where(l => l.Activo && l.Formatos.Any(f => f.FormatoId == formatoId))
            .OrderBy(l => l.Codigo)
            .Select(l => l.Codigo + " · " + l.Planta!.Nombre)
            .ToListAsync();

    public Task<bool> FormatoExisteAsync(int formatoId) => db.Formatos.AnyAsync(f => f.Id == formatoId);

    public void Agregar(Producto producto) => db.Productos.Add(producto);
    public void Eliminar(Producto producto) => db.Productos.Remove(producto);
    public Task GuardarAsync() => db.SaveChangesAsync();
}

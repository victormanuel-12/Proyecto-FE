using Microsoft.EntityFrameworkCore;
using SPIn.Data;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository.RepositoryImpl;

public class LineaRepositoryImpl(ApplicationDbContext db) : ILineaRepository
{
    private static IQueryable<LineaResponse> Proyectar(IQueryable<LineaProduccion> consulta) =>
        consulta.Select(l => new LineaResponse
        {
            Id = l.Id,
            Codigo = l.Codigo,
            Nombre = l.Nombre,
            PlantaId = l.PlantaId,
            PlantaNombre = l.Planta!.Nombre,
            TipoEnvase = l.TipoEnvase,
            VelocidadNominal = l.VelocidadNominal,
            Activo = l.Activo,
            Formatos = l.Formatos
                .OrderBy(f => f.Formato!.VolumenMl)
                .Select(f => new FormatoResponse
                {
                    Id = f.FormatoId,
                    Nombre = f.Formato!.Nombre,
                    TipoEnvase = f.Formato.TipoEnvase,
                    VolumenMl = f.Formato.VolumenMl
                }).ToList()
        });

    public async Task<ListadoResponse<LineaResponse>> ListarAsync(FiltroLineaRequest filtro)
    {
        var consulta = db.Lineas.AsNoTracking();

        var texto = ConsultaExtensions.TextoBusqueda(filtro.Buscar);
        if (texto is not null)
            consulta = consulta.Where(l => l.Codigo.ToLower().Contains(texto) || l.Nombre.ToLower().Contains(texto));
        if (filtro.PlantaId is int plantaId)
            consulta = consulta.Where(l => l.PlantaId == plantaId);

        var activos = await consulta.CountAsync(l => l.Activo);
        var inactivos = await consulta.CountAsync(l => !l.Activo);

        if (filtro.Estado == "activos") consulta = consulta.Where(l => l.Activo);
        else if (filtro.Estado == "inactivos") consulta = consulta.Where(l => !l.Activo);

        var resultado = await Proyectar(consulta.OrderBy(l => l.Planta!.Nombre).ThenBy(l => l.Codigo))
            .PaginarAsync(filtro.Pagina, filtro.TamanoPagina, activos, inactivos);

        foreach (var item in resultado.Items) item.TipoEnvaseNombre = Etiquetas.De(item.TipoEnvase);
        return resultado;
    }

    public async Task<LineaResponse?> ObtenerResumenAsync(int id)
    {
        var item = await Proyectar(db.Lineas.AsNoTracking().Where(l => l.Id == id)).FirstOrDefaultAsync();
        if (item is not null) item.TipoEnvaseNombre = Etiquetas.De(item.TipoEnvase);
        return item;
    }

    public Task<LineaProduccion?> ObtenerConFormatosAsync(int id) =>
        db.Lineas.Include(l => l.Formatos).FirstOrDefaultAsync(l => l.Id == id);

    public Task<bool> ExisteCodigoAsync(string codigo, int? excluirId) =>
        db.Lineas.AnyAsync(l => l.Codigo == codigo && (excluirId == null || l.Id != excluirId));

    public Task<List<Formato>> FormatosPorIdsAsync(IEnumerable<int> ids)
    {
        var lista = ids.Distinct().ToList();
        return db.Formatos.Where(f => lista.Contains(f.Id)).ToListAsync();
    }

    public Task<List<FormatoResponse>> ListarFormatosAsync() =>
        db.Formatos.AsNoTracking()
            .OrderBy(f => f.TipoEnvase).ThenBy(f => f.VolumenMl)
            .Select(f => new FormatoResponse { Id = f.Id, Nombre = f.Nombre, TipoEnvase = f.TipoEnvase, VolumenMl = f.VolumenMl })
            .ToListAsync();

    public Task<int> ContarAsync() => db.Lineas.CountAsync(l => l.Activo);

    public void Agregar(LineaProduccion linea) => db.Lineas.Add(linea);

    public void Eliminar(LineaProduccion linea) => db.Lineas.Remove(linea);

    public Task GuardarAsync() => db.SaveChangesAsync();
}

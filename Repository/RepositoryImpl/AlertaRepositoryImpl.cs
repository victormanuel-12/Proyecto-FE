using Microsoft.EntityFrameworkCore;
using SPIn.Data;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository.RepositoryImpl;

public class AlertaRepositoryImpl(ApplicationDbContext db) : IAlertaRepository
{
    public Task<List<Alerta>> NoResueltasAsync() => db.Alertas.Where(a => a.Estado != EstadoAlerta.Resuelta).ToListAsync();

    public async Task<PaginaResponse<AlertaResponse>> ListarAsync(FiltroAlertaRequest f, DateTime desdeResueltas)
    {
        var q = db.Alertas.AsNoTracking().AsQueryable();
        q = f.Estado switch
        {
            "importantes" => q.Where(a => a.Estado == EstadoAlerta.Activa && a.Severidad <= SeveridadAlerta.Alta),
            "descartadas" => q.Where(a => a.Estado == EstadoAlerta.Descartada),
            "resueltas" => q.Where(a => a.Estado == EstadoAlerta.Resuelta && a.ResueltaEn >= desdeResueltas),
            _ => q.Where(a => a.Estado == EstadoAlerta.Activa)
        };
        if (f.Tipo is TipoAlerta t) q = q.Where(a => a.Tipo == t);
        var texto = ConsultaExtensions.TextoBusqueda(f.Buscar);
        if (texto is not null) q = q.Where(a => a.Titulo.ToLower().Contains(texto) || a.Detalle.ToLower().Contains(texto));

        q = f.Estado == "resueltas"
            ? q.OrderByDescending(a => a.ResueltaEn)
            : q.OrderBy(a => a.Severidad).ThenBy(a => a.PrimeraVez).ThenBy(a => a.Id);

        var total = await q.CountAsync();
        var paginas = Math.Max(1, (int)Math.Ceiling(total / (double)f.TamanoPagina));
        var pagina = Math.Min(Math.Max(1, f.Pagina), paginas);
        var items = await q.Skip((pagina - 1) * f.TamanoPagina).Take(f.TamanoPagina).ToListAsync();
        return new PaginaResponse<AlertaResponse> { Items = items.Select(Mapear).ToList(), Total = total, Pagina = pagina, TamanoPagina = f.TamanoPagina };
    }

    public async Task<ResumenAlertasResponse> ResumenAsync()
    {
        var filas = await db.Alertas.AsNoTracking().Where(a => a.Estado != EstadoAlerta.Resuelta)
            .GroupBy(a => new { a.Estado, a.Severidad }).Select(g => new { g.Key.Estado, g.Key.Severidad, Cantidad = g.Count() }).ToListAsync();
        int Activas(SeveridadAlerta s) => filas.Where(x => x.Estado == EstadoAlerta.Activa && x.Severidad == s).Sum(x => x.Cantidad);
        return new ResumenAlertasResponse
        {
            Criticas = Activas(SeveridadAlerta.Critica), Altas = Activas(SeveridadAlerta.Alta),
            Medias = Activas(SeveridadAlerta.Media), Informativas = Activas(SeveridadAlerta.Informativa),
            Descartadas = filas.Where(x => x.Estado == EstadoAlerta.Descartada).Sum(x => x.Cantidad)
        };
    }

    public async Task<List<AlertaResponse>> PrincipalesAsync(int cantidad) =>
        (await db.Alertas.AsNoTracking().Where(a => a.Estado == EstadoAlerta.Activa)
            .OrderBy(a => a.Severidad).ThenBy(a => a.PrimeraVez).Take(cantidad).ToListAsync()).Select(Mapear).ToList();

    public Task<Alerta?> ObtenerAsync(int id) => db.Alertas.FirstOrDefaultAsync(a => a.Id == id);
    public void Agregar(Alerta alerta) => db.Alertas.Add(alerta);

    public async Task PodarResueltasAsync(DateTime antesDe)
    {
        var viejas = await db.Alertas.Where(a => a.Estado == EstadoAlerta.Resuelta && a.ResueltaEn < antesDe).ToListAsync();
        if (viejas.Count == 0) return;
        db.Alertas.RemoveRange(viejas);
        await db.SaveChangesAsync();
    }

    public async Task<List<(int Id, int Numero, string AlmacenCodigo, DateTime CreadoEn)>> ConteosAbiertosAsync() =>
        (await db.Conteos.AsNoTracking().Where(c => c.Estado == EstadoConteo.Abierto)
            .Select(c => new { c.Id, c.Numero, Codigo = c.Almacen!.Codigo, c.CreadoEn }).ToListAsync())
        .Select(c => (c.Id, c.Numero, c.Codigo, c.CreadoEn)).ToList();

    public Task GuardarAsync() => db.SaveChangesAsync();

    public static AlertaResponse Mapear(Alerta a) => new()
    {
        Id = a.Id, Tipo = a.Tipo, TipoNombre = EtiquetasAlerta.De(a.Tipo), Severidad = a.Severidad, SeveridadNombre = EtiquetasAlerta.De(a.Severidad),
        Estado = a.Estado, EstadoNombre = EtiquetasAlerta.De(a.Estado), Titulo = a.Titulo, Detalle = a.Detalle, Enlace = a.Enlace,
        TextoEnlace = a.TextoEnlace, PrimeraVez = a.PrimeraVez, UltimaVez = a.UltimaVez, DescartadaEn = a.DescartadaEn,
        DescartadaPor = a.DescartadaPor, MotivoDescarte = a.MotivoDescarte, ResueltaEn = a.ResueltaEn
    };
}

using Microsoft.EntityFrameworkCore;
using SPIn.Data;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository.RepositoryImpl;

public class AlmacenRepositoryImpl(ApplicationDbContext db) : IAlmacenRepository
{
    private static IQueryable<AlmacenResponse> Proyectar(IQueryable<Almacen> consulta) =>
        consulta.Select(a => new AlmacenResponse
        {
            Id = a.Id,
            Codigo = a.Codigo,
            Nombre = a.Nombre,
            PlantaId = a.PlantaId,
            PlantaNombre = a.Planta!.Nombre,
            Tipo = a.Tipo,
            Activo = a.Activo
        });

    public async Task<ListadoResponse<AlmacenResponse>> ListarAsync(FiltroAlmacenRequest filtro)
    {
        var consulta = db.Almacenes.AsNoTracking();

        var texto = ConsultaExtensions.TextoBusqueda(filtro.Buscar);
        if (texto is not null)
            consulta = consulta.Where(a => a.Codigo.ToLower().Contains(texto) || a.Nombre.ToLower().Contains(texto));
        if (filtro.PlantaId is int plantaId)
            consulta = consulta.Where(a => a.PlantaId == plantaId);
        if (filtro.Tipo is TipoAlmacen tipo)
            consulta = consulta.Where(a => a.Tipo == tipo);

        var activos = await consulta.CountAsync(a => a.Activo);
        var inactivos = await consulta.CountAsync(a => !a.Activo);

        if (filtro.Estado == "activos") consulta = consulta.Where(a => a.Activo);
        else if (filtro.Estado == "inactivos") consulta = consulta.Where(a => !a.Activo);

        var resultado = await Proyectar(consulta.OrderBy(a => a.Planta!.Nombre).ThenBy(a => a.Nombre))
            .PaginarAsync(filtro.Pagina, filtro.TamanoPagina, activos, inactivos);

        foreach (var item in resultado.Items) item.TipoNombre = Etiquetas.De(item.Tipo);
        return resultado;
    }

    public async Task<AlmacenResponse?> ObtenerResumenAsync(int id)
    {
        var item = await Proyectar(db.Almacenes.AsNoTracking().Where(a => a.Id == id)).FirstOrDefaultAsync();
        if (item is not null) item.TipoNombre = Etiquetas.De(item.Tipo);
        return item;
    }

    public Task<List<OpcionResponse>> OpcionesAsync(bool soloActivos, bool soloInsumos, bool soloProductoTerminado = false) =>
        db.Almacenes.AsNoTracking()
            .Where(a => (!soloActivos || a.Activo) && (!soloInsumos || a.Tipo != TipoAlmacen.ProductoTerminado)
                        && (!soloProductoTerminado || a.Tipo == TipoAlmacen.ProductoTerminado))
            .OrderBy(a => a.Codigo)
            .Select(a => new OpcionResponse { Id = a.Id, Nombre = a.Codigo + " · " + a.Nombre })
            .ToListAsync();

    public Task<Almacen?> ObtenerAsync(int id) => db.Almacenes.FirstOrDefaultAsync(a => a.Id == id);

    public Task<bool> ExisteCodigoAsync(string codigo, int? excluirId) =>
        db.Almacenes.AnyAsync(a => a.Codigo == codigo && (excluirId == null || a.Id != excluirId));

    public Task<int> ContarAsync() => db.Almacenes.CountAsync(a => a.Activo);

    public void Agregar(Almacen almacen) => db.Almacenes.Add(almacen);

    public void Eliminar(Almacen almacen) => db.Almacenes.Remove(almacen);

    public Task GuardarAsync() => db.SaveChangesAsync();
}

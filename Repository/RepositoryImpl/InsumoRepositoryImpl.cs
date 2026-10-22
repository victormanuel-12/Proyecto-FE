using Microsoft.EntityFrameworkCore;
using SPIn.Data;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository.RepositoryImpl;

public class InsumoRepositoryImpl(ApplicationDbContext db) : IInsumoRepository
{
    private static IQueryable<InsumoResponse> Proyectar(IQueryable<Insumo> consulta) =>
        consulta.Select(i => new InsumoResponse
        {
            Id = i.Id,
            Codigo = i.Codigo,
            Nombre = i.Nombre,
            CategoriaId = i.CategoriaId,
            CategoriaNombre = i.Categoria!.Nombre,
            UnidadCompraId = i.UnidadCompraId,
            UnidadCompraCodigo = i.UnidadCompra!.Codigo,
            UnidadConsumoId = i.UnidadConsumoId,
            UnidadConsumoCodigo = i.UnidadConsumo!.Codigo,
            FactorConversion = i.FactorConversion,
            StockMinimo = i.StockMinimo,
            StockMaximo = i.StockMaximo,
            CapacidadMaxima = i.CapacidadMaxima,
            EsCritico = i.EsCritico,
            EsImportado = i.EsImportado,
            NivelServicio = i.NivelServicio,
            TipoConsumo = i.TipoConsumo,
            ConsumoEstimadoDiario = i.ConsumoEstimadoDiario,
            ControlaLote = i.ControlaLote,
            Activo = i.Activo,
            Proveedores = i.Proveedores.Count,
            ProveedorPrincipal = i.Proveedores.Where(c => c.EsPrincipal)
                .Select(c => c.Proveedor!.NombreComercial ?? c.Proveedor.RazonSocial).FirstOrDefault(),
            ProveedorPrincipalActivo = i.Proveedores.Where(c => c.EsPrincipal)
                .Select(c => (bool?)c.Proveedor!.Activo).FirstOrDefault(),
            EntregaPrincipalDias = i.Proveedores.Where(c => c.EsPrincipal)
                .Select(c => (int?)c.TiempoEntregaDias).FirstOrDefault()
        });

    public async Task<ListadoResponse<InsumoResponse>> ListarAsync(FiltroInsumoRequest filtro)
    {
        var consulta = db.Insumos.AsNoTracking();

        var texto = ConsultaExtensions.TextoBusqueda(filtro.Buscar);
        if (texto is not null)
            consulta = consulta.Where(i => i.Codigo.ToLower().Contains(texto) || i.Nombre.ToLower().Contains(texto));
        if (filtro.CategoriaId is int categoriaId)
            consulta = consulta.Where(i => i.CategoriaId == categoriaId);
        if (filtro.Criticos == true)
            consulta = consulta.Where(i => i.EsCritico);
        if (filtro.Importados is bool importados)
            consulta = consulta.Where(i => i.EsImportado == importados);

        var activos = await consulta.CountAsync(i => i.Activo);
        var inactivos = await consulta.CountAsync(i => !i.Activo);
        if (filtro.Estado == "activos") consulta = consulta.Where(i => i.Activo);
        else if (filtro.Estado == "inactivos") consulta = consulta.Where(i => !i.Activo);

        var resultado = await Proyectar(consulta.OrderByDescending(i => i.EsCritico).ThenBy(i => i.Codigo))
            .PaginarAsync(filtro.Pagina, filtro.TamanoPagina, activos, inactivos);
        foreach (var item in resultado.Items) item.TipoConsumoNombre = EtiquetasCatalogo.De(item.TipoConsumo);
        return resultado;
    }

    public Task<List<OpcionResponse>> OpcionesAsync(bool soloActivos) =>
        db.Insumos.AsNoTracking()
            .Where(i => !soloActivos || i.Activo)
            .OrderBy(i => i.Codigo)
            .Select(i => new OpcionResponse { Id = i.Id, Nombre = i.Codigo + " · " + i.Nombre })
            .ToListAsync();

    public async Task<InsumoResponse?> ObtenerResumenAsync(int id)
    {
        var item = await Proyectar(db.Insumos.AsNoTracking().Where(i => i.Id == id)).FirstOrDefaultAsync();
        if (item is not null) item.TipoConsumoNombre = EtiquetasCatalogo.De(item.TipoConsumo);
        return item;
    }

    public Task<Insumo?> ObtenerAsync(int id) => db.Insumos.FirstOrDefaultAsync(i => i.Id == id);

    public Task<bool> ExisteCodigoAsync(string codigo, int? excluirId) =>
        db.Insumos.AnyAsync(i => i.Codigo == codigo && (excluirId == null || i.Id != excluirId));

    public Task<bool> TieneCondicionesAsync(int id) => db.ProveedorInsumos.AnyAsync(c => c.InsumoId == id);

    public Task<int> ContarAsync() => db.Insumos.CountAsync(i => i.Activo);

    public void Agregar(Insumo insumo) => db.Insumos.Add(insumo);
    public void Eliminar(Insumo insumo) => db.Insumos.Remove(insumo);
    public Task GuardarAsync() => db.SaveChangesAsync();
}

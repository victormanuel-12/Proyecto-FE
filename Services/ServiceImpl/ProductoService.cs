using SPIn.Common;
using SPIn.Common.Excepciones;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;

namespace SPIn.Services.ServiceImpl;

public class ProductoService(IProductoRepository repo, IRecetaRepository recetas, IVentaRepository ventas, IProduccionOperacionRepository produccion, ILogger<ProductoService> logger) : IProductoService
{
    public async Task<GenericResponse<ListadoResponse<ProductoResponse>>> ListarAsync(FiltroProductoRequest filtro) =>
        new(await repo.ListarAsync(filtro, Reloj.Hoy));

    public async Task<GenericResponse<List<OpcionResponse>>> OpcionesConRecetaAsync(int? excluirId) =>
        new(await repo.OpcionesConRecetaAsync(Reloj.Hoy, excluirId));

    public async Task<GenericResponse<List<ProductoOpcionResponse>>> OpcionesAsync(bool soloActivos) =>
        new(await repo.OpcionesAsync(soloActivos));

    public async Task<GenericResponse<ProductoResponse>> ObtenerAsync(int id) => new(await DetalleAsync(id));

    public async Task<GenericResponse<ProductoResponse>> CrearAsync(ProductoRequest request)
    {
        var codigo = Validaciones.NormalizarCodigo(request.Codigo);
        await ValidarAsync(request, codigo, null);

        var producto = new Producto { Codigo = codigo, Activo = true };
        Copiar(request, producto);
        repo.Agregar(producto);
        await repo.GuardarAsync();
        logger.LogInformation("Producto {ProductoId} ({Codigo}) creado", producto.Id, producto.Codigo);

        return new GenericResponse<ProductoResponse>(true, await DetalleAsync(producto.Id),
            "Producto registrado. Ahora defina su receta: qué insumos consume cada botella.");
    }

    public async Task<GenericResponse<ProductoResponse>> ActualizarAsync(int id, ProductoRequest request)
    {
        var producto = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("El producto no existe.");
        var codigo = Validaciones.NormalizarCodigo(request.Codigo);
        await ValidarAsync(request, codigo, id);

        producto.Codigo = codigo;
        Copiar(request, producto);
        await repo.GuardarAsync();
        return new GenericResponse<ProductoResponse>(true, await DetalleAsync(id), "Producto actualizado.");
    }

    public async Task<GenericResponse<ProductoResponse>> CambiarEstadoAsync(int id, bool activo)
    {
        var producto = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("El producto no existe.");
        producto.Activo = activo;
        await repo.GuardarAsync();
        return new GenericResponse<ProductoResponse>(true, await DetalleAsync(id),
            activo ? "Producto activado." : "Producto desactivado. Ya no se podrá planificar ni producir.");
    }

    public async Task<GenericResponse<object>> EliminarAsync(int id)
    {
        var producto = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("El producto no existe.");
        if (await repo.TieneRecetasAsync(id))
            throw GenericException.Conflicto("No se puede eliminar: el producto ya tiene receta. Puede desactivarlo.");
        if (await ventas.ProductoTieneVentasAsync(id) || await ventas.ProductoTieneMovimientosAsync(id))
            throw GenericException.Conflicto("No se puede eliminar: el producto tiene ventas o movimientos de stock. Puede desactivarlo.");
        if (await produccion.ProductoUsadoAsync(id))
            throw GenericException.Conflicto("No se puede eliminar: el producto tiene producción registrada o programada. Puede desactivarlo.");
        repo.Eliminar(producto);
        await repo.GuardarAsync();
        logger.LogInformation("Producto {ProductoId} ({Codigo}) eliminado", id, producto.Codigo);
        return new GenericResponse<object>(true, "Producto eliminado.");
    }

    private async Task ValidarAsync(ProductoRequest r, string codigo, int? id)
    {
        if (await repo.ExisteCodigoAsync(codigo, id))
            throw GenericException.Campo("codigo", $"Ya existe un producto con el código {codigo}.");
        if (!await repo.FormatoExisteAsync(r.FormatoId))
            throw GenericException.Campo("formatoId", "Seleccione un formato válido.");
    }

    private static void Copiar(ProductoRequest r, Producto p)
    {
        p.Nombre = r.Nombre.Trim();
        p.Sabor = Validaciones.TextoOpcional(r.Sabor);
        p.FormatoId = r.FormatoId;
        p.UnidadesPorPaquete = r.UnidadesPorPaquete;
        p.PaquetesPorPallet = r.PaquetesPorPallet;
    }

    private async Task<ProductoResponse> DetalleAsync(int id)
    {
        var hoy = Reloj.Hoy;
        var producto = await repo.ObtenerResumenAsync(id, hoy) ?? throw GenericException.NoEncontrado("El producto no existe.");
        producto.RecetaVigente = await recetas.VigenteAsync(id, hoy, hoy);
        producto.Historial = await recetas.HistorialAsync(id, hoy);
        producto.LineasCompatibles = await repo.LineasCompatiblesAsync(producto.FormatoId);
        return producto;
    }
}

using SPIn.Common.Excepciones;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;

namespace SPIn.Services.ServiceImpl;

/// <summary>
/// Condiciones de compra (proveedor ↔ insumo).
/// Regla: cada insumo con proveedores tiene exactamente un proveedor principal,
/// que es el que usará el cálculo de reabastecimiento (Entrega 8).
/// </summary>
public class CondicionCompraService(
    ICondicionCompraRepository repo,
    IInsumoRepository insumos,
    IProveedorRepository proveedores,
    ILogger<CondicionCompraService> logger) : ICondicionCompraService
{
    public async Task<GenericResponse<List<CondicionCompraResponse>>> ListarPorInsumoAsync(int insumoId)
    {
        _ = await insumos.ObtenerAsync(insumoId) ?? throw GenericException.NoEncontrado("El insumo no existe.");
        return new GenericResponse<List<CondicionCompraResponse>>(await repo.ListarPorInsumoAsync(insumoId));
    }

    public async Task<GenericResponse<List<CondicionCompraResponse>>> CrearAsync(CondicionCompraRequest request)
    {
        var insumo = await insumos.ObtenerAsync(request.InsumoId) ?? throw GenericException.NoEncontrado("El insumo no existe.");
        if (!insumo.Activo) throw new GenericException("El insumo está inactivo.");

        var proveedor = await proveedores.ObtenerAsync(request.ProveedorId);
        if (proveedor is null || !proveedor.Activo)
            throw GenericException.Campo("proveedorId", "Seleccione un proveedor activo.");
        if (await repo.ExisteParAsync(request.ProveedorId, request.InsumoId, null))
            throw GenericException.Campo("proveedorId", "Este proveedor ya está asignado a este insumo. Edite sus condiciones.");
        ValidarEntrega(request);

        var existentes = await repo.EntidadesPorInsumoAsync(request.InsumoId);
        var condicion = new ProveedorInsumo { InsumoId = request.InsumoId, ProveedorId = request.ProveedorId };
        Copiar(request, condicion);

        // El primero siempre es principal; si se marca como principal, los demás dejan de serlo.
        condicion.EsPrincipal = request.EsPrincipal || existentes.Count == 0;
        if (condicion.EsPrincipal) existentes.ForEach(c => c.EsPrincipal = false);

        repo.Agregar(condicion);
        await repo.GuardarAsync();
        logger.LogInformation("Proveedor {ProveedorId} asignado al insumo {InsumoId}", request.ProveedorId, request.InsumoId);

        return new GenericResponse<List<CondicionCompraResponse>>(true, await repo.ListarPorInsumoAsync(request.InsumoId),
            condicion.EsPrincipal ? "Proveedor asignado como principal." : "Proveedor asignado.");
    }

    public async Task<GenericResponse<List<CondicionCompraResponse>>> ActualizarAsync(int id, CondicionCompraRequest request)
    {
        var condicion = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("La condición de compra no existe.");
        if (condicion.ProveedorId != request.ProveedorId || condicion.InsumoId != request.InsumoId)
            throw new GenericException("No se puede cambiar el proveedor ni el insumo: elimine esta condición y cree otra.");
        ValidarEntrega(request);

        Copiar(request, condicion);
        if (request.EsPrincipal && !condicion.EsPrincipal)
            await MarcarComoUnicoPrincipalAsync(condicion);

        await repo.GuardarAsync();
        return new GenericResponse<List<CondicionCompraResponse>>(true, await repo.ListarPorInsumoAsync(condicion.InsumoId), "Condiciones actualizadas.");
    }

    public async Task<GenericResponse<List<CondicionCompraResponse>>> MarcarPrincipalAsync(int id)
    {
        var condicion = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("La condición de compra no existe.");
        var proveedor = await proveedores.ObtenerAsync(condicion.ProveedorId);
        if (proveedor is null || !proveedor.Activo)
            throw new GenericException("No se puede marcar como principal a un proveedor inactivo.");

        await MarcarComoUnicoPrincipalAsync(condicion);
        await repo.GuardarAsync();
        return new GenericResponse<List<CondicionCompraResponse>>(true, await repo.ListarPorInsumoAsync(condicion.InsumoId), "Proveedor principal actualizado.");
    }

    public async Task<GenericResponse<List<CondicionCompraResponse>>> EliminarAsync(int id)
    {
        var condicion = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("La condición de compra no existe.");
        var insumoId = condicion.InsumoId;
        var eraPrincipal = condicion.EsPrincipal;
        repo.Eliminar(condicion);

        var mensaje = "Proveedor quitado del insumo.";
        if (eraPrincipal)
        {
            // Pasa a principal el proveedor activo que entrega más rápido.
            var reemplazo = (await repo.EntidadesPorInsumoAsync(insumoId))
                .Where(c => c.Id != id && c.Proveedor is { Activo: true })
                .OrderBy(c => c.TiempoEntregaDias)
                .FirstOrDefault();
            if (reemplazo is not null)
            {
                reemplazo.EsPrincipal = true;
                mensaje = $"Proveedor quitado. Ahora el principal es {reemplazo.Proveedor!.NombreComercial ?? reemplazo.Proveedor.RazonSocial}.";
            }
            else
            {
                mensaje = "Proveedor quitado. El insumo quedó sin proveedor principal activo.";
            }
        }

        await repo.GuardarAsync();
        return new GenericResponse<List<CondicionCompraResponse>>(true, await repo.ListarPorInsumoAsync(insumoId), mensaje);
    }

    private async Task MarcarComoUnicoPrincipalAsync(ProveedorInsumo condicion)
    {
        foreach (var otra in await repo.EntidadesPorInsumoAsync(condicion.InsumoId))
            otra.EsPrincipal = otra.Id == condicion.Id;
        condicion.EsPrincipal = true;
    }

    private static void ValidarEntrega(CondicionCompraRequest r)
    {
        if (r.VariacionEntregaDias > r.TiempoEntregaDias)
            throw GenericException.Campo("variacionEntregaDias", "La variación no puede ser mayor que el tiempo de entrega.");
    }

    private static void Copiar(CondicionCompraRequest r, ProveedorInsumo c)
    {
        c.Moneda = r.Moneda;
        c.PrecioUnitario = r.PrecioUnitario;
        c.TiempoEntregaDias = r.TiempoEntregaDias;
        c.VariacionEntregaDias = r.VariacionEntregaDias;
        c.PedidoMinimo = r.PedidoMinimo;
        c.MultiploCompra = r.MultiploCompra;
    }
}

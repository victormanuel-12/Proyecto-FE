using SPIn.Common;
using SPIn.Common.Excepciones;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;

namespace SPIn.Services.ServiceImpl;

public class InsumoService(
    IInsumoRepository repo,
    IPedidoRepository pedidos,
    ICategoriaRepository categorias,
    IUnidadRepository unidades,
    ICondicionCompraRepository condiciones,
    IRecetaRepository recetas,
    IInventarioRepository inventario,
    ILogger<InsumoService> logger) : IInsumoService
{
    public async Task<GenericResponse<ListadoResponse<InsumoResponse>>> ListarAsync(FiltroInsumoRequest filtro) =>
        new(await repo.ListarAsync(filtro));

    public async Task<GenericResponse<List<OpcionResponse>>> OpcionesAsync(bool soloActivos) =>
        new(await repo.OpcionesAsync(soloActivos));

    public async Task<GenericResponse<InsumoResponse>> ObtenerAsync(int id) =>
        new(await DetalleAsync(id));

    public async Task<GenericResponse<InsumoResponse>> CrearAsync(InsumoRequest request)
    {
        var codigo = Validaciones.NormalizarCodigo(request.Codigo);
        await ValidarAsync(request, null);
        if (await repo.ExisteCodigoAsync(codigo, null))
            throw GenericException.Campo("codigo", $"Ya existe un insumo con el código {codigo}.");

        var insumo = new Insumo { Codigo = codigo, Activo = true };
        Copiar(request, insumo);
        repo.Agregar(insumo);
        await repo.GuardarAsync();
        logger.LogInformation("Insumo {InsumoId} ({Codigo}) creado", insumo.Id, insumo.Codigo);

        return new GenericResponse<InsumoResponse>(true, await DetalleAsync(insumo.Id),
            "Insumo registrado. Ahora agregue su proveedor principal.");
    }

    public async Task<GenericResponse<InsumoResponse>> ActualizarAsync(int id, InsumoRequest request)
    {
        var insumo = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("El insumo no existe.");
        var codigo = Validaciones.NormalizarCodigo(request.Codigo);
        await ValidarAsync(request, insumo);
        if (await repo.ExisteCodigoAsync(codigo, id))
            throw GenericException.Campo("codigo", $"Ya existe otro insumo con el código {codigo}.");

        insumo.Codigo = codigo;
        Copiar(request, insumo);
        await repo.GuardarAsync();
        return new GenericResponse<InsumoResponse>(true, await DetalleAsync(id), "Insumo actualizado.");
    }

    public async Task<GenericResponse<InsumoResponse>> CambiarEstadoAsync(int id, bool activo)
    {
        var insumo = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("El insumo no existe.");
        insumo.Activo = activo;
        await repo.GuardarAsync();

        var mensaje = "Insumo activado.";
        if (!activo)
        {
            var enUso = await recetas.ProductosQueUsanInsumoAsync(id, Reloj.Hoy);
            mensaje = enUso == 0
                ? "Insumo desactivado. Ya no se considerará en pronósticos ni alertas."
                : $"Insumo desactivado. Atención: está en la receta vigente de {enUso} producto(s); actualice esas recetas.";
        }
        return new GenericResponse<InsumoResponse>(true, await DetalleAsync(id), mensaje);
    }

    public async Task<GenericResponse<object>> EliminarAsync(int id)
    {
        var insumo = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("El insumo no existe.");
        if (await inventario.InsumoTieneMovimientosAsync(id))
            throw GenericException.Conflicto("No se puede eliminar: el insumo tiene movimientos en el kardex. Puede desactivarlo.");
        if (await repo.TieneCondicionesAsync(id))
            throw GenericException.Conflicto("No se puede eliminar: el insumo tiene proveedores asignados. Quítelos primero o desactive el insumo.");
        if (await recetas.InsumoEnAlgunaRecetaAsync(id))
            throw GenericException.Conflicto("No se puede eliminar: el insumo está en la receta de algún producto. Puede desactivarlo.");
        if (await pedidos.InsumoUsadoAsync(id))
            throw GenericException.Conflicto("No se puede eliminar: el insumo figura en pedidos. Puede desactivarlo.");

        repo.Eliminar(insumo);
        await repo.GuardarAsync();
        logger.LogInformation("Insumo {InsumoId} ({Codigo}) eliminado", id, insumo.Codigo);
        return new GenericResponse<object>(true, "Insumo eliminado.");
    }

    // ---------------------------------------------------------------- reglas

    private async Task ValidarAsync(InsumoRequest r, Insumo? actual)
    {
        if (actual is null || actual.CategoriaId != r.CategoriaId)
        {
            var categoria = await categorias.ObtenerAsync(r.CategoriaId);
            if (categoria is null || !categoria.Activo)
                throw GenericException.Campo("categoriaId", "Seleccione una categoría activa.");
        }

        if (actual is null || actual.UnidadCompraId != r.UnidadCompraId)
        {
            var unidad = await unidades.ObtenerAsync(r.UnidadCompraId);
            if (unidad is null || !unidad.Activo)
                throw GenericException.Campo("unidadCompraId", "Seleccione una unidad activa.");
        }

        if (actual is null || actual.UnidadConsumoId != r.UnidadConsumoId)
        {
            var unidad = await unidades.ObtenerAsync(r.UnidadConsumoId);
            if (unidad is null || !unidad.Activo)
                throw GenericException.Campo("unidadConsumoId", "Seleccione una unidad activa.");
        }

        if (r.UnidadCompraId == r.UnidadConsumoId && r.FactorConversion != 1)
            throw GenericException.Campo("factorConversion", "Si compra y consume en la misma unidad, el factor debe ser 1.");

        if (r.StockMaximo < r.StockMinimo)
            throw GenericException.Campo("stockMaximo", "El stock máximo no puede ser menor que el mínimo.");

        if (r.CapacidadMaxima is decimal capacidad && capacidad < r.StockMaximo)
            throw GenericException.Campo("capacidadMaxima", "La capacidad de almacenamiento no puede ser menor que el stock máximo.");
    }

    private static void Copiar(InsumoRequest r, Insumo i)
    {
        i.Nombre = r.Nombre.Trim();
        i.CategoriaId = r.CategoriaId;
        i.UnidadCompraId = r.UnidadCompraId;
        i.UnidadConsumoId = r.UnidadConsumoId;
        i.FactorConversion = r.FactorConversion;
        i.StockMinimo = r.StockMinimo;
        i.StockMaximo = r.StockMaximo;
        i.CapacidadMaxima = r.CapacidadMaxima;
        i.EsCritico = r.EsCritico;
        i.EsImportado = r.EsImportado;
        i.NivelServicio = r.NivelServicio;
        i.TipoConsumo = r.TipoConsumo;
        i.ConsumoEstimadoDiario = r.ConsumoEstimadoDiario;
        i.ControlaLote = r.ControlaLote;
    }

    private async Task<InsumoResponse> DetalleAsync(int id)
    {
        var insumo = await repo.ObtenerResumenAsync(id) ?? throw GenericException.NoEncontrado("El insumo no existe.");
        insumo.Condiciones = await condiciones.ListarPorInsumoAsync(id);
        insumo.Stock = await inventario.StockPorInsumoAsync(id);
        return insumo;
    }
}

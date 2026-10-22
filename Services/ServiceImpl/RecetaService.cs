using SPIn.Common;
using SPIn.Common.Excepciones;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;

namespace SPIn.Services.ServiceImpl;

/// <summary>
/// Recetas versionadas (lista de materiales) y cálculo de consumo de insumos.
///
/// Al guardar una receta:
/// - Si la fecha de inicio es la misma que la de la última versión, se corrige esa versión.
/// - Si es posterior, se crea la versión siguiente y la anterior se cierra el día previo.
/// - No se permite una fecha anterior a la última versión: cambiaría el consumo de producciones ya registradas.
/// </summary>
public class RecetaService(
    IRecetaRepository repo,
    IProductoRepository productos,
    IProductoService productoService,
    ILogger<RecetaService> logger) : IRecetaService
{
    public async Task<GenericResponse<List<InsumoRecetaOpcion>>> InsumosDisponiblesAsync() =>
        new(await repo.InsumosDisponiblesAsync());

    public async Task<GenericResponse<RecetaResponse>> ObtenerAsync(int recetaId) =>
        new(await repo.ObtenerAsync(recetaId, Reloj.Hoy) ?? throw GenericException.NoEncontrado("La receta no existe."));

    public async Task<GenericResponse<RecetaResponse>> VigenteAsync(int productoId, DateOnly? fecha)
    {
        _ = await productos.ObtenerAsync(productoId) ?? throw GenericException.NoEncontrado("El producto no existe.");
        var dia = fecha ?? Reloj.Hoy;
        var receta = await repo.VigenteAsync(productoId, dia, Reloj.Hoy)
            ?? throw GenericException.NoEncontrado($"El producto no tiene receta vigente al {dia:dd/MM/yyyy}.");
        return new GenericResponse<RecetaResponse>(receta);
    }

    public async Task<GenericResponse<ProductoResponse>> GuardarAsync(int productoId, RecetaRequest request)
    {
        var producto = await productos.ObtenerAsync(productoId) ?? throw GenericException.NoEncontrado("El producto no existe.");
        if (!producto.Activo) throw new GenericException("El producto está inactivo: actívelo antes de cambiar su receta.");

        var desde = request.VigenteDesde == default ? Reloj.Hoy : request.VigenteDesde;
        await ValidarDetallesAsync(request.Detalles);

        var ultima = await repo.UltimaConDetallesAsync(productoId);
        string mensaje;

        if (ultima is not null && desde < ultima.VigenteDesde)
        {
            throw GenericException.Campo("vigenteDesde",
                $"La fecha no puede ser anterior al {ultima.VigenteDesde:dd/MM/yyyy}, inicio de la versión {ultima.Version}.");
        }

        if (ultima is not null && desde == ultima.VigenteDesde)
        {
            // Corrección de la misma versión (mismo día de inicio).
            foreach (var d in ultima.Detalles.ToList()) repo.QuitarDetalle(d);
            ultima.Comentario = Validaciones.TextoOpcional(request.Comentario);
            foreach (var d in request.Detalles) ultima.Detalles.Add(NuevoDetalle(d));
            mensaje = $"Receta versión {ultima.Version} corregida.";
        }
        else
        {
            var version = (ultima?.Version ?? 0) + 1;
            if (ultima is not null) ultima.VigenteHasta = desde.AddDays(-1);

            var nueva = new Receta
            {
                ProductoId = productoId,
                Version = version,
                VigenteDesde = desde,
                Comentario = Validaciones.TextoOpcional(request.Comentario)
            };
            foreach (var d in request.Detalles) nueva.Detalles.Add(NuevoDetalle(d));
            repo.Agregar(nueva);

            mensaje = ultima is null
                ? "Receta guardada (versión 1)."
                : $"Versión {version} guardada. Rige desde el {desde:dd/MM/yyyy}; la versión {ultima.Version} queda hasta el {desde.AddDays(-1):dd/MM/yyyy}.";
        }

        await repo.GuardarAsync();
        logger.LogInformation("Receta del producto {ProductoId} guardada desde {Desde}", productoId, desde);

        var detalle = await productoService.ObtenerAsync(productoId);
        return new GenericResponse<ProductoResponse>(true, detalle.Data!, mensaje);
    }

    public async Task<GenericResponse<ExplosionResponse>> ExplotarAsync(int productoId, ExplosionRequest request)
    {
        var producto = await productos.ObtenerAsync(productoId) ?? throw GenericException.NoEncontrado("El producto no existe.");
        var fecha = request.Fecha ?? Reloj.Hoy;
        var receta = await repo.VigenteAsync(productoId, fecha, Reloj.Hoy)
            ?? throw new GenericException($"El producto no tiene receta vigente al {fecha:dd/MM/yyyy}.");

        // Todo se lleva a botellas, paquetes y pallets.
        decimal porPaquete = producto.UnidadesPorPaquete, porPallet = producto.PaquetesPorPallet;
        var botellas = request.Unidad switch
        {
            UnidadExplosion.Paquetes => request.Cantidad * porPaquete,
            UnidadExplosion.Pallets => request.Cantidad * porPaquete * porPallet,
            _ => request.Cantidad
        };
        var paquetes = botellas / porPaquete;
        var pallets = paquetes / porPallet;

        var insumos = (await repo.InsumosConUnidadesAsync(receta.Detalles.Select(d => d.InsumoId)))
            .ToDictionary(i => i.Id);

        var consumos = receta.Detalles.Select(d =>
        {
            var insumo = insumos[d.InsumoId];
            var base_ = d.Nivel switch
            {
                NivelReceta.PorPaquete => paquetes,
                NivelReceta.PorPallet => pallets,
                _ => botellas
            };
            var neto = d.Cantidad * base_;
            var total = neto * (1 + d.MermaPorcentaje / 100m);
            return new ConsumoInsumoResponse
            {
                InsumoId = insumo.Id,
                InsumoCodigo = insumo.Codigo,
                InsumoNombre = insumo.Nombre,
                UnidadConsumoCodigo = insumo.UnidadConsumo?.Codigo ?? string.Empty,
                UnidadCompraCodigo = insumo.UnidadCompra?.Codigo ?? string.Empty,
                FactorConversion = insumo.FactorConversion,
                ConsumoNeto = Math.Round(neto, 4),
                ConsumoTotal = Math.Round(total, 4),
                EnUnidadCompra = insumo.FactorConversion > 0 ? Math.Round(total / insumo.FactorConversion, 4) : 0
            };
        }).ToList();

        return new GenericResponse<ExplosionResponse>(new ExplosionResponse
        {
            ProductoId = producto.Id,
            ProductoNombre = producto.Nombre,
            RecetaVersion = receta.Version,
            Fecha = fecha,
            Botellas = Math.Round(botellas, 2),
            Paquetes = Math.Round(paquetes, 2),
            Pallets = Math.Round(pallets, 4),
            Insumos = consumos
        });
    }

    // ---------------------------------------------------------------- reglas de las líneas

    private async Task ValidarDetallesAsync(List<RecetaDetalleRequest> detalles)
    {
        var repetido = detalles.GroupBy(d => d.InsumoId).FirstOrDefault(g => g.Count() > 1);
        if (repetido is not null)
        {
            var fila = detalles.FindLastIndex(d => d.InsumoId == repetido.Key) + 1;
            throw GenericException.Campo("detalles", $"Fila {fila}: el insumo ya está en la receta. Use una sola fila por insumo.");
        }

        var insumos = (await repo.InsumosPorIdsAsync(detalles.Select(d => d.InsumoId))).ToDictionary(i => i.Id);
        for (var i = 0; i < detalles.Count; i++)
        {
            if (!insumos.TryGetValue(detalles[i].InsumoId, out var insumo))
                throw GenericException.Campo("detalles", $"Fila {i + 1}: el insumo no existe.");
            if (!insumo.Activo)
                throw GenericException.Campo("detalles", $"Fila {i + 1}: {insumo.Codigo} está inactivo.");
            if (insumo.TipoConsumo != TipoConsumo.PorProduccion)
                throw GenericException.Campo("detalles",
                    $"Fila {i + 1}: {insumo.Codigo} se consume «{EtiquetasCatalogo.De(insumo.TipoConsumo)}», no por producción. Cámbielo en Insumos si corresponde.");
        }
    }

    private static RecetaDetalle NuevoDetalle(RecetaDetalleRequest d) => new()
    {
        InsumoId = d.InsumoId,
        Cantidad = d.Cantidad,
        Nivel = d.Nivel,
        MermaPorcentaje = d.MermaPorcentaje
    };
}

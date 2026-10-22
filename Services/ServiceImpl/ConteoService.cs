using SPIn.Common;
using SPIn.Common.Excepciones;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;

namespace SPIn.Services.ServiceImpl;

/// <summary>
/// Conteo físico: se toma una foto del stock del sistema, el almacén cuenta y anota, y al aprobar
/// se generan ajustes por la diferencia entre lo contado y el stock en ese momento.
/// - Un solo conteo abierto por almacén.
/// - Diferencias mayores al umbral (5 %) solo las aprueba un Administrador.
/// </summary>
public class ConteoService(
    IInventarioRepository repo,
    IInventarioService inventario,
    IAlmacenRepository almacenes,
    ICategoriaRepository categorias,
    IPrediccionRepository configuracion,
    ITenantProvider tenant,
    IHttpContextAccessor http,
    ILogger<ConteoService> logger) : IConteoService
{
    /// <summary>Diferencia (%) a partir de la cual se requiere un Administrador (Configuración, Entrega 8).</summary>
    private async Task<decimal> UmbralAsync() => (await configuracion.ConfiguracionAsync()).UmbralConteo;

    private bool EsAdministrador =>
        http.HttpContext?.User is { } u && (u.IsInRole(Roles.SuperAdmin) || u.IsInRole(Roles.Administrador));

    public async Task<GenericResponse<PaginaResponse<ConteoResponse>>> ListarAsync(FiltroConteoRequest filtro) =>
        new(await repo.ListarConteosAsync(filtro));

    public async Task<GenericResponse<ConteoDetalleCompletoResponse>> ObtenerAsync(int id) =>
        new(await CompletoAsync(id));

    public async Task<GenericResponse<ConteoDetalleCompletoResponse>> CrearAsync(ConteoCrearRequest r)
    {
        var almacen = await almacenes.ObtenerAsync(r.AlmacenId);
        if (almacen is null || !almacen.Activo) throw GenericException.Campo("almacenId", "Seleccione un almacén activo.");
        if (almacen.Tipo == TipoAlmacen.ProductoTerminado)
            throw GenericException.Campo("almacenId", "El conteo de insumos se hace en almacenes de insumos o cuarentena.");
        if (r.CategoriaId is int categoriaId && await categorias.ObtenerAsync(categoriaId) is null)
            throw GenericException.Campo("categoriaId", "La categoría no existe.");

        var id = await repo.EnTransaccionAsync(async () =>
        {
            if (await repo.HayConteoAbiertoAsync(r.AlmacenId))
                throw GenericException.Campo("almacenId", "Ya hay un conteo abierto en este almacén. Termínelo o anúlelo primero.");

            var stock = await repo.StockDeAlmacenAsync(r.AlmacenId);
            var lista = (await repo.InsumosParaConteoAsync(r.CategoriaId))
                .Where(i => !r.SoloConStock || stock.GetValueOrDefault(i.Id) > 0)
                .ToList();
            if (lista.Count == 0)
                throw new GenericException("No hay insumos que contar con esos criterios.");

            var conteo = new ConteoFisico
            {
                Numero = await repo.SiguienteNumeroConteoAsync(),
                AlmacenId = r.AlmacenId,
                CategoriaId = r.CategoriaId,
                Observacion = Validaciones.TextoOpcional(r.Observacion),
                Estado = EstadoConteo.Abierto,
                CreadoEn = Reloj.Ahora,
                CreadoPor = tenant.UsuarioNombre ?? "sistema"
            };
            foreach (var insumo in lista)
                conteo.Detalles.Add(new ConteoDetalle { InsumoId = insumo.Id, StockSistema = stock.GetValueOrDefault(insumo.Id) });

            repo.AgregarConteo(conteo);
            await repo.GuardarAsync();
            return conteo.Id;
        });

        logger.LogInformation("Conteo {ConteoId} creado en almacén {AlmacenId}", id, r.AlmacenId);
        return new GenericResponse<ConteoDetalleCompletoResponse>(true, await CompletoAsync(id),
            "Conteo creado. Imprima la hoja, cuente y registre las cantidades.");
    }

    public async Task<GenericResponse<ConteoDetalleCompletoResponse>> GuardarAsync(int id, ConteoGuardarRequest request)
    {
        var conteo = await ConteoAbiertoAsync(id);
        Aplicar(conteo, request);
        await repo.GuardarAsync();
        var completo = await CompletoAsync(id);
        return new GenericResponse<ConteoDetalleCompletoResponse>(true, completo,
            $"Avance guardado: {completo.Contados} de {completo.Insumos} insumos contados.");
    }

    public async Task<GenericResponse<ConteoDetalleCompletoResponse>> AprobarAsync(int id, ConteoGuardarRequest request)
    {
        var ajustes = await repo.EnTransaccionAsync(async () =>
        {
            var conteo = await ConteoAbiertoAsync(id);
            Aplicar(conteo, request);

            var pendientes = conteo.Detalles.Count(d => d.CantidadContada is null);
            if (pendientes > 0)
                throw new GenericException($"Faltan contar {pendientes} insumo(s). Si un insumo no tiene existencias, registre 0.");

            var actual = await repo.StockDeAlmacenAsync(conteo.AlmacenId);
            var umbral = await UmbralAsync();
            var grandes = conteo.Detalles.Count(d => PorcentajeDiferencia(actual.GetValueOrDefault(d.InsumoId), d.CantidadContada!.Value) > umbral);
            if (grandes > 0 && !EsAdministrador)
            {
                throw GenericException.Prohibido(
                    $"Hay {grandes} insumo(s) con diferencias mayores al {umbral:0.##} %. Solo un Administrador puede aprobar este conteo.");
            }

            var cantidadAjustes = 0;
            foreach (var d in conteo.Detalles)
            {
                var sistema = actual.GetValueOrDefault(d.InsumoId);
                var ajuste = Math.Round(d.CantidadContada!.Value - sistema, 6);
                d.StockAlAprobar = sistema;
                d.AjusteAplicado = ajuste;
                if (ajuste == 0) continue;

                await inventario.AplicarAsync(d.InsumoId, conteo.AlmacenId,
                    ajuste > 0 ? TipoMovimiento.AjustePositivo : TipoMovimiento.AjusteNegativo,
                    Math.Abs(ajuste),
                    motivo: "Diferencia de conteo físico",
                    referencia: $"Conteo N° {conteo.Numero}",
                    conteoId: conteo.Id);
                cantidadAjustes++;
            }

            conteo.Estado = EstadoConteo.Aprobado;
            conteo.CerradoEn = Reloj.Ahora;
            conteo.CerradoPor = tenant.UsuarioNombre;
            await repo.GuardarAsync();
            return cantidadAjustes;
        });

        logger.LogInformation("Conteo {ConteoId} aprobado con {Ajustes} ajustes", id, ajustes);
        return new GenericResponse<ConteoDetalleCompletoResponse>(true, await CompletoAsync(id),
            ajustes == 0
                ? "Conteo aprobado. El stock coincidía: no hubo ajustes."
                : $"Conteo aprobado. Se registraron {ajustes} ajuste(s) en el kardex.");
    }

    public async Task<GenericResponse<ConteoDetalleCompletoResponse>> AnularAsync(int id)
    {
        var conteo = await ConteoAbiertoAsync(id);
        conteo.Estado = EstadoConteo.Anulado;
        conteo.CerradoEn = Reloj.Ahora;
        conteo.CerradoPor = tenant.UsuarioNombre;
        await repo.GuardarAsync();
        return new GenericResponse<ConteoDetalleCompletoResponse>(true, await CompletoAsync(id), "Conteo anulado. El stock no cambió.");
    }

    // ---------------------------------------------------------------- auxiliares

    /// <summary>% de diferencia respecto al sistema. Si el sistema tiene 0 y se contó algo, es 100 %.</summary>
    public static decimal PorcentajeDiferencia(decimal sistema, decimal contado)
    {
        if (sistema == 0) return contado == 0 ? 0 : 100;
        return Math.Abs(contado - sistema) / Math.Abs(sistema) * 100;
    }

    private async Task<ConteoFisico> ConteoAbiertoAsync(int id)
    {
        var conteo = await repo.ConteoConDetallesAsync(id) ?? throw GenericException.NoEncontrado("El conteo no existe.");
        if (conteo.Estado != EstadoConteo.Abierto)
            throw GenericException.Conflicto($"El conteo N° {conteo.Numero} ya está {EtiquetasInventario.De(conteo.Estado).ToLowerInvariant()}.");
        return conteo;
    }

    private static void Aplicar(ConteoFisico conteo, ConteoGuardarRequest request)
    {
        var porInsumo = conteo.Detalles.ToDictionary(d => d.InsumoId);
        foreach (var c in request.Cantidades)
        {
            if (!porInsumo.TryGetValue(c.InsumoId, out var detalle))
                throw new GenericException("Uno de los insumos no pertenece a este conteo. Recargue la página.");
            detalle.CantidadContada = c.CantidadContada is decimal v ? Math.Round(v, 6) : null;
        }
    }

    private async Task<ConteoDetalleCompletoResponse> CompletoAsync(int id)
    {
        var c = await repo.ConteoCompletoAsync(id) ?? throw GenericException.NoEncontrado("El conteo no existe.");
        c.UmbralAprobacion = await UmbralAsync();
        c.PuedeAprobarDiferenciasGrandes = EsAdministrador;
        return c;
    }
}

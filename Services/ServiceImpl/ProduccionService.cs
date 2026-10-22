using SPIn.Common;
using SPIn.Common.Excepciones;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;

namespace SPIn.Services.ServiceImpl;

/// <summary>
/// Plan y partes de producción.
/// Registrar un parte: valida línea, producto, formato y almacenes de la misma planta; explota la receta vigente en la fecha
/// (con merma); descuenta cada insumo («Salida a producción») y suma los paquetes al producto terminado
/// («Entrada de producción»), todo en una transacción. Si falta algún insumo no se registra nada.
/// Anular un parte devuelve los insumos y retira los paquetes (si todavía están en stock).
/// </summary>
public class ProduccionService(
    IProduccionOperacionRepository repo,
    IProductoRepository productos,
    IAlmacenRepository almacenes,
    IRecetaService recetas,
    IInventarioService inventario,
    IInventarioRepository inventarioRepo,
    IProductoTerminadoService productoTerminado,
    IPedidoRepository pedidos,
    ITenantProvider tenant,
    ILogger<ProduccionService> logger) : IProduccionService
{
    public async Task<GenericResponse<OpcionesProduccionResponse>> OpcionesAsync() => new(await repo.OpcionesAsync(Reloj.Hoy));

    // ================================================================ plan
    public async Task<GenericResponse<PaginaResponse<PlanResponse>>> PlanesAsync(FiltroPlanRequest filtro) =>
        new(await repo.PlanesAsync(filtro, Reloj.Hoy));

    public async Task<GenericResponse<PlanResponse>> CrearPlanAsync(PlanRequest r)
    {
        await ValidarPlanAsync(r);
        var plan = new PlanProduccion
        {
            Fecha = r.Fecha, LineaId = r.LineaId, ProductoId = r.ProductoId, Paquetes = r.Paquetes,
            Comentario = Validaciones.TextoOpcional(r.Comentario)
        };
        repo.AgregarPlan(plan);
        await repo.GuardarAsync();
        return new GenericResponse<PlanResponse>(true, (await repo.PlanResumenAsync(plan.Id, Reloj.Hoy))!, "Producción programada.");
    }

    public async Task<GenericResponse<PlanResponse>> ActualizarPlanAsync(int id, PlanRequest r)
    {
        var plan = await repo.PlanAsync(id) ?? throw GenericException.NoEncontrado("La programación no existe.");
        if (plan.Estado != EstadoPlan.Planificado) throw GenericException.Conflicto("Solo se edita una programación pendiente.");
        await ValidarPlanAsync(r, plan.Fecha);
        plan.Fecha = r.Fecha;
        plan.LineaId = r.LineaId;
        plan.ProductoId = r.ProductoId;
        plan.Paquetes = r.Paquetes;
        plan.Comentario = Validaciones.TextoOpcional(r.Comentario);
        await repo.GuardarAsync();
        return new GenericResponse<PlanResponse>(true, (await repo.PlanResumenAsync(id, Reloj.Hoy))!, "Programación actualizada.");
    }

    public async Task<GenericResponse<PlanResponse>> CancelarPlanAsync(int id)
    {
        var plan = await repo.PlanAsync(id) ?? throw GenericException.NoEncontrado("La programación no existe.");
        if (plan.Estado != EstadoPlan.Planificado) throw GenericException.Conflicto("Solo se cancela una programación pendiente.");
        plan.Estado = EstadoPlan.Cancelado;
        await repo.GuardarAsync();
        return new GenericResponse<PlanResponse>(true, (await repo.PlanResumenAsync(id, Reloj.Hoy))!, "Programación cancelada.");
    }

    private async Task ValidarPlanAsync(PlanRequest r, DateOnly? fechaAnterior = null)
    {
        var hoy = Reloj.Hoy;
        if (r.Fecha == default) throw GenericException.Campo("fecha", "Indique la fecha.");
        // Se programa de hoy en adelante (al editar se permite mantener la fecha original).
        if (r.Fecha < hoy && r.Fecha != fechaAnterior) throw GenericException.Campo("fecha", "Programe de hoy en adelante.");
        if (r.Fecha > hoy.AddDays(366)) throw GenericException.Campo("fecha", "Programe como máximo con un año de anticipación.");
        await ValidarLineaProductoAsync(r.LineaId, r.ProductoId);
    }

    private async Task<(LineaProduccion Linea, Producto Producto)> ValidarLineaProductoAsync(int lineaId, int productoId)
    {
        var linea = await repo.LineaConFormatosAsync(lineaId);
        if (linea is null || !linea.Activo) throw GenericException.Campo("lineaId", "Seleccione una línea activa.");
        var producto = await productos.ObtenerAsync(productoId);
        if (producto is null || !producto.Activo) throw GenericException.Campo("productoId", "Seleccione un producto activo.");
        if (!linea.Formatos.Any(f => f.FormatoId == producto.FormatoId))
            throw GenericException.Campo("productoId", $"La línea {linea.Codigo} no embotella el formato de este producto. Elija otra línea o agréguele el formato.");
        return (linea, producto);
    }

    // ================================================================ partes
    public async Task<GenericResponse<PaginaResponse<ParteResponse>>> PartesAsync(FiltroParteRequest filtro) =>
        new(await repo.PartesAsync(filtro));

    public async Task<GenericResponse<ParteResponse>> ParteAsync(int id) =>
        new(await repo.ParteResumenAsync(id) ?? throw GenericException.NoEncontrado("El parte no existe."));

    public async Task<GenericResponse<RequerimientoResponse>> RequerimientoAsync(RequerimientoRequest r)
    {
        var explosion = (await recetas.ExplotarAsync(r.ProductoId,
            new ExplosionRequest { Cantidad = r.Paquetes, Unidad = UnidadExplosion.Paquetes, Fecha = r.Fecha ?? Reloj.Hoy })).Data!;
        var stock = await repo.StockInsumosAsync(explosion.Insumos.Select(i => i.InsumoId), r.AlmacenInsumosId);
        var enCamino = await pedidos.EnCaminoPorInsumoAsync();
        return new GenericResponse<RequerimientoResponse>(new RequerimientoResponse
        {
            RecetaVersion = explosion.RecetaVersion,
            Paquetes = explosion.Paquetes,
            Botellas = explosion.Botellas,
            Lineas = explosion.Insumos.Select(i => new RequerimientoLineaResponse
            {
                InsumoId = i.InsumoId, InsumoCodigo = i.InsumoCodigo, InsumoNombre = i.InsumoNombre,
                UnidadConsumoCodigo = i.UnidadConsumoCodigo, Necesario = i.ConsumoTotal,
                Disponible = stock.GetValueOrDefault(i.InsumoId), EnCamino = enCamino.GetValueOrDefault(i.InsumoId)
            }).ToList()
        });
    }

    public async Task<GenericResponse<ParteResponse>> RegistrarParteAsync(ParteRequest r)
    {
        var hoy = Reloj.Hoy;
        var fecha = r.Fecha == default ? hoy : r.Fecha;
        if (fecha > hoy) throw GenericException.Campo("fecha", "El parte registra producción realizada: la fecha no puede ser futura.");
        if (fecha < hoy.AddDays(-60)) throw GenericException.Campo("fecha", "Solo se registran partes de los últimos 60 días.");

        var (linea, producto) = await ValidarLineaProductoAsync(r.LineaId, r.ProductoId);
        var almIns = await almacenes.ObtenerAsync(r.AlmacenInsumosId);
        if (almIns is null || !almIns.Activo || almIns.Tipo != TipoAlmacen.Insumos)
            throw GenericException.Campo("almacenInsumosId", "Elija un almacén de insumos activo.");
        var almPt = await almacenes.ObtenerAsync(r.AlmacenProductoId);
        if (almPt is null || !almPt.Activo || almPt.Tipo != TipoAlmacen.ProductoTerminado)
            throw GenericException.Campo("almacenProductoId", "Elija un almacén de producto terminado activo.");
        if (almIns.PlantaId != linea.PlantaId || almPt.PlantaId != linea.PlantaId)
            throw GenericException.Campo("almacenInsumosId", "Los almacenes deben ser de la misma planta que la línea.");

        var botellas = r.Paquetes * producto.UnidadesPorPaquete;
        if (r.HorasOperacion is decimal horas && linea.VelocidadNominal > 0 && botellas > linea.VelocidadNominal * horas * 1.1m)
        {
            throw GenericException.Campo("paquetes",
                $"{botellas:#,0} botellas superan la capacidad de {linea.Codigo} en {horas:0.#} h ({linea.VelocidadNominal * horas:#,0} botellas a velocidad nominal). Revise las horas o la cantidad.");
        }

        // Receta vigente en la fecha del parte (lanza error si no hay).
        var explosion = (await recetas.ExplotarAsync(producto.Id,
            new ExplosionRequest { Cantidad = r.Paquetes, Unidad = UnidadExplosion.Paquetes, Fecha = fecha })).Data!;

        var parte = await inventarioRepo.EnTransaccionAsync(async () =>
        {
            // Verificación previa: lista completa de lo que falta, en vez de fallar en el primer insumo.
            var stock = await repo.StockInsumosAsync(explosion.Insumos.Select(i => i.InsumoId), almIns.Id);
            var faltantes = explosion.Insumos
                .Where(i => i.ConsumoTotal > stock.GetValueOrDefault(i.InsumoId))
                .Select(i => $"{i.InsumoCodigo} (necesita {i.ConsumoTotal:#,0.####} {i.UnidadConsumoCodigo}, hay {stock.GetValueOrDefault(i.InsumoId):#,0.####})")
                .ToList();
            if (faltantes.Count > 0)
                throw GenericException.Campo("paquetes", $"Stock de insumos insuficiente en {almIns.Codigo}: {string.Join("; ", faltantes)}.");

            var plan = r.PlanId is int planId ? await repo.PlanAsync(planId) : await repo.PlanPendienteAsync(fecha, linea.Id, producto.Id);
            if (r.PlanId is not null && (plan is null || plan.Estado != EstadoPlan.Planificado))
                throw GenericException.Campo("planId", "La programación elegida ya no está pendiente.");

            var p = new ParteProduccion
            {
                Numero = await repo.SiguienteNumeroParteAsync(),
                Fecha = fecha, Turno = r.Turno, LineaId = linea.Id, ProductoId = producto.Id, Paquetes = r.Paquetes,
                HorasOperacion = r.HorasOperacion, AlmacenInsumosId = almIns.Id, AlmacenProductoId = almPt.Id,
                RecetaVersion = explosion.RecetaVersion, PlanId = plan?.Id,
                Observacion = Validaciones.TextoOpcional(r.Observacion),
                CreadoEn = Reloj.Ahora, CreadoPor = tenant.UsuarioNombre ?? "sistema"
            };
            repo.AgregarParte(p);
            await repo.GuardarAsync(); // Id del parte para enlazar los movimientos

            var referencia = $"Parte de producción N° {p.Numero}";
            foreach (var i in explosion.Insumos.Where(i => i.ConsumoTotal > 0))
            {
                var m = await inventario.AplicarAsync(i.InsumoId, almIns.Id, TipoMovimiento.SalidaProduccion, i.ConsumoTotal,
                    $"PP-{p.Numero}", null, referencia, parteProduccionId: p.Id);
                await repo.GuardarAsync();
                p.Consumos.Add(new ParteConsumo { InsumoId = i.InsumoId, Cantidad = m.Cantidad, MovimientoId = m.Id });
            }
            await productoTerminado.AplicarAsync(producto.Id, almPt.Id, TipoMovimientoProducto.EntradaProduccion, r.Paquetes,
                $"PP-{p.Numero}", null, referencia, parteProduccionId: p.Id);

            if (plan is not null)
            {
                plan.Estado = EstadoPlan.Ejecutado;
                plan.ParteId = p.Id;
            }
            await repo.GuardarAsync();
            return p;
        });

        logger.LogInformation("Parte de producción {Numero}: {Paquetes} paquetes de {ProductoId}", parte.Numero, r.Paquetes, producto.Id);
        return new GenericResponse<ParteResponse>(true, (await repo.ParteResumenAsync(parte.Id))!,
            $"Parte N° {parte.Numero} registrado: +{r.Paquetes:#,0.##} paquetes en {almPt.Codigo} y {explosion.Insumos.Count} insumos descontados de {almIns.Codigo}.");
    }

    public async Task<GenericResponse<ParteResponse>> AnularParteAsync(int id, MotivoRequest request)
    {
        var motivo = request.Motivo.Trim();
        var numero = await inventarioRepo.EnTransaccionAsync(async () =>
        {
            var parte = await repo.ParteConConsumosAsync(id) ?? throw GenericException.NoEncontrado("El parte no existe.");
            if (parte.Estado == EstadoParte.Anulado) throw GenericException.Conflicto("El parte ya está anulado.");
            var referencia = $"Anulación del parte N° {parte.Numero}";

            // Primero se retiran los paquetes: si ya se vendieron, no se puede anular.
            try
            {
                await productoTerminado.AplicarAsync(parte.ProductoId, parte.AlmacenProductoId, TipoMovimientoProducto.AjusteNegativo,
                    parte.Paquetes, $"PP-{parte.Numero}", motivo, referencia, parteProduccionId: parte.Id);
            }
            catch (GenericException)
            {
                throw GenericException.Conflicto("No se puede anular: parte de esos paquetes ya salió del almacén (ventas o ajustes). Registre un ajuste en su lugar.");
            }

            foreach (var c in parte.Consumos)
            {
                await inventario.AplicarAsync(c.InsumoId, parte.AlmacenInsumosId, TipoMovimiento.AjustePositivo, c.Cantidad,
                    $"PP-{parte.Numero}", motivo, referencia, revierteId: c.MovimientoId, parteProduccionId: parte.Id);
                var original = await inventarioRepo.MovimientoAsync(c.MovimientoId);
                if (original is not null) original.Revertido = true;
            }

            if (parte.PlanId is int planId && await repo.PlanAsync(planId) is { } plan && plan.Estado == EstadoPlan.Ejecutado)
            {
                plan.Estado = EstadoPlan.Planificado;
                plan.ParteId = null;
            }
            parte.Estado = EstadoParte.Anulado;
            parte.MotivoAnulacion = motivo;
            await repo.GuardarAsync();
            return parte.Numero;
        });

        logger.LogInformation("Parte de producción {Numero} anulado", numero);
        return new GenericResponse<ParteResponse>(true, (await repo.ParteResumenAsync(id))!,
            $"Parte N° {numero} anulado: los insumos volvieron al almacén y se retiraron los paquetes.");
    }
}

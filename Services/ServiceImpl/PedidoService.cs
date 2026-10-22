using SPIn.Common;
using SPIn.Common.Excepciones;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;

namespace SPIn.Services.ServiceImpl;

/// <summary>
/// Pedidos a proveedores («pedidos en camino») y su recepción.
/// - Solo se piden insumos que el proveedor tiene asignados (condiciones de compra).
/// - La recepción puede ser parcial; cada una genera «Entradas por compra» en el kardex (convertidas a unidad de consumo).
/// - No se recibe más de lo pendiente. Un error de recepción se corrige anulándola (si el stock lo permite).
/// </summary>
public class PedidoService(
    IPedidoRepository repo,
    IProveedorRepository proveedores,
    IAlmacenRepository almacenes,
    IInventarioService inventario,
    IInventarioRepository inventarioRepo,
    ITenantProvider tenant,
    ILogger<PedidoService> logger) : IPedidoService
{
    public async Task<GenericResponse<PedidosPaginaResponse>> ListarAsync(FiltroPedidoRequest filtro) =>
        new(await repo.ListarAsync(filtro, Reloj.Hoy));

    public async Task<GenericResponse<PedidoResponse>> ObtenerAsync(int id) => new(await DetalleAsync(id));

    public async Task<GenericResponse<List<CondicionProveedorResponse>>> CondicionesAsync(int proveedorId) =>
        new(await repo.CondicionesAsync(proveedorId));

    private async Task<PedidoResponse> DetalleAsync(int id) =>
        await repo.DetalleAsync(id, Reloj.Hoy) ?? throw GenericException.NoEncontrado("El pedido no existe.");

    // ---------------------------------------------------------------- crear / editar
    public async Task<GenericResponse<PedidoResponse>> CrearAsync(PedidoRequest r)
    {
        var fechaEstimada = await ValidarAsync(r);
        var pedido = await inventarioRepo.EnTransaccionAsync(async () =>
        {
            var p = new PedidoCompra
            {
                Numero = await repo.SiguienteNumeroAsync(),
                ProveedorId = r.ProveedorId,
                FechaPedido = r.FechaPedido == default ? Reloj.Hoy : r.FechaPedido,
                FechaEstimada = fechaEstimada,
                Moneda = r.Moneda,
                DocumentoProveedor = Validaciones.TextoOpcional(r.DocumentoProveedor),
                Observacion = Validaciones.TextoOpcional(r.Observacion),
                Detalles = r.Detalles.Select(d => new PedidoDetalle { InsumoId = d.InsumoId, Cantidad = d.Cantidad, PrecioUnitario = d.PrecioUnitario }).ToList()
            };
            repo.Agregar(p);
            await repo.GuardarAsync();
            return p;
        });
        logger.LogInformation("Pedido {Numero} creado", pedido.Numero);
        return new GenericResponse<PedidoResponse>(true, await DetalleAsync(pedido.Id),
            $"Pedido {pedido.Numero} registrado. Llegada estimada: {pedido.FechaEstimada:dd/MM/yyyy}.");
    }

    public async Task<GenericResponse<PedidoResponse>> ActualizarAsync(int id, PedidoRequest r)
    {
        var pedido = await repo.ConDetallesAsync(id) ?? throw GenericException.NoEncontrado("El pedido no existe.");
        if (pedido.Estado != EstadoPedido.Pendiente || pedido.Detalles.Any(d => d.CantidadRecibida > 0))
            throw GenericException.Conflicto("Solo se edita un pedido que aún no tiene recepciones.");
        var fechaEstimada = await ValidarAsync(r);

        repo.QuitarDetalles(pedido);
        pedido.Detalles = r.Detalles.Select(d => new PedidoDetalle { InsumoId = d.InsumoId, Cantidad = d.Cantidad, PrecioUnitario = d.PrecioUnitario }).ToList();
        pedido.ProveedorId = r.ProveedorId;
        pedido.FechaPedido = r.FechaPedido == default ? pedido.FechaPedido : r.FechaPedido;
        pedido.FechaEstimada = fechaEstimada;
        pedido.Moneda = r.Moneda;
        pedido.DocumentoProveedor = Validaciones.TextoOpcional(r.DocumentoProveedor);
        pedido.Observacion = Validaciones.TextoOpcional(r.Observacion);
        await repo.GuardarAsync();
        return new GenericResponse<PedidoResponse>(true, await DetalleAsync(id), $"Pedido {pedido.Numero} actualizado.");
    }

    /// <summary>Valida el pedido y devuelve la fecha estimada de llegada.</summary>
    private async Task<DateOnly> ValidarAsync(PedidoRequest r)
    {
        var hoy = Reloj.Hoy;
        var fechaPedido = r.FechaPedido == default ? hoy : r.FechaPedido;
        if (fechaPedido > hoy) throw GenericException.Campo("fechaPedido", "La fecha del pedido no puede ser futura.");
        if (fechaPedido < hoy.AddDays(-365)) throw GenericException.Campo("fechaPedido", "La fecha del pedido es demasiado antigua.");

        var proveedor = await proveedores.ObtenerAsync(r.ProveedorId);
        if (proveedor is null || !proveedor.Activo) throw GenericException.Campo("proveedorId", "Seleccione un proveedor activo.");

        var repetido = r.Detalles.GroupBy(d => d.InsumoId).FirstOrDefault(g => g.Count() > 1);
        if (repetido is not null)
            throw GenericException.Campo("detalles", $"Fila {r.Detalles.FindLastIndex(d => d.InsumoId == repetido.Key) + 1}: el insumo está repetido. Use una sola fila por insumo.");

        var condiciones = (await repo.CondicionesAsync(r.ProveedorId)).ToDictionary(c => c.InsumoId);
        for (var i = 0; i < r.Detalles.Count; i++)
        {
            if (!condiciones.ContainsKey(r.Detalles[i].InsumoId))
                throw GenericException.Campo("detalles", $"Fila {i + 1}: este proveedor no tiene asignado el insumo (o está inactivo). Asígnelo en Insumos → Proveedores.");
        }

        var entrega = r.Detalles.Max(d => condiciones[d.InsumoId].TiempoEntregaDias);
        var estimada = r.FechaEstimada ?? fechaPedido.AddDays(entrega);
        if (estimada < fechaPedido) throw GenericException.Campo("fechaEstimada", "La llegada estimada no puede ser anterior al pedido.");
        return estimada;
    }

    public async Task<GenericResponse<PedidoResponse>> CancelarAsync(int id, MotivoRequest request)
    {
        var pedido = await repo.ConDetallesAsync(id) ?? throw GenericException.NoEncontrado("El pedido no existe.");
        if (pedido.Estado is EstadoPedido.Recibido or EstadoPedido.Cancelado)
            throw GenericException.Conflicto("El pedido ya no está en camino.");
        pedido.Estado = EstadoPedido.Cancelado;
        pedido.MotivoCancelacion = request.Motivo.Trim();
        await repo.GuardarAsync();
        var parcial = pedido.Detalles.Any(d => d.CantidadRecibida > 0);
        return new GenericResponse<PedidoResponse>(true, await DetalleAsync(id),
            parcial ? $"Pedido {pedido.Numero} cancelado. Lo ya recibido se conserva; el saldo deja de figurar en camino."
                    : $"Pedido {pedido.Numero} cancelado.");
    }

    // ---------------------------------------------------------------- recepción
    public async Task<GenericResponse<PedidoResponse>> RecibirAsync(int id, RecepcionRequest r)
    {
        var almacen = await almacenes.ObtenerAsync(r.AlmacenId);
        if (almacen is null || !almacen.Activo) throw GenericException.Campo("almacenId", "Seleccione un almacén activo.");
        if (almacen.Tipo == TipoAlmacen.ProductoTerminado)
            throw GenericException.Campo("almacenId", "Los insumos se reciben en un almacén de insumos o de cuarentena.");

        var lineas = r.Detalles.Where(d => d.Cantidad > 0).ToList();
        if (lineas.Count == 0) throw GenericException.Campo("detalles", "Indique la cantidad recibida de al menos un insumo.");

        var numero = await inventarioRepo.EnTransaccionAsync(async () =>
        {
            var pedido = await repo.ConDetallesAsync(id) ?? throw GenericException.NoEncontrado("El pedido no existe.");
            if (pedido.Estado is not (EstadoPedido.Pendiente or EstadoPedido.Parcial))
                throw GenericException.Conflicto("El pedido ya no está en camino.");

            var recepcion = new Recepcion
            {
                PedidoId = pedido.Id, Fecha = Reloj.Ahora, AlmacenId = almacen.Id, Guia = r.Guia.Trim(),
                Observacion = Validaciones.TextoOpcional(r.Observacion), Usuario = tenant.UsuarioNombre ?? "sistema"
            };
            repo.AgregarRecepcion(recepcion);
            await repo.GuardarAsync();

            var referencia = $"Pedido {pedido.Numero}";
            foreach (var l in lineas)
            {
                var detalle = pedido.Detalles.FirstOrDefault(d => d.Id == l.PedidoDetalleId)
                    ?? throw GenericException.Campo("detalles", "Una de las líneas no pertenece a este pedido.");
                var pendiente = detalle.Cantidad - detalle.CantidadRecibida;
                if (l.Cantidad > pendiente + 0.0001m)
                    throw GenericException.Campo("detalles",
                        $"{detalle.Insumo!.Codigo}: se reciben {l.Cantidad:#,0.####} y solo quedan {pendiente:#,0.####} pendientes. Si llegó de más, registre la diferencia como ajuste.");

                var enConsumo = Math.Round(l.Cantidad * detalle.Insumo!.FactorConversion, 6);
                var m = await inventario.AplicarAsync(detalle.InsumoId, almacen.Id, TipoMovimiento.EntradaCompra, enConsumo,
                    recepcion.Guia, null, referencia, recepcionId: recepcion.Id);
                await repo.GuardarAsync();
                detalle.CantidadRecibida = Math.Round(detalle.CantidadRecibida + l.Cantidad, 6);
                recepcion.Detalles.Add(new RecepcionDetalle { PedidoDetalleId = detalle.Id, Cantidad = l.Cantidad, MovimientoId = m.Id });
            }

            pedido.Estado = pedido.Detalles.All(d => d.CantidadRecibida >= d.Cantidad - 0.0001m) ? EstadoPedido.Recibido : EstadoPedido.Parcial;
            await repo.GuardarAsync();
            return pedido.Numero;
        });

        logger.LogInformation("Recepción del pedido {Numero} en {Almacen}", numero, almacen.Codigo);
        var detalleResp = await DetalleAsync(id);
        return new GenericResponse<PedidoResponse>(true, detalleResp,
            detalleResp.Estado == EstadoPedido.Recibido
                ? $"Recepción registrada en {almacen.Codigo}. Pedido {numero} completo."
                : $"Recepción parcial registrada en {almacen.Codigo}. El saldo sigue en camino.");
    }

    public async Task<GenericResponse<PedidoResponse>> AnularRecepcionAsync(int recepcionId, MotivoRequest request)
    {
        var motivo = request.Motivo.Trim();
        var pedidoId = await inventarioRepo.EnTransaccionAsync(async () =>
        {
            var rec = await repo.RecepcionAsync(recepcionId) ?? throw GenericException.NoEncontrado("La recepción no existe.");
            if (rec.Anulada) throw GenericException.Conflicto("La recepción ya está anulada.");
            var pedido = rec.Pedido!;
            if (pedido.Estado == EstadoPedido.Cancelado) throw GenericException.Conflicto("El pedido está cancelado: corrija con un ajuste de stock.");

            foreach (var d in rec.Detalles)
            {
                var insumo = d.PedidoDetalle!.Insumo!;
                var enConsumo = Math.Round(d.Cantidad * insumo.FactorConversion, 6);
                try
                {
                    await inventario.AplicarAsync(insumo.Id, rec.AlmacenId, TipoMovimiento.AjusteNegativo, enConsumo,
                        rec.Guia, motivo, $"Anulación de recepción del pedido {pedido.Numero}", revierteId: d.MovimientoId, recepcionId: rec.Id);
                }
                catch (GenericException)
                {
                    throw GenericException.Conflicto($"No se puede anular: {insumo.Codigo} ya se usó y no queda stock suficiente en el almacén. Corrija con un ajuste.");
                }
                var original = await inventarioRepo.MovimientoAsync(d.MovimientoId);
                if (original is not null) original.Revertido = true;
                var detalle = pedido.Detalles.First(x => x.Id == d.PedidoDetalleId);
                detalle.CantidadRecibida = Math.Max(0, Math.Round(detalle.CantidadRecibida - d.Cantidad, 6));
            }

            rec.Anulada = true;
            rec.MotivoAnulacion = motivo;
            pedido.Estado = pedido.Detalles.All(x => x.CantidadRecibida <= 0) ? EstadoPedido.Pendiente : EstadoPedido.Parcial;
            await repo.GuardarAsync();
            return pedido.Id;
        });

        return new GenericResponse<PedidoResponse>(true, await DetalleAsync(pedidoId),
            "Recepción anulada: se retiró del stock y el pedido vuelve a figurar en camino.");
    }
}

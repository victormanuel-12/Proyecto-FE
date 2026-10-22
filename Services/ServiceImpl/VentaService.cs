using SPIn.Common;
using SPIn.Common.Excepciones;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;

namespace SPIn.Services.ServiceImpl;

/// <summary>
/// Ventas y despachos: historial de demanda por producto, día y canal.
/// - Con «descontar stock», la venta sale del almacén de producto terminado (no puede quedar negativo).
/// - Sin él, es solo historial (por ejemplo, ventas pasadas importadas).
/// - No se borran: se anulan con motivo, y si descontaron stock se devuelve.
/// </summary>
public class VentaService(
    IVentaRepository repo,
    IProductoRepository productos,
    IAlmacenRepository almacenes,
    IProductoTerminadoService productoTerminado,
    ITenantProvider tenant,
    ILogger<VentaService> logger) : IVentaService
{
    public async Task<GenericResponse<VentasPaginaResponse>> ListarAsync(FiltroVentaRequest filtro) =>
        new(await repo.ListarAsync(filtro));

    public async Task<GenericResponse<VentasSemanalesResponse>> SemanalAsync(int? productoId, int semanas)
    {
        semanas = Math.Clamp(semanas, 4, 156);
        string titulo = "Todos los productos";
        if (productoId is int id)
        {
            var producto = await productos.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("El producto no existe.");
            titulo = $"{producto.Codigo} · {producto.Nombre}";
        }

        var hoy = Reloj.Hoy;
        var lunesActual = hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7));
        var desde = lunesActual.AddDays(-7 * (semanas - 1));
        var diarias = await repo.VentasDiariasAsync(productoId, desde, hoy);

        var porSemana = diarias
            .GroupBy(d => d.Fecha.AddDays(-(((int)d.Fecha.DayOfWeek + 6) % 7)))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Paquetes));

        var lista = Enumerable.Range(0, semanas)
            .Select(i => desde.AddDays(7 * i))
            .Select(lunes => new VentaSemanaResponse { Semana = lunes, Paquetes = porSemana.GetValueOrDefault(lunes) })
            .ToList();

        var primera = await repo.PrimeraVentaAsync(productoId);
        return new GenericResponse<VentasSemanalesResponse>(new VentasSemanalesResponse
        {
            ProductoId = productoId,
            Titulo = titulo,
            Semanas = lista,
            PrimeraVenta = primera,
            SemanasConHistorial = primera is DateOnly p ? (hoy.DayNumber - p.DayNumber) / 7 + 1 : 0
        });
    }

    public async Task<GenericResponse<VentaResponse>> RegistrarAsync(VentaRequest r)
    {
        var hoy = Reloj.Hoy;
        var fecha = r.Fecha == default ? hoy : r.Fecha;
        if (fecha > hoy) throw GenericException.Campo("fecha", "La fecha no puede ser futura.");
        if (fecha < hoy.AddYears(-10)) throw GenericException.Campo("fecha", "La fecha es demasiado antigua.");

        var producto = await productos.ObtenerAsync(r.ProductoId);
        if (producto is null || !producto.Activo) throw GenericException.Campo("productoId", "Seleccione un producto activo.");

        var paquetes = Math.Round(r.EnBotellas ? r.Cantidad / producto.UnidadesPorPaquete : r.Cantidad, 4);
        if (paquetes <= 0) throw GenericException.Campo("cantidad", "La cantidad debe ser mayor que 0.");

        Almacen? almacen = null;
        if (r.AfectaStock)
        {
            if (r.AlmacenId is not int almacenId) throw GenericException.Campo("almacenId", "Seleccione el almacén de producto terminado.");
            almacen = await almacenes.ObtenerAsync(almacenId);
            if (almacen is null || !almacen.Activo) throw GenericException.Campo("almacenId", "Seleccione un almacén activo.");
            if (almacen.Tipo != TipoAlmacen.ProductoTerminado)
                throw GenericException.Campo("almacenId", "Elija un almacén de producto terminado.");
        }

        var venta = await repo.EnTransaccionAsync(async () =>
        {
            var v = new Venta
            {
                Fecha = fecha,
                ProductoId = r.ProductoId,
                Canal = r.Canal,
                Paquetes = paquetes,
                Documento = Validaciones.TextoOpcional(r.Documento),
                Origen = OrigenVenta.Manual,
                AfectaStock = r.AfectaStock,
                AlmacenId = almacen?.Id,
                CreadoEn = Reloj.Ahora,
                CreadoPor = tenant.UsuarioNombre ?? "sistema"
            };
            repo.Agregar(v);
            await repo.GuardarAsync(); // para tener el Id de la venta

            if (r.AfectaStock)
            {
                await productoTerminado.AplicarAsync(r.ProductoId, almacen!.Id, TipoMovimientoProducto.SalidaVenta, paquetes,
                    v.Documento, referencia: $"Venta N° {v.Id}", ventaId: v.Id);
                await repo.GuardarAsync();
            }
            return v;
        });

        logger.LogInformation("Venta {VentaId} de {Paquetes} paquetes del producto {ProductoId}", venta.Id, paquetes, r.ProductoId);
        var mensaje = r.AfectaStock
            ? $"Venta registrada y descontada de {almacen!.Codigo}."
            : "Venta registrada como historial (no descuenta stock).";
        return new GenericResponse<VentaResponse>(true, (await repo.ResumenAsync(venta.Id))!, mensaje);
    }

    public async Task<GenericResponse<VentaResponse>> AnularAsync(long id, AnularVentaRequest request)
    {
        await repo.EnTransaccionAsync(async () =>
        {
            var venta = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("La venta no existe.");
            if (venta.Anulada) throw GenericException.Conflicto("La venta ya está anulada.");

            if (venta.AfectaStock)
            {
                var salida = await repo.MovimientoDeVentaAsync(id);
                if (salida is not null && !salida.Revertido)
                {
                    await productoTerminado.AplicarAsync(salida.ProductoId, salida.AlmacenId, TipoMovimientoProducto.AjustePositivo,
                        salida.Paquetes, salida.Documento, request.Motivo.Trim(), $"Anulación de la venta N° {id}", id, salida.Id);
                    salida.Revertido = true;
                }
            }

            venta.Anulada = true;
            venta.MotivoAnulacion = request.Motivo.Trim();
            await repo.GuardarAsync();
            return true;
        });

        logger.LogInformation("Venta {VentaId} anulada", id);
        var v = (await repo.ResumenAsync(id))!;
        return new GenericResponse<VentaResponse>(true, v,
            v.AfectaStock ? "Venta anulada. Los paquetes volvieron al stock." : "Venta anulada. Ya no cuenta en el historial de demanda.");
    }
}

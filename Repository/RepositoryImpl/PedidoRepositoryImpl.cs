using Microsoft.EntityFrameworkCore;
using SPIn.Data;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;

namespace SPIn.Repository.RepositoryImpl;

public class PedidoRepositoryImpl(ApplicationDbContext db) : IPedidoRepository
{
    private static readonly EstadoPedido[] EnCamino = [EstadoPedido.Pendiente, EstadoPedido.Parcial];

    public async Task<PedidosPaginaResponse> ListarAsync(FiltroPedidoRequest f, DateOnly hoy)
    {
        var q = db.Pedidos.AsNoTracking().AsQueryable();
        var texto = ConsultaExtensions.TextoBusqueda(f.Buscar);
        if (texto is not null)
            q = q.Where(p => p.Numero.ToLower().Contains(texto) || p.Proveedor!.RazonSocial.ToLower().Contains(texto)
                             || (p.Proveedor.NombreComercial != null && p.Proveedor.NombreComercial.ToLower().Contains(texto))
                             || (p.DocumentoProveedor != null && p.DocumentoProveedor.ToLower().Contains(texto)));
        if (f.ProveedorId is int prov) q = q.Where(p => p.ProveedorId == prov);
        if (f.InsumoId is int ins) q = q.Where(p => p.Detalles.Any(d => d.InsumoId == ins));

        // Resumen con los filtros de texto/proveedor, antes del filtro de estado.
        var enCamino = q.Where(p => EnCamino.Contains(p.Estado));
        var resumen = new ResumenPedidosResponse
        {
            EnCamino = await enCamino.CountAsync(),
            Atrasados = await enCamino.CountAsync(p => p.FechaEstimada < hoy),
            LleganEn7Dias = await enCamino.CountAsync(p => p.FechaEstimada >= hoy && p.FechaEstimada <= hoy.AddDays(7))
        };

        q = f.Estado switch
        {
            "encamino" => enCamino.OrderBy(p => p.FechaEstimada).ThenBy(p => p.Id),
            "atrasados" => enCamino.Where(p => p.FechaEstimada < hoy).OrderBy(p => p.FechaEstimada).ThenBy(p => p.Id),
            "recibidos" => q.Where(p => p.Estado == EstadoPedido.Recibido).OrderByDescending(p => p.Id),
            "cancelados" => q.Where(p => p.Estado == EstadoPedido.Cancelado).OrderByDescending(p => p.Id),
            _ => q.OrderByDescending(p => p.Id)
        };

        var total = await q.CountAsync();
        var paginas = Math.Max(1, (int)Math.Ceiling(total / (double)f.TamanoPagina));
        var pagina = Math.Min(Math.Max(1, f.Pagina), paginas);
        var lista = await Incluir(q).Skip((pagina - 1) * f.TamanoPagina).Take(f.TamanoPagina).ToListAsync();

        return new PedidosPaginaResponse
        {
            Items = lista.Select(p => Mapear(p, hoy)).ToList(),
            Total = total,
            Pagina = pagina,
            TamanoPagina = f.TamanoPagina,
            Resumen = resumen
        };
    }

    private static IQueryable<PedidoCompra> Incluir(IQueryable<PedidoCompra> q) => q
        .Include(p => p.Proveedor)
        .Include(p => p.Detalles).ThenInclude(d => d.Insumo!).ThenInclude(i => i.UnidadCompra)
        .Include(p => p.Detalles).ThenInclude(d => d.Insumo!).ThenInclude(i => i.UnidadConsumo)
        .AsSplitQuery();

    private static PedidoResponse Mapear(PedidoCompra p, DateOnly hoy)
    {
        var detalles = p.Detalles.OrderBy(d => d.Insumo!.Codigo).Select(d => new PedidoDetalleResponse
        {
            Id = d.Id, InsumoId = d.InsumoId, InsumoCodigo = d.Insumo!.Codigo, InsumoNombre = d.Insumo.Nombre,
            UnidadCompraCodigo = d.Insumo.UnidadCompra?.Codigo ?? string.Empty,
            UnidadConsumoCodigo = d.Insumo.UnidadConsumo?.Codigo ?? string.Empty,
            FactorConversion = d.Insumo.FactorConversion,
            Cantidad = d.Cantidad, Recibida = d.CantidadRecibida, PrecioUnitario = d.PrecioUnitario
        }).ToList();
        var total = detalles.Sum(d => d.Subtotal);
        var recibido = detalles.Sum(d => Math.Min(d.Recibida, d.Cantidad) * d.PrecioUnitario);
        var avance = total > 0
            ? recibido / total * 100
            : detalles.Count == 0 ? 0 : detalles.Average(d => d.Cantidad > 0 ? Math.Min(1, d.Recibida / d.Cantidad) : 0) * 100;
        var enCamino = p.Estado is EstadoPedido.Pendiente or EstadoPedido.Parcial;

        return new PedidoResponse
        {
            Id = p.Id, Numero = p.Numero, ProveedorId = p.ProveedorId,
            ProveedorNombre = p.Proveedor?.NombreComercial ?? p.Proveedor?.RazonSocial ?? string.Empty,
            FechaPedido = p.FechaPedido, FechaEstimada = p.FechaEstimada,
            DiasAtraso = enCamino && p.FechaEstimada < hoy ? hoy.DayNumber - p.FechaEstimada.DayNumber : 0,
            Moneda = p.Moneda, Simbolo = EtiquetasCatalogo.Simbolo(p.Moneda),
            Estado = p.Estado, EstadoNombre = EtiquetasOperacion.De(p.Estado),
            Total = Math.Round(total, 2), Avance = Math.Round(avance, 1),
            DocumentoProveedor = p.DocumentoProveedor, Observacion = p.Observacion, MotivoCancelacion = p.MotivoCancelacion,
            Editable = p.Estado == EstadoPedido.Pendiente && detalles.All(d => d.Recibida == 0),
            CreadoPor = p.CreadoPor,
            Detalles = detalles
        };
    }

    public async Task<PedidoResponse?> DetalleAsync(int id, DateOnly hoy)
    {
        var p = await Incluir(db.Pedidos.AsNoTracking().Where(x => x.Id == id)).FirstOrDefaultAsync();
        if (p is null) return null;
        var r = Mapear(p, hoy);
        var recepciones = await db.Recepciones.AsNoTracking().Where(x => x.PedidoId == id)
            .Include(x => x.Almacen)
            .Include(x => x.Detalles).ThenInclude(d => d.PedidoDetalle!).ThenInclude(d => d.Insumo!).ThenInclude(i => i.UnidadCompra)
            .AsSplitQuery()
            .OrderByDescending(x => x.Fecha).ToListAsync();
        r.Recepciones = recepciones.Select(x => new RecepcionResponse
        {
            Id = x.Id, Fecha = x.Fecha, AlmacenCodigo = x.Almacen?.Codigo ?? string.Empty, Guia = x.Guia, Observacion = x.Observacion,
            Usuario = x.Usuario, Anulada = x.Anulada, MotivoAnulacion = x.MotivoAnulacion,
            Lineas = x.Detalles.Select(d => new RecepcionLineaResponse
            {
                InsumoCodigo = d.PedidoDetalle!.Insumo!.Codigo, InsumoNombre = d.PedidoDetalle.Insumo.Nombre,
                UnidadCompraCodigo = d.PedidoDetalle.Insumo.UnidadCompra?.Codigo ?? string.Empty, Cantidad = d.Cantidad
            }).ToList()
        }).ToList();
        return r;
    }

    public Task<PedidoCompra?> ConDetallesAsync(int id) =>
        db.Pedidos.Include(p => p.Detalles).ThenInclude(d => d.Insumo).Include(p => p.Proveedor)
            .AsSplitQuery().FirstOrDefaultAsync(p => p.Id == id);

    public Task<Recepcion?> RecepcionAsync(int id) =>
        db.Recepciones.Include(r => r.Detalles).ThenInclude(d => d.PedidoDetalle!).ThenInclude(d => d.Insumo)
            .Include(r => r.Pedido!).ThenInclude(p => p.Detalles)
            .AsSplitQuery().FirstOrDefaultAsync(r => r.Id == id);

    public Task<List<CondicionProveedorResponse>> CondicionesAsync(int proveedorId) =>
        db.ProveedorInsumos.AsNoTracking()
            .Where(c => c.ProveedorId == proveedorId && c.Insumo!.Activo)
            .OrderBy(c => c.Insumo!.Codigo)
            .Select(c => new CondicionProveedorResponse
            {
                InsumoId = c.InsumoId, Codigo = c.Insumo!.Codigo, Nombre = c.Insumo.Nombre,
                UnidadCompraCodigo = c.Insumo.UnidadCompra!.Codigo, UnidadConsumoCodigo = c.Insumo.UnidadConsumo!.Codigo,
                FactorConversion = c.Insumo.FactorConversion, Moneda = c.Moneda, PrecioUnitario = c.PrecioUnitario,
                TiempoEntregaDias = c.TiempoEntregaDias, PedidoMinimo = c.PedidoMinimo, MultiploCompra = c.MultiploCompra,
                EsPrincipal = c.EsPrincipal
            }).ToListAsync();

    public async Task<Dictionary<int, decimal>> EnCaminoPorInsumoAsync()
    {
        var filas = await db.PedidoDetalles.AsNoTracking()
            .Where(d => EnCamino.Contains(d.Pedido!.Estado))
            .Select(d => new { d.InsumoId, d.Cantidad, d.CantidadRecibida, d.Insumo!.FactorConversion })
            .ToListAsync();
        return filas.GroupBy(f => f.InsumoId)
            .ToDictionary(g => g.Key, g => Math.Round(g.Sum(f => Math.Max(0, f.Cantidad - f.CantidadRecibida) * f.FactorConversion), 4));
    }

    public async Task<string> SiguienteNumeroAsync()
    {
        var cantidad = await db.Pedidos.CountAsync();
        string numero;
        do numero = $"OC-{++cantidad:000000}";
        while (await db.Pedidos.AnyAsync(p => p.Numero == numero));
        return numero;
    }

    public Task<bool> ProveedorUsadoAsync(int proveedorId) => db.Pedidos.AnyAsync(p => p.ProveedorId == proveedorId);
    public Task<bool> InsumoUsadoAsync(int insumoId) => db.PedidoDetalles.AnyAsync(d => d.InsumoId == insumoId);
    public void Agregar(PedidoCompra pedido) => db.Pedidos.Add(pedido);
    public void QuitarDetalles(PedidoCompra pedido) => db.PedidoDetalles.RemoveRange(pedido.Detalles);
    public void AgregarRecepcion(Recepcion recepcion) => db.Recepciones.Add(recepcion);
    public Task GuardarAsync() => db.SaveChangesAsync();
}

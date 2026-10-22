using SPIn.Models;

namespace SPIn.Dtos.Response;

// ------------------------------------------------------------------ producción

public class LineaOpcionResponse
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int PlantaId { get; set; }
    public string PlantaNombre { get; set; } = string.Empty;
    public int VelocidadNominal { get; set; }
    public List<int> FormatoIds { get; set; } = [];
}

public class AlmacenOpcionResponse
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int PlantaId { get; set; }
    public TipoAlmacen Tipo { get; set; }
}

public class ProductoProduccionOpcion
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int FormatoId { get; set; }
    public string FormatoNombre { get; set; } = string.Empty;
    public int UnidadesPorPaquete { get; set; }
    public int PaquetesPorPallet { get; set; }
    public bool TieneReceta { get; set; }
}

public class OpcionesProduccionResponse
{
    public List<LineaOpcionResponse> Lineas { get; set; } = [];
    public List<AlmacenOpcionResponse> Almacenes { get; set; } = [];
    public List<ProductoProduccionOpcion> Productos { get; set; } = [];
}

public class PlanResponse
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    public int LineaId { get; set; }
    public string LineaCodigo { get; set; } = string.Empty;
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public int UnidadesPorPaquete { get; set; }
    public decimal Paquetes { get; set; }
    public decimal Botellas => Paquetes * UnidadesPorPaquete;
    public int VelocidadNominal { get; set; }
    /// <summary>Horas de línea a velocidad nominal.</summary>
    public decimal? HorasEstimadas { get; set; }
    public EstadoPlan Estado { get; set; }
    public string EstadoNombre { get; set; } = string.Empty;
    public string? Comentario { get; set; }
    public int? ParteId { get; set; }
    public int? ParteNumero { get; set; }
    public bool Vencido { get; set; }
}

public class ParteConsumoResponse
{
    public int InsumoId { get; set; }
    public string InsumoCodigo { get; set; } = string.Empty;
    public string InsumoNombre { get; set; } = string.Empty;
    public string UnidadConsumoCodigo { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
}

public class ParteResponse
{
    public int Id { get; set; }
    public int Numero { get; set; }
    public DateOnly Fecha { get; set; }
    public Turno Turno { get; set; }
    public string TurnoNombre { get; set; } = string.Empty;
    public int LineaId { get; set; }
    public string LineaCodigo { get; set; } = string.Empty;
    public int VelocidadNominal { get; set; }
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public int UnidadesPorPaquete { get; set; }
    public decimal Paquetes { get; set; }
    public decimal Botellas => Paquetes * UnidadesPorPaquete;
    public decimal? HorasOperacion { get; set; }
    /// <summary>Botellas producidas / (velocidad nominal × horas), en %.</summary>
    public decimal? Eficiencia => HorasOperacion is > 0 && VelocidadNominal > 0
        ? Math.Round(Botellas / (VelocidadNominal * HorasOperacion.Value) * 100, 1) : null;
    public string AlmacenInsumosCodigo { get; set; } = string.Empty;
    public string AlmacenProductoCodigo { get; set; } = string.Empty;
    public int RecetaVersion { get; set; }
    public int? PlanId { get; set; }
    public EstadoParte Estado { get; set; }
    public string EstadoNombre { get; set; } = string.Empty;
    public string? Observacion { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime CreadoEn { get; set; }
    public string CreadoPor { get; set; } = string.Empty;
    public List<ParteConsumoResponse> Consumos { get; set; } = [];
}

public class RequerimientoLineaResponse
{
    public int InsumoId { get; set; }
    public string InsumoCodigo { get; set; } = string.Empty;
    public string InsumoNombre { get; set; } = string.Empty;
    public string UnidadConsumoCodigo { get; set; } = string.Empty;
    public decimal Necesario { get; set; }
    public decimal Disponible { get; set; }
    public decimal Falta => Math.Max(0, Math.Round(Necesario - Disponible, 4));
    /// <summary>En camino (pedidos pendientes), en unidad de consumo.</summary>
    public decimal EnCamino { get; set; }
}

public class RequerimientoResponse
{
    public int RecetaVersion { get; set; }
    public decimal Paquetes { get; set; }
    public decimal Botellas { get; set; }
    public List<RequerimientoLineaResponse> Lineas { get; set; } = [];
    public bool Alcanza => Lineas.All(l => l.Falta <= 0);
}

// ------------------------------------------------------------------ pedidos

public class CondicionProveedorResponse
{
    public int InsumoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string UnidadCompraCodigo { get; set; } = string.Empty;
    public string UnidadConsumoCodigo { get; set; } = string.Empty;
    public decimal FactorConversion { get; set; }
    public Moneda Moneda { get; set; }
    public decimal PrecioUnitario { get; set; }
    public int TiempoEntregaDias { get; set; }
    public decimal PedidoMinimo { get; set; }
    public decimal MultiploCompra { get; set; }
    public bool EsPrincipal { get; set; }
}

public class PedidoDetalleResponse
{
    public int Id { get; set; }
    public int InsumoId { get; set; }
    public string InsumoCodigo { get; set; } = string.Empty;
    public string InsumoNombre { get; set; } = string.Empty;
    public string UnidadCompraCodigo { get; set; } = string.Empty;
    public string UnidadConsumoCodigo { get; set; } = string.Empty;
    public decimal FactorConversion { get; set; }
    public decimal Cantidad { get; set; }
    public decimal Recibida { get; set; }
    public decimal Pendiente => Math.Max(0, Cantidad - Recibida);
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => Math.Round(Cantidad * PrecioUnitario, 2);
}

public class RecepcionLineaResponse
{
    public string InsumoCodigo { get; set; } = string.Empty;
    public string InsumoNombre { get; set; } = string.Empty;
    public string UnidadCompraCodigo { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
}

public class RecepcionResponse
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string AlmacenCodigo { get; set; } = string.Empty;
    public string Guia { get; set; } = string.Empty;
    public string? Observacion { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public bool Anulada { get; set; }
    public string? MotivoAnulacion { get; set; }
    public List<RecepcionLineaResponse> Lineas { get; set; } = [];
}

public class PedidoResponse
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public DateOnly FechaPedido { get; set; }
    public DateOnly FechaEstimada { get; set; }
    /// <summary>Días de atraso respecto a la fecha estimada (solo si sigue en camino).</summary>
    public int DiasAtraso { get; set; }
    public Moneda Moneda { get; set; }
    public string Simbolo { get; set; } = string.Empty;
    public EstadoPedido Estado { get; set; }
    public string EstadoNombre { get; set; } = string.Empty;
    public decimal Total { get; set; }
    /// <summary>% recibido (por valor del pedido).</summary>
    public decimal Avance { get; set; }
    public string? DocumentoProveedor { get; set; }
    public string? Observacion { get; set; }
    public string? MotivoCancelacion { get; set; }
    /// <summary>Se puede editar: está pendiente y no tiene recepciones.</summary>
    public bool Editable { get; set; }
    public bool EnCamino => Estado is EstadoPedido.Pendiente or EstadoPedido.Parcial;
    public string? CreadoPor { get; set; }
    public List<PedidoDetalleResponse> Detalles { get; set; } = [];
    public List<RecepcionResponse> Recepciones { get; set; } = [];
}

public class ResumenPedidosResponse
{
    public int EnCamino { get; set; }
    public int Atrasados { get; set; }
    public int LleganEn7Dias { get; set; }
}

public class PedidosPaginaResponse : PaginaResponse<PedidoResponse>
{
    public ResumenPedidosResponse Resumen { get; set; } = new();
}

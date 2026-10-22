using SPIn.Models;

namespace SPIn.Dtos.Response;

public class ConfiguracionResponse
{
    public int SemanasHorizonte { get; set; }
    public int SemanasValidacion { get; set; }
    public int SemanasMinimas { get; set; }
    public FrecuenciaReentrenamiento Frecuencia { get; set; }
    public int HoraReentrenamiento { get; set; }
    public int DiasRevision { get; set; }
    public int DiasEntregaPorDefecto { get; set; }
    public decimal UmbralConteo { get; set; }
    public DateTime? ProximoEntrenamiento { get; set; }
    public DateTime? ModificadoEn { get; set; }
    public string? ModificadoPor { get; set; }
}

public class EjecucionResponse
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public OrigenEjecucion Origen { get; set; }
    public string OrigenNombre { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public int DuracionMs { get; set; }
    public bool Exitosa { get; set; }
    public string? Mensaje { get; set; }
    public int Productos { get; set; }
    public decimal? MapePromedio { get; set; }
    public DateOnly UltimaSemanaHistorial { get; set; }
    public int SemanasHorizonte { get; set; }
}

public class PronosticoProductoResponse
{
    public int ProductoId { get; set; }
    /// <summary>Clave de fila para el listado.</summary>
    public int Id => ProductoId;
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public MetodoPronostico Metodo { get; set; }
    public string MetodoNombre { get; set; } = string.Empty;
    public decimal? Mape { get; set; }
    /// <summary>"buena" (&lt; 15 %), "aceptable" (&lt; 30 %), "baja" o "sin" (no medido).</summary>
    public string Calidad { get; set; } = "sin";
    public int SemanasHistorial { get; set; }
    public decimal PromedioReciente { get; set; }
    /// <summary>Paquetes pronosticados para las próximas 4 semanas.</summary>
    public decimal Proximas4 { get; set; }
    /// <summary>% de cambio de las próximas 4 semanas frente a las últimas 4 reales (aprox. promedio reciente × 4).</summary>
    public decimal? Variacion { get; set; }
    public string? Nota { get; set; }
}

public class MapeMetodoResponse
{
    public MetodoPronostico Metodo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal? Mape { get; set; }
    public bool Elegido { get; set; }
}

public class PuntoSemanaResponse
{
    public DateOnly Semana { get; set; }
    public decimal? Real { get; set; }
    public decimal? Pronostico { get; set; }
    public decimal? Minimo { get; set; }
    public decimal? Maximo { get; set; }
}

public class DetallePronosticoResponse : PronosticoProductoResponse
{
    public List<MapeMetodoResponse> Metodos { get; set; } = [];
    /// <summary>Historia real (últimas 52 semanas), validación (real vs pronóstico) y pronóstico futuro.</summary>
    public List<PuntoSemanaResponse> Historial { get; set; } = [];
    public List<PuntoSemanaResponse> Validacion { get; set; } = [];
    public List<PuntoSemanaResponse> Pronostico { get; set; } = [];
}

public class PanelPronosticoResponse
{
    public EjecucionResponse? Ultima { get; set; }
    public DateTime? ProximoEntrenamiento { get; set; }
    public FrecuenciaReentrenamiento Frecuencia { get; set; }
    public bool EnCurso { get; set; }
    public List<PronosticoProductoResponse> Productos { get; set; } = [];
}

public class ConsumoProductoResponse
{
    public string Codigo { get; set; } = string.Empty;
    public decimal PaquetesDiarios { get; set; }
    public decimal ConsumoPorPaquete { get; set; }
    public decimal ConsumoDiario { get; set; }
}

public class ReposicionInsumoResponse
{
    public int InsumoId { get; set; }
    public int Id => InsumoId;
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public string UnidadConsumoCodigo { get; set; } = string.Empty;
    public string UnidadCompraCodigo { get; set; } = string.Empty;
    public decimal FactorConversion { get; set; }
    public bool EsCritico { get; set; }

    public decimal Stock { get; set; }
    public decimal EnCamino { get; set; }
    public decimal Posicion => Stock + EnCamino;

    /// <summary>Consumo diario esperado (unidad de consumo) y su desviación.</summary>
    public decimal ConsumoDiario { get; set; }
    public decimal DesviacionDiaria { get; set; }
    public string FuenteDemanda { get; set; } = string.Empty;
    public List<ConsumoProductoResponse> Productos { get; set; } = [];

    public decimal? DiasCobertura { get; set; }
    public DateOnly? FechaAgotamiento { get; set; }

    public int TiempoEntrega { get; set; }
    public int VariacionEntrega { get; set; }
    public decimal NivelServicio { get; set; }
    public decimal Z { get; set; }
    public decimal StockSeguridad { get; set; }
    public decimal PuntoReorden { get; set; }
    public decimal StockMinimoActual { get; set; }
    public decimal StockMaximo { get; set; }
    public DateOnly? FechaLimitePedido { get; set; }

    /// <summary>Cantidad sugerida a pedir en unidad de compra (ya ajustada a mínimo y múltiplo) y su equivalente en consumo.</summary>
    public decimal CantidadSugerida { get; set; }
    public decimal CantidadSugeridaConsumo { get; set; }
    public decimal? CostoEstimado { get; set; }

    /// <summary>"urgente", "pedir", "ok", "sin-demanda" o "sin-proveedor".</summary>
    public string Estado { get; set; } = "ok";
    public string EstadoTexto { get; set; } = string.Empty;

    public int? ProveedorId { get; set; }
    public string? ProveedorNombre { get; set; }
    public decimal PedidoMinimo { get; set; }
    public decimal MultiploCompra { get; set; }
    public decimal PrecioUnitario { get; set; }
    public string? Simbolo { get; set; }
}

public class ReposicionPaginaResponse : PaginaResponse<ReposicionInsumoResponse>
{
    public int Urgentes { get; set; }
    public int PorPedir { get; set; }
    public int Ok { get; set; }
    public int SinDatos { get; set; }
    public DateTime? PronosticoDel { get; set; }
    public int DiasRevision { get; set; }
}

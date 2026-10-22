using SPIn.Models;

namespace SPIn.Dtos.Response;

public class AlertaResponse
{
    public int Id { get; set; }
    public TipoAlerta Tipo { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public SeveridadAlerta Severidad { get; set; }
    public string SeveridadNombre { get; set; } = string.Empty;
    public EstadoAlerta Estado { get; set; }
    public string EstadoNombre { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public string? Enlace { get; set; }
    public string? TextoEnlace { get; set; }
    public DateTime PrimeraVez { get; set; }
    public DateTime UltimaVez { get; set; }
    public DateTime? DescartadaEn { get; set; }
    public string? DescartadaPor { get; set; }
    public string? MotivoDescarte { get; set; }
    public DateTime? ResueltaEn { get; set; }
}

public class ResumenAlertasResponse
{
    public int Criticas { get; set; }
    public int Altas { get; set; }
    public int Medias { get; set; }
    public int Informativas { get; set; }
    public int Activas => Criticas + Altas + Medias + Informativas;
    public int Descartadas { get; set; }
}

public class AlertasPaginaResponse : PaginaResponse<AlertaResponse>
{
    public ResumenAlertasResponse Resumen { get; set; } = new();
    public DateTime Revisado { get; set; }
}

// ------------------------------------------------------------------ panel

public class PanelInsumoResponse
{
    public int InsumoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool EsCritico { get; set; }
    public decimal? DiasCobertura { get; set; }
    public int TiempoEntrega { get; set; }
    public string Estado { get; set; } = string.Empty;
    public decimal CantidadSugerida { get; set; }
    public string UnidadCompraCodigo { get; set; } = string.Empty;
}

public class PanelProductoResponse
{
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal StockPaquetes { get; set; }
    public decimal VentaDiaria { get; set; }
    public decimal? DiasDeVenta { get; set; }
    public decimal PromedioSemanal { get; set; }
    public decimal Proximas4 { get; set; }
    public decimal? Variacion { get; set; }
    public decimal? Mape { get; set; }
    public string Calidad { get; set; } = "sin";
    public decimal ProducidoSemana { get; set; }
    public decimal ProgramadoSemana { get; set; }
}

public class PanelPedidoResponse
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string ProveedorNombre { get; set; } = string.Empty;
    public DateOnly FechaEstimada { get; set; }
    public int DiasAtraso { get; set; }
    public string Insumos { get; set; } = string.Empty;
    public decimal Avance { get; set; }
}

public class PanelResponse
{
    public DateTime Generado { get; set; }
    public ResumenAlertasResponse Alertas { get; set; } = new();
    public List<AlertaResponse> Principales { get; set; } = [];
    public List<PanelInsumoResponse> Insumos { get; set; } = [];
    public List<PanelProductoResponse> Productos { get; set; } = [];
    public List<PanelPedidoResponse> Pedidos { get; set; } = [];
    public int PedidosEnCamino { get; set; }
    public int PedidosAtrasados { get; set; }
    public decimal VentasUltimos7 { get; set; }
    public decimal VentasPrevios7 { get; set; }
    public decimal ProducidoSemana { get; set; }
    public int PartesSemana { get; set; }
    public decimal ProgramadoPendienteSemana { get; set; }
    public DateTime? PronosticoDel { get; set; }
    public decimal? MapePromedio { get; set; }
    public bool PronosticoExitoso { get; set; }
}

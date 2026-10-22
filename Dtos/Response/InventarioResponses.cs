using SPIn.Models;

namespace SPIn.Dtos.Response;

public class StockAlmacenResponse
{
    public int AlmacenId { get; set; }
    public string AlmacenCodigo { get; set; } = string.Empty;
    public string AlmacenNombre { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
}

public class StockInsumoResponse
{
    public int InsumoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string CategoriaNombre { get; set; } = string.Empty;
    public string UnidadConsumoCodigo { get; set; } = string.Empty;
    public string UnidadCompraCodigo { get; set; } = string.Empty;
    public decimal FactorConversion { get; set; }
    public bool EsCritico { get; set; }
    public decimal StockMinimo { get; set; }
    public decimal StockMaximo { get; set; }
    public decimal Total { get; set; }
    /// <summary>Pendiente de llegar en pedidos (unidad de consumo). Entrega 7.</summary>
    public decimal EnCamino { get; set; }
    /// <summary>"sin", "bajo", "normal" o "sobre".</summary>
    public string Situacion { get; set; } = "normal";
    public DateTime? UltimoMovimiento { get; set; }
    public List<StockAlmacenResponse> Almacenes { get; set; } = [];
}

public class ResumenStockResponse
{
    public int Insumos { get; set; }
    public int SinStock { get; set; }
    public int BajoMinimo { get; set; }
    public int SobreMaximo { get; set; }
}

public class MovimientoResponse
{
    public long Id { get; set; }
    public DateTime Fecha { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public int Signo { get; set; }
    public int InsumoId { get; set; }
    public string InsumoCodigo { get; set; } = string.Empty;
    public string InsumoNombre { get; set; } = string.Empty;
    public string UnidadConsumoCodigo { get; set; } = string.Empty;
    public int AlmacenId { get; set; }
    public string AlmacenCodigo { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal SaldoAlmacen { get; set; }
    public string? Documento { get; set; }
    public string? Motivo { get; set; }
    public string? Referencia { get; set; }
    public bool Revertido { get; set; }
    public long? RevierteId { get; set; }
    public string Usuario { get; set; } = string.Empty;
    /// <summary>Se puede revertir: no es una reversión, no está revertido y no viene de un conteo.</summary>
    public bool Reversible { get; set; }
}

public class PaginaResponse<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int TotalPaginas => TamanoPagina <= 0 ? 0 : (int)Math.Ceiling(Total / (double)TamanoPagina);
}

public class KardexLineaResponse
{
    public long Id { get; set; }
    public DateTime Fecha { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public string AlmacenCodigo { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public string? Detalle { get; set; }
    public decimal? Entrada { get; set; }
    public decimal? Salida { get; set; }
    public decimal Saldo { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public bool Revertido { get; set; }
}

public class KardexResponse
{
    public int InsumoId { get; set; }
    public string InsumoCodigo { get; set; } = string.Empty;
    public string InsumoNombre { get; set; } = string.Empty;
    public string CategoriaNombre { get; set; } = string.Empty;
    public string UnidadConsumoCodigo { get; set; } = string.Empty;
    public decimal StockMinimo { get; set; }
    public decimal StockMaximo { get; set; }
    public string AlmacenNombre { get; set; } = "Todos los almacenes";
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public decimal SaldoInicial { get; set; }
    public decimal TotalEntradas { get; set; }
    public decimal TotalSalidas { get; set; }
    public decimal SaldoFinal { get; set; }
    public int Movimientos { get; set; }
    public PaginaResponse<KardexLineaResponse> Lineas { get; set; } = new();
    public DateTime GeneradoEn { get; set; }
    public string GeneradoPor { get; set; } = string.Empty;
}

public class ConteoResponse
{
    public int Id { get; set; }
    public int Numero { get; set; }
    public int AlmacenId { get; set; }
    public string AlmacenCodigo { get; set; } = string.Empty;
    public string AlmacenNombre { get; set; } = string.Empty;
    public string? CategoriaNombre { get; set; }
    public EstadoConteo Estado { get; set; }
    public string EstadoNombre { get; set; } = string.Empty;
    public string? Observacion { get; set; }
    public DateTime CreadoEn { get; set; }
    public string CreadoPor { get; set; } = string.Empty;
    public DateTime? CerradoEn { get; set; }
    public string? CerradoPor { get; set; }
    public int Insumos { get; set; }
    public int Contados { get; set; }
    public int ConDiferencia { get; set; }
}

public class ConteoDetalleResponse
{
    public int InsumoId { get; set; }
    public string InsumoCodigo { get; set; } = string.Empty;
    public string InsumoNombre { get; set; } = string.Empty;
    public string CategoriaNombre { get; set; } = string.Empty;
    public string UnidadConsumoCodigo { get; set; } = string.Empty;
    public decimal StockSistema { get; set; }
    /// <summary>Stock actual (para avisar si hubo movimientos durante el conteo). Solo en conteos abiertos.</summary>
    public decimal? StockActual { get; set; }
    public decimal? CantidadContada { get; set; }
    public decimal? StockAlAprobar { get; set; }
    public decimal? AjusteAplicado { get; set; }
}

public class ConteoDetalleCompletoResponse : ConteoResponse
{
    public List<ConteoDetalleResponse> Detalles { get; set; } = [];
    /// <summary>% de diferencia a partir del cual solo un administrador puede aprobar.</summary>
    public decimal UmbralAprobacion { get; set; }
    public bool PuedeAprobarDiferenciasGrandes { get; set; }
}

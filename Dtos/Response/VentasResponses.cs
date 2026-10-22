using SPIn.Models;

namespace SPIn.Dtos.Response;

public class VentaResponse
{
    public long Id { get; set; }
    public DateOnly Fecha { get; set; }
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public int UnidadesPorPaquete { get; set; }
    public CanalVenta Canal { get; set; }
    public string CanalNombre { get; set; } = string.Empty;
    public decimal Paquetes { get; set; }
    public decimal Botellas => Paquetes * UnidadesPorPaquete;
    public string? Documento { get; set; }
    public OrigenVenta Origen { get; set; }
    public bool AfectaStock { get; set; }
    public string? AlmacenCodigo { get; set; }
    public bool Anulada { get; set; }
    public string? MotivoAnulacion { get; set; }
    public string CreadoPor { get; set; } = string.Empty;
}

public class ResumenVentasResponse
{
    public int Registros { get; set; }
    public decimal Paquetes { get; set; }
    public decimal Botellas { get; set; }
    public int Productos { get; set; }
    public int Dias { get; set; }
}

public class VentasPaginaResponse : PaginaResponse<VentaResponse>
{
    public ResumenVentasResponse Resumen { get; set; } = new();
}

public class VentaSemanaResponse
{
    /// <summary>Lunes de la semana.</summary>
    public DateOnly Semana { get; set; }
    public decimal Paquetes { get; set; }
}

public class VentasSemanalesResponse
{
    public int? ProductoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public List<VentaSemanaResponse> Semanas { get; set; } = [];
    public DateOnly? PrimeraVenta { get; set; }
    public int SemanasConHistorial { get; set; }
}

public class StockProductoResponse
{
    public int ProductoId { get; set; }
    /// <summary>Clave de fila para el listado del frontend.</summary>
    public int Id => ProductoId;
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string FormatoNombre { get; set; } = string.Empty;
    public int UnidadesPorPaquete { get; set; }
    public int PaquetesPorPallet { get; set; }
    public decimal Paquetes { get; set; }
    public decimal Botellas => Paquetes * UnidadesPorPaquete;
    public decimal Pallets => PaquetesPorPallet > 0 ? Math.Round(Paquetes / PaquetesPorPallet, 2) : 0;
    public DateTime? UltimoMovimiento { get; set; }
    /// <summary>Promedio diario de venta de los últimos 28 días (paquetes), para dar contexto al stock.</summary>
    public decimal VentaDiariaPromedio { get; set; }
    public decimal? DiasDeVenta => VentaDiariaPromedio > 0 ? Math.Round(Paquetes / VentaDiariaPromedio, 1) : null;
    public List<StockAlmacenResponse> Almacenes { get; set; } = [];
}

public class MovimientoProductoResponse
{
    public long Id { get; set; }
    public DateTime Fecha { get; set; }
    public TipoMovimientoProducto Tipo { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public int Signo { get; set; }
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public string AlmacenCodigo { get; set; } = string.Empty;
    public decimal Paquetes { get; set; }
    public decimal SaldoAlmacen { get; set; }
    public string? Documento { get; set; }
    public string? Motivo { get; set; }
    public string? Referencia { get; set; }
    public bool Revertido { get; set; }
    public string Usuario { get; set; } = string.Empty;
}

using SPIn.Models;

namespace SPIn.Repository;

/// <summary>Datos insumo para el cálculo de reposición.</summary>
public class InsumoReposicionDatos
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public string UnidadConsumoCodigo { get; set; } = string.Empty;
    public string UnidadCompraCodigo { get; set; } = string.Empty;
    public decimal FactorConversion { get; set; }
    public decimal StockMinimo { get; set; }
    public decimal StockMaximo { get; set; }
    public decimal? CapacidadMaxima { get; set; }
    public bool EsCritico { get; set; }
    public decimal NivelServicio { get; set; }
    public TipoConsumo TipoConsumo { get; set; }
    public decimal? ConsumoEstimadoDiario { get; set; }
    public decimal Stock { get; set; }
    public CondicionReposicion? Condicion { get; set; }
}

public class CondicionReposicion
{
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public int TiempoEntregaDias { get; set; }
    public int VariacionEntregaDias { get; set; }
    public decimal PedidoMinimo { get; set; }
    public decimal MultiploCompra { get; set; }
    public decimal PrecioUnitario { get; set; }
    public Moneda Moneda { get; set; }
    public bool EsPrincipal { get; set; }
}

public interface IPrediccionRepository
{
    /// <summary>Configuración de la empresa actual (la crea con valores por defecto si no existe).</summary>
    Task<ConfiguracionEmpresa> ConfiguracionAsync();
    Task MarcarReentrenoPendienteAsync();

    Task<List<(int ProductoId, DateOnly Fecha, decimal Paquetes)>> VentasAsync();
    Task<List<(int Id, string Codigo, string Nombre, bool Activo)>> ProductosAsync();

    void AgregarEjecucion(EjecucionPronostico ejecucion);
    Task<EjecucionPronostico?> UltimaEjecucionAsync(bool soloExitosas);
    Task<List<EjecucionPronostico>> EjecucionesAsync(int cantidad);
    Task<List<PronosticoProducto>> ResultadosAsync(int ejecucionId);
    Task<PronosticoProducto?> ResultadoAsync(int ejecucionId, int productoId);
    /// <summary>Deja solo las últimas N ejecuciones (las anteriores se borran con sus resultados).</summary>
    Task PodarAsync(int conservar);

    Task<List<InsumoReposicionDatos>> InsumosReposicionAsync();
    /// <summary>Consumo diario promedio real (salidas a producción, consumo y mermas no revertidas) de los últimos N días.</summary>
    Task<Dictionary<int, decimal>> ConsumoHistoricoDiarioAsync(int dias, DateTime desde);

    Task<List<int>> EmpresasActivasAsync();
    Task GuardarAsync();
}

using System.Text.Json.Serialization;
using SPIn.Models;

namespace SPIn.Dtos.Response;

public class CategoriaResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public int Insumos { get; set; }
}

public class UnidadResponse
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public int Insumos { get; set; }
}

public class InsumoResponse
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public int UnidadCompraId { get; set; }
    public string UnidadCompraCodigo { get; set; } = string.Empty;
    public int UnidadConsumoId { get; set; }
    public string UnidadConsumoCodigo { get; set; } = string.Empty;
    public decimal FactorConversion { get; set; }
    public decimal StockMinimo { get; set; }
    public decimal StockMaximo { get; set; }
    public decimal? CapacidadMaxima { get; set; }
    public bool EsCritico { get; set; }
    public bool EsImportado { get; set; }
    public decimal NivelServicio { get; set; }
    public TipoConsumo TipoConsumo { get; set; }
    public string TipoConsumoNombre { get; set; } = string.Empty;
    public decimal? ConsumoEstimadoDiario { get; set; }
    public bool ControlaLote { get; set; }
    public bool Activo { get; set; }

    public int Proveedores { get; set; }
    public string? ProveedorPrincipal { get; set; }
    public bool? ProveedorPrincipalActivo { get; set; }
    public int? EntregaPrincipalDias { get; set; }

    /// <summary>Solo se llena al consultar un insumo (panel de detalle); en los listados no se envía.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<CondicionCompraResponse>? Condiciones { get; set; }

    /// <summary>Stock por almacén (solo en el detalle).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<StockAlmacenResponse>? Stock { get; set; }
}

public class ProveedorResponse
{
    public int Id { get; set; }
    public string Pais { get; set; } = string.Empty;
    public string? Ruc { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string? Contacto { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public bool EsExtranjero { get; set; }
    public bool Activo { get; set; }
    public int Insumos { get; set; }
    public int InsumosPrincipal { get; set; }
}

public class CondicionCompraResponse
{
    public int Id { get; set; }
    public int InsumoId { get; set; }
    public string InsumoCodigo { get; set; } = string.Empty;
    public string InsumoNombre { get; set; } = string.Empty;
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public string ProveedorPais { get; set; } = string.Empty;
    public bool ProveedorActivo { get; set; }
    public Moneda Moneda { get; set; }
    public string MonedaSimbolo { get; set; } = string.Empty;
    public decimal PrecioUnitario { get; set; }
    public string UnidadCompraCodigo { get; set; } = string.Empty;
    public int TiempoEntregaDias { get; set; }
    public int VariacionEntregaDias { get; set; }
    public decimal PedidoMinimo { get; set; }
    public decimal MultiploCompra { get; set; }
    public bool EsPrincipal { get; set; }
}

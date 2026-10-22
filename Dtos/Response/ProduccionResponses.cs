using System.Text.Json.Serialization;
using SPIn.Models;

namespace SPIn.Dtos.Response;

public class ProductoResponse
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Sabor { get; set; }
    public int FormatoId { get; set; }
    public string FormatoNombre { get; set; } = string.Empty;
    public TipoEnvase TipoEnvase { get; set; }
    public string TipoEnvaseNombre { get; set; } = string.Empty;
    public int VolumenMl { get; set; }
    public int UnidadesPorPaquete { get; set; }
    public int PaquetesPorPallet { get; set; }
    public int BotellasPorPallet => UnidadesPorPaquete * PaquetesPorPallet;
    public bool Activo { get; set; }

    /// <summary>Versión de la receta vigente hoy (null si no tiene).</summary>
    public int? RecetaVersion { get; set; }
    public int RecetaInsumos { get; set; }
    public int Versiones { get; set; }

    // Solo en el detalle:
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RecetaResponse? RecetaVigente { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<RecetaVersionResponse>? Historial { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? LineasCompatibles { get; set; }
}

public class RecetaVersionResponse
{
    public int Id { get; set; }
    public int Version { get; set; }
    public DateOnly VigenteDesde { get; set; }
    public DateOnly? VigenteHasta { get; set; }
    public string? Comentario { get; set; }
    public int Insumos { get; set; }
    public string? CreadoPor { get; set; }
    public bool EsVigente { get; set; }
    public bool EsFutura { get; set; }
}

public class RecetaResponse : RecetaVersionResponse
{
    public List<RecetaDetalleResponse> Detalles { get; set; } = [];
}

public class RecetaDetalleResponse
{
    public int InsumoId { get; set; }
    public string InsumoCodigo { get; set; } = string.Empty;
    public string InsumoNombre { get; set; } = string.Empty;
    public string UnidadConsumoCodigo { get; set; } = string.Empty;
    public bool InsumoActivo { get; set; }
    public decimal Cantidad { get; set; }
    public NivelReceta Nivel { get; set; }
    public string NivelNombre { get; set; } = string.Empty;
    public decimal MermaPorcentaje { get; set; }
}

/// <summary>Insumo que se puede usar en una receta (consumo por producción).</summary>
public class InsumoRecetaOpcion
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string CategoriaNombre { get; set; } = string.Empty;
    public string UnidadConsumoCodigo { get; set; } = string.Empty;
}

public class ExplosionResponse
{
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public int RecetaVersion { get; set; }
    public DateOnly Fecha { get; set; }
    public decimal Botellas { get; set; }
    public decimal Paquetes { get; set; }
    public decimal Pallets { get; set; }
    public List<ConsumoInsumoResponse> Insumos { get; set; } = [];
}

public class ConsumoInsumoResponse
{
    public int InsumoId { get; set; }
    public string InsumoCodigo { get; set; } = string.Empty;
    public string InsumoNombre { get; set; } = string.Empty;
    public string UnidadConsumoCodigo { get; set; } = string.Empty;
    public string UnidadCompraCodigo { get; set; } = string.Empty;
    public decimal FactorConversion { get; set; }
    /// <summary>Consumo sin merma, en unidad de consumo.</summary>
    public decimal ConsumoNeto { get; set; }
    /// <summary>Consumo con merma, en unidad de consumo.</summary>
    public decimal ConsumoTotal { get; set; }
    /// <summary>ConsumoTotal expresado en unidad de compra.</summary>
    public decimal EnUnidadCompra { get; set; }
}

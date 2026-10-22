using System.ComponentModel.DataAnnotations;
using SPIn.Models;

namespace SPIn.Dtos.Request;

public class ConfiguracionRequest
{
    [Range(4, 26, ErrorMessage = "El horizonte debe estar entre 4 y 26 semanas.")]
    public int SemanasHorizonte { get; set; } = 12;

    [Range(2, 26, ErrorMessage = "La validación debe estar entre 2 y 26 semanas.")]
    public int SemanasValidacion { get; set; } = 8;

    [Range(4, 52, ErrorMessage = "El mínimo de historia debe estar entre 4 y 52 semanas.")]
    public int SemanasMinimas { get; set; } = 12;

    [EnumDataType(typeof(FrecuenciaReentrenamiento), ErrorMessage = "Seleccione la frecuencia.")]
    public FrecuenciaReentrenamiento Frecuencia { get; set; } = FrecuenciaReentrenamiento.Semanal;

    [Range(0, 23, ErrorMessage = "La hora debe estar entre 0 y 23.")]
    public int HoraReentrenamiento { get; set; } = 6;

    [Range(1, 60, ErrorMessage = "El periodo de revisión debe estar entre 1 y 60 días.")]
    public int DiasRevision { get; set; } = 7;

    [Range(1, 180, ErrorMessage = "El tiempo de entrega debe estar entre 1 y 180 días.")]
    public int DiasEntregaPorDefecto { get; set; } = 7;

    [Range(typeof(decimal), "0.5", "50", ErrorMessage = "El umbral debe estar entre 0.5 % y 50 %.")]
    public decimal UmbralConteo { get; set; } = 5;
}

public class FiltroReposicionRequest
{
    [StringLength(100)]
    public string? Buscar { get; set; }

    /// <summary>"urgente", "pedir" (incluye urgentes), "ok", "sin" (sin demanda o sin proveedor) o vacío.</summary>
    [RegularExpression("^(urgente|pedir|ok|sin)?$", ErrorMessage = "El estado no es válido.")]
    public string? Estado { get; set; }

    public int? CategoriaId { get; set; }

    [Range(1, 100000)] public int Pagina { get; set; } = 1;
    [Range(5, 100)] public int TamanoPagina { get; set; } = 20;
}

public class FiltroAlertaRequest
{
    [StringLength(100)]
    public string? Buscar { get; set; }

    /// <summary>"activas" (por defecto), "importantes" (críticas y altas activas), "descartadas" o "resueltas" (últimos 30 días).</summary>
    [RegularExpression("^(activas|importantes|descartadas|resueltas)?$", ErrorMessage = "El estado no es válido.")]
    public string? Estado { get; set; }

    [EnumDataType(typeof(TipoAlerta), ErrorMessage = "El tipo no es válido.")]
    public TipoAlerta? Tipo { get; set; }

    [Range(1, 100000)] public int Pagina { get; set; } = 1;
    [Range(5, 100)] public int TamanoPagina { get; set; } = 20;
}

public class DescartarAlertaRequest
{
    [StringLength(200, ErrorMessage = "El motivo admite hasta 200 caracteres.")]
    public string? Motivo { get; set; }
}

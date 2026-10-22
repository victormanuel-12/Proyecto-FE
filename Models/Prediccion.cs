namespace SPIn.Models;

public enum FrecuenciaReentrenamiento
{
    /// <summary>Cada lunes a la hora configurada (recomendado: la semana anterior ya cerró).</summary>
    Semanal = 1,
    Diaria = 2,
    /// <summary>Solo con el botón «Recalcular pronóstico».</summary>
    Manual = 3
}

public enum OrigenEjecucion
{
    Manual = 1,
    Programado = 2,
    AlIniciar = 3,
    Importacion = 4
}

public enum MetodoPronostico
{
    /// <summary>ML.NET: Singular Spectrum Analysis (tendencia + ciclos).</summary>
    Ssa = 1,
    /// <summary>Suavizamiento exponencial doble (nivel + tendencia).</summary>
    Holt = 2,
    /// <summary>Promedio de las últimas 8 semanas.</summary>
    PromedioMovil = 3,
    /// <summary>Misma semana del año anterior (requiere más de un año de historia).</summary>
    Estacional = 4,
    /// <summary>Sin historia suficiente: se usa lo disponible o el consumo estimado del insumo.</summary>
    SinHistorial = 5
}

/// <summary>Parámetros del motor predictivo y reglas generales, por empresa (Entrega 8).</summary>
public class ConfiguracionEmpresa : IEntidadEmpresa
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }

    /// <summary>Semanas a pronosticar.</summary>
    public int SemanasHorizonte { get; set; } = 12;
    /// <summary>Últimas semanas reales que se reservan para medir el error (MAPE).</summary>
    public int SemanasValidacion { get; set; } = 8;
    /// <summary>Semanas mínimas de historia para usar modelos (menos: promedio de lo disponible).</summary>
    public int SemanasMinimas { get; set; } = 12;

    public FrecuenciaReentrenamiento Frecuencia { get; set; } = FrecuenciaReentrenamiento.Semanal;
    /// <summary>Hora (0 a 23, hora de Perú) del reentrenamiento programado.</summary>
    public int HoraReentrenamiento { get; set; } = 6;
    /// <summary>Lo marca la importación de ventas: el servicio en segundo plano reentrena en el siguiente minuto.</summary>
    public bool ReentrenarPendiente { get; set; }

    /// <summary>Cada cuántos días se revisa y se compra (para la cantidad sugerida).</summary>
    public int DiasRevision { get; set; } = 7;
    /// <summary>Tiempo de entrega cuando el insumo no tiene proveedor con condiciones.</summary>
    public int DiasEntregaPorDefecto { get; set; } = 7;
    /// <summary>Diferencia (%) de un conteo físico que solo aprueba un Administrador.</summary>
    public decimal UmbralConteo { get; set; } = 5;

    public DateTime? ModificadoEn { get; set; }
    public string? ModificadoPor { get; set; }
}

/// <summary>Cada entrenamiento del motor: cuándo, por qué, cuánto tardó y cómo le fue.</summary>
public class EjecucionPronostico : IEntidadEmpresa
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public DateTime Fecha { get; set; }
    public OrigenEjecucion Origen { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public int DuracionMs { get; set; }
    public bool Exitosa { get; set; }
    public string? Mensaje { get; set; }
    public int Productos { get; set; }
    /// <summary>MAPE promedio ponderado por volumen (%).</summary>
    public decimal? MapePromedio { get; set; }
    public DateOnly UltimaSemanaHistorial { get; set; }
    public int SemanasHorizonte { get; set; }
    public ICollection<PronosticoProducto> Resultados { get; set; } = new List<PronosticoProducto>();
}

public class PronosticoProducto
{
    public int Id { get; set; }
    public int EjecucionId { get; set; }
    public EjecucionPronostico? Ejecucion { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public MetodoPronostico Metodo { get; set; }
    public decimal? Mape { get; set; }
    public decimal? MapeSsa { get; set; }
    public decimal? MapeHolt { get; set; }
    public decimal? MapePromedio { get; set; }
    public decimal? MapeEstacional { get; set; }
    public int SemanasHistorial { get; set; }
    /// <summary>Promedio semanal de las últimas 8 semanas reales (paquetes).</summary>
    public decimal PromedioReciente { get; set; }
    /// <summary>Nota técnica (por ejemplo, por qué no se usó SSA).</summary>
    public string? Nota { get; set; }
    public ICollection<PronosticoSemana> Semanas { get; set; } = new List<PronosticoSemana>();
}

/// <summary>Una semana pronosticada (o, si EsValidacion, una semana real usada para medir el error).</summary>
public class PronosticoSemana
{
    public int Id { get; set; }
    public int PronosticoProductoId { get; set; }
    public PronosticoProducto? PronosticoProducto { get; set; }
    public DateOnly Semana { get; set; }
    public bool EsValidacion { get; set; }
    public decimal Paquetes { get; set; }
    public decimal Minimo { get; set; }
    public decimal Maximo { get; set; }
    /// <summary>Solo en validación: lo que realmente se vendió.</summary>
    public decimal? Real { get; set; }
}

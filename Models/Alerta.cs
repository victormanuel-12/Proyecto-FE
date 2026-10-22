namespace SPIn.Models;

public enum SeveridadAlerta
{
    Critica = 1,
    Alta = 2,
    Media = 3,
    Informativa = 4
}

public enum EstadoAlerta
{
    /// <summary>La condición sigue presente y nadie la descartó.</summary>
    Activa = 1,
    /// <summary>Un usuario la dio por vista; vuelve a «Activa» si la condición empeora a una severidad mayor.</summary>
    Descartada = 2,
    /// <summary>La condición desapareció (por ejemplo, llegó el pedido): se cierra sola.</summary>
    Resuelta = 3
}

public enum TipoAlerta
{
    InsumoUrgente = 1,
    InsumoPorPedir = 2,
    InsumoBajoMinimo = 3,
    InsumoSinProveedor = 4,
    InsumoSobreMaximo = 5,
    PedidoAtrasado = 6,
    PedidoPorLlegar = 7,
    ProductoTerminadoBajo = 8,
    ProgramacionVencida = 9,
    PronosticoDesactualizado = 10,
    PronosticoErrorAlto = 11,
    ConteoAbierto = 12
}

/// <summary>
/// Alerta (Entrega 9). SPIn la genera revisando el estado actual del sistema; la clave identifica la condición
/// (por ejemplo «insumo-urgente:12») para no duplicarla y para cerrarla sola cuando deja de cumplirse.
/// </summary>
public class Alerta : IEntidadEmpresa
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public string Clave { get; set; } = string.Empty;
    public TipoAlerta Tipo { get; set; }
    public SeveridadAlerta Severidad { get; set; }
    public EstadoAlerta Estado { get; set; } = EstadoAlerta.Activa;
    public string Titulo { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    /// <summary>Página donde se atiende (por ejemplo, /reposicion?estado=urgente).</summary>
    public string? Enlace { get; set; }
    public string? TextoEnlace { get; set; }
    public DateTime PrimeraVez { get; set; }
    public DateTime UltimaVez { get; set; }
    public DateTime? DescartadaEn { get; set; }
    public string? DescartadaPor { get; set; }
    public string? MotivoDescarte { get; set; }
    public DateTime? ResueltaEn { get; set; }
}

public static class EtiquetasAlerta
{
    public static string De(SeveridadAlerta s) => s switch
    {
        SeveridadAlerta.Critica => "Crítica",
        SeveridadAlerta.Alta => "Alta",
        SeveridadAlerta.Media => "Media",
        _ => "Informativa"
    };

    public static string De(EstadoAlerta e) => e switch
    {
        EstadoAlerta.Activa => "Activa",
        EstadoAlerta.Descartada => "Descartada",
        _ => "Resuelta"
    };

    public static string De(TipoAlerta t) => t switch
    {
        TipoAlerta.InsumoUrgente => "Insumo por agotarse",
        TipoAlerta.InsumoPorPedir => "Insumo por pedir",
        TipoAlerta.InsumoBajoMinimo => "Bajo el stock mínimo",
        TipoAlerta.InsumoSinProveedor => "Insumo sin proveedor",
        TipoAlerta.InsumoSobreMaximo => "Sobre el stock máximo",
        TipoAlerta.PedidoAtrasado => "Pedido atrasado",
        TipoAlerta.PedidoPorLlegar => "Pedido por llegar",
        TipoAlerta.ProductoTerminadoBajo => "Producto terminado bajo",
        TipoAlerta.ProgramacionVencida => "Programación vencida",
        TipoAlerta.PronosticoDesactualizado => "Pronóstico desactualizado",
        TipoAlerta.PronosticoErrorAlto => "Pronóstico con error alto",
        TipoAlerta.ConteoAbierto => "Conteo físico abierto",
        _ => t.ToString()
    };
}

namespace SPIn.Models;

/// <summary>Producto terminado (SKU). Ej.: Gaseosa cola 500 ml, paquete x12.</summary>
public class Producto : EntidadAuditable, IEntidadEmpresa
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Sabor { get; set; }

    public int FormatoId { get; set; }
    public Formato? Formato { get; set; }

    /// <summary>Botellas por paquete (ej.: 12).</summary>
    public int UnidadesPorPaquete { get; set; } = 1;

    /// <summary>Paquetes por pallet (ej.: 84).</summary>
    public int PaquetesPorPallet { get; set; } = 1;

    public bool Activo { get; set; } = true;

    public ICollection<Receta> Recetas { get; set; } = new List<Receta>();
}

/// <summary>
/// Versión de la lista de materiales (BOM) de un producto. Cada cambio con otra fecha crea una versión nueva;
/// la anterior queda cerrada el día previo, así la producción pasada se calcula con la receta de su fecha.
/// </summary>
public class Receta : EntidadAuditable, IEntidadEmpresa
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int Version { get; set; }
    public DateOnly VigenteDesde { get; set; }
    public DateOnly? VigenteHasta { get; set; }
    public string? Comentario { get; set; }

    public ICollection<RecetaDetalle> Detalles { get; set; } = new List<RecetaDetalle>();
}

public class RecetaDetalle
{
    public int Id { get; set; }
    public int RecetaId { get; set; }
    public Receta? Receta { get; set; }
    public int InsumoId { get; set; }
    public Insumo? Insumo { get; set; }

    /// <summary>Cantidad en la unidad de consumo del insumo, por botella, paquete o pallet según <see cref="Nivel"/>.</summary>
    public decimal Cantidad { get; set; }
    public NivelReceta Nivel { get; set; } = NivelReceta.PorBotella;

    /// <summary>Merma esperada en el proceso (%). Se suma al consumo calculado.</summary>
    public decimal MermaPorcentaje { get; set; }
}

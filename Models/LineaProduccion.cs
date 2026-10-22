namespace SPIn.Models;

public class LineaProduccion : EntidadAuditable, IEntidadEmpresa
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int PlantaId { get; set; }
    public Planta? Planta { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public TipoEnvase TipoEnvase { get; set; }

    /// <summary>Botellas por hora que la línea produce en condiciones normales.</summary>
    public int VelocidadNominal { get; set; }

    public bool Activo { get; set; } = true;
    public ICollection<LineaFormato> Formatos { get; set; } = new List<LineaFormato>();
}

public class LineaFormato
{
    public int LineaProduccionId { get; set; }
    public LineaProduccion? LineaProduccion { get; set; }
    public int FormatoId { get; set; }
    public Formato? Formato { get; set; }
}

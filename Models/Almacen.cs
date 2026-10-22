namespace SPIn.Models;

public class Almacen : EntidadAuditable, IEntidadEmpresa
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int PlantaId { get; set; }
    public Planta? Planta { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public TipoAlmacen Tipo { get; set; } = TipoAlmacen.Insumos;
    public bool Activo { get; set; } = true;
}

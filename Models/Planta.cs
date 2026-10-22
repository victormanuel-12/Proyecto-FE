namespace SPIn.Models;

public class Planta : EntidadAuditable, IEntidadEmpresa
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public Empresa? Empresa { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Almacen> Almacenes { get; set; } = new List<Almacen>();
    public ICollection<LineaProduccion> Lineas { get; set; } = new List<LineaProduccion>();
}

namespace SPIn.Models;

public class Empresa : EntidadAuditable
{
    public int Id { get; set; }
    public string Ruc { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public bool Activo { get; set; } = true;
}

namespace SPIn.Models;

/// <summary>Formato de envase (catálogo general, igual para todas las empresas). Ej.: PET 500 ml.</summary>
public class Formato
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoEnvase TipoEnvase { get; set; }
    public int VolumenMl { get; set; }
}

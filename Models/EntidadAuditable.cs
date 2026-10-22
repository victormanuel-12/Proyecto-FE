namespace SPIn.Models;

/// <summary>Campos de auditoría que se llenan automáticamente en ApplicationDbContext.SaveChangesAsync.</summary>
public abstract class EntidadAuditable
{
    public DateTime CreadoEn { get; set; }
    public string? CreadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }
    public string? ModificadoPor { get; set; }
}

/// <summary>Entidad que pertenece a una empresa cliente. Se filtra automáticamente por la empresa del usuario.</summary>
public interface IEntidadEmpresa
{
    int EmpresaId { get; set; }
}

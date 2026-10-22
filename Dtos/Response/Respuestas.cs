using System.Text.Json.Serialization;
using SPIn.Models;

namespace SPIn.Dtos.Response;

public class EmpresaResponse
{
    public int Id { get; set; }
    public string Ruc { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public bool Activo { get; set; }
    public int Plantas { get; set; }
    public int Usuarios { get; set; }
}

public class PlantaResponse
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public bool Activo { get; set; }
    public int Almacenes { get; set; }
    public int Lineas { get; set; }
}

public class AlmacenResponse
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int PlantaId { get; set; }
    public string PlantaNombre { get; set; } = string.Empty;
    public TipoAlmacen Tipo { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
}

public class FormatoResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoEnvase TipoEnvase { get; set; }
    public int VolumenMl { get; set; }
}

public class LineaResponse
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int PlantaId { get; set; }
    public string PlantaNombre { get; set; } = string.Empty;
    public TipoEnvase TipoEnvase { get; set; }
    public string TipoEnvaseNombre { get; set; } = string.Empty;
    public int VelocidadNominal { get; set; }
    public bool Activo { get; set; }
    public List<FormatoResponse> Formatos { get; set; } = [];
}

public class UsuarioResponse
{
    public string Id { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string RolNombre { get; set; } = string.Empty;
    public int EmpresaId { get; set; }
    public string EmpresaNombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public bool Bloqueado { get; set; }
    public bool EsUsuarioActual { get; set; }

    /// <summary>Uso interno: SQLite no compara DateTimeOffset, por eso "Bloqueado" se calcula en memoria.</summary>
    [JsonIgnore]
    public DateTimeOffset? BloqueoHasta { get; set; }
}

public class ResumenInicioResponse
{
    public string EmpresaNombre { get; set; } = string.Empty;
    public int Plantas { get; set; }
    public int Almacenes { get; set; }
    public int Lineas { get; set; }
    public int Usuarios { get; set; }
    public int Insumos { get; set; }
    public int Proveedores { get; set; }
    public int ProductosConReceta { get; set; }
    public int InsumosConStock { get; set; }
    public int ProductosConVentas { get; set; }
}

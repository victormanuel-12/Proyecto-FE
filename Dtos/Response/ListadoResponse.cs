namespace SPIn.Dtos.Response;

/// <summary>Página de resultados con los conteos por estado que usa la franja de filtros.</summary>
public class ListadoResponse<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int TotalPaginas => TamanoPagina <= 0 ? 0 : (int)Math.Ceiling(Total / (double)TamanoPagina);
    public int Activos { get; set; }
    public int Inactivos { get; set; }
}

public class OpcionResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

/// <summary>Opción de producto con su empaque (para convertir botellas a paquetes en los formularios).</summary>
public class ProductoOpcionResponse : OpcionResponse
{
    public int UnidadesPorPaquete { get; set; }
    public bool Activo { get; set; }
}

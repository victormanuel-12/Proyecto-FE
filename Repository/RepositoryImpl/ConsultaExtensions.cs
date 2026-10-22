using Microsoft.EntityFrameworkCore;
using SPIn.Dtos.Response;

namespace SPIn.Repository.RepositoryImpl;

internal static class ConsultaExtensions
{
    public static async Task<ListadoResponse<T>> PaginarAsync<T>(
        this IQueryable<T> consulta, int pagina, int tamanoPagina, int activos, int inactivos)
    {
        var total = await consulta.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanoPagina));
        var paginaReal = Math.Min(Math.Max(1, pagina), totalPaginas);

        var items = await consulta
            .Skip((paginaReal - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return new ListadoResponse<T>
        {
            Items = items,
            Total = total,
            Pagina = paginaReal,
            TamanoPagina = tamanoPagina,
            Activos = activos,
            Inactivos = inactivos
        };
    }

    /// <summary>Texto de búsqueda normalizado (minúsculas) o null si está vacío.</summary>
    public static string? TextoBusqueda(string? buscar) =>
        string.IsNullOrWhiteSpace(buscar) ? null : buscar.Trim().ToLowerInvariant();
}

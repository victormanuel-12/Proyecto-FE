using Microsoft.EntityFrameworkCore;
using SPIn.Models;

namespace SPIn.Data;

/// <summary>
/// Categorías y unidades sugeridas para una embotelladora. Se crean una sola vez por empresa
/// (al registrarla o, para empresas existentes, al iniciar el sistema) y luego cada empresa las edita.
/// </summary>
public static class CatalogoInicial
{
    private static readonly (string Nombre, string Descripcion)[] Categorias =
    [
        ("Preformas", "Preformas PET por formato y color"),
        ("Tapas", "Tapas plásticas y coronas"),
        ("Etiquetas", "Etiquetas termoencogibles o BOPP por SKU"),
        ("CO₂", "Dióxido de carbono grado alimentario"),
        ("Concentrado y jarabe", "Concentrados por sabor y jarabe"),
        ("Azúcar", "Azúcar en sacos o a granel"),
        ("Embalaje", "Bandejas, film termoencogible, film stretch, esquineros, parihuelas"),
        ("Envases retornables", "Botellas de vidrio y jabas"),
        ("Insumos químicos", "Lubricante de transportador, soda cáustica, tinta del codificador")
    ];

    private static readonly (string Codigo, string Nombre)[] Unidades =
    [
        ("UND", "Unidad"),
        ("MIL", "Millar"),
        ("CJ", "Caja"),
        ("ROL", "Rollo"),
        ("SAC", "Saco"),
        ("KG", "Kilogramo"),
        ("G", "Gramo"),
        ("TN", "Tonelada"),
        ("L", "Litro"),
        ("ML", "Mililitro")
    ];

    /// <summary>Agrega lo que falte. No guarda: quien llama ejecuta SaveChangesAsync.</summary>
    public static async Task AgregarSiFaltaAsync(ApplicationDbContext db, int empresaId)
    {
        var tieneCategorias = await db.Categorias.IgnoreQueryFilters().AnyAsync(c => c.EmpresaId == empresaId);
        if (!tieneCategorias)
        {
            db.Categorias.AddRange(Categorias.Select(c => new CategoriaInsumo
            {
                EmpresaId = empresaId,
                Nombre = c.Nombre,
                Descripcion = c.Descripcion,
                Activo = true
            }));
        }

        var tieneUnidades = await db.Unidades.IgnoreQueryFilters().AnyAsync(u => u.EmpresaId == empresaId);
        if (!tieneUnidades)
        {
            db.Unidades.AddRange(Unidades.Select(u => new UnidadMedida
            {
                EmpresaId = empresaId,
                Codigo = u.Codigo,
                Nombre = u.Nombre,
                Activo = true
            }));
        }
    }
}

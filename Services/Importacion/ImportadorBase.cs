using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using SPIn.Dtos.Response;

namespace SPIn.Services.Importacion;

public interface IImportador
{
    string Tipo { get; }
    string Titulo { get; }
    string Descripcion { get; }
    int Orden { get; }
    string? Requisito { get; }
    IReadOnlyList<Columna> Columnas { get; }
    /// <summary>Modos cuando el registro ya existe; el primero es el predeterminado.</summary>
    IReadOnlyList<OpcionTextoResponse> Modos { get; }
    string? TextoFecha { get; }
    /// <summary>Filas de ejemplo para la plantilla (en el orden de <see cref="Columnas"/>).</summary>
    IReadOnlyList<string[]> Ejemplos { get; }
    /// <summary>Listas de valores válidos con los datos actuales de la empresa (para la hoja de instrucciones).</summary>
    Task<List<(string Titulo, List<string> Valores)>> ValoresValidosAsync();
    Task<Preparacion> PrepararAsync(List<Fila> filas, OpcionesImportacion opciones);
}

/// <summary>Ayudas comunes para interpretar celdas: textos, números (punto o coma decimal), sí/no, fechas y listas.</summary>
public abstract class ImportadorBase : IImportador
{
    public abstract string Tipo { get; }
    public abstract string Titulo { get; }
    public abstract string Descripcion { get; }
    public abstract int Orden { get; }
    public virtual string? Requisito => null;
    public abstract IReadOnlyList<Columna> Columnas { get; }
    public virtual IReadOnlyList<OpcionTextoResponse> Modos => ModosCatalogo;
    public virtual string? TextoFecha => null;
    public abstract IReadOnlyList<string[]> Ejemplos { get; }
    public abstract Task<List<(string Titulo, List<string> Valores)>> ValoresValidosAsync();
    public abstract Task<Preparacion> PrepararAsync(List<Fila> filas, OpcionesImportacion opciones);

    protected static readonly IReadOnlyList<OpcionTextoResponse> ModosCatalogo =
    [
        new() { Valor = "actualizar", Texto = "Actualizar los que ya existen" },
        new() { Valor = "omitir", Texto = "Omitir los que ya existen (solo crear nuevos)" }
    ];

    protected static string ModoElegido(OpcionesImportacion o, IReadOnlyList<OpcionTextoResponse> modos) =>
        modos.Any(m => m.Valor == o.Modo) ? o.Modo! : modos[0].Valor;

    /// <summary>Normaliza para comparar: minúsculas, sin tildes, sin espacios ni signos. FormKD convierte «CO₂» en «co2».</summary>
    public static string Clave(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        var sb = new StringBuilder();
        foreach (var c in texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormKD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(c)) sb.Append(c);
        }
        return sb.ToString();
    }

    // ------------------------------------------------------------ lectura de celdas
    protected static string? Texto(Fila f, Columna c)
    {
        var v = f.Valores.GetValueOrDefault(Clave(c.Nombre));
        return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    }

    protected static string? Requerido(Fila f, Columna c, Preparacion p)
    {
        var v = Texto(f, c);
        if (v is null) p.Error(f.Numero, c.Nombre, "Es obligatorio.");
        return v;
    }

    public static bool TryDecimal(string? texto, out decimal valor)
    {
        valor = 0;
        if (string.IsNullOrWhiteSpace(texto)) return false;
        var t = texto.Trim().Replace(" ", "").Replace(" ", "").Replace("%", "");
        var punto = t.LastIndexOf('.');
        var coma = t.LastIndexOf(',');
        if (punto >= 0 && coma >= 0)
        {
            // 1.234,56 o 1,234.56: el último signo es el decimal.
            t = coma > punto ? t.Replace(".", "").Replace(',', '.') : t.Replace(",", "");
        }
        else if (coma >= 0) t = t.Replace(',', '.');
        return decimal.TryParse(t, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
            CultureInfo.InvariantCulture, out valor);
    }

    protected static decimal? Numero(Fila f, Columna c, Preparacion p, bool obligatorio, decimal? predeterminado = null, decimal minimo = 0)
    {
        var t = Texto(f, c);
        if (t is null)
        {
            if (obligatorio) p.Error(f.Numero, c.Nombre, "Es obligatorio.");
            return predeterminado;
        }
        if (!TryDecimal(t, out var v)) { p.Error(f.Numero, c.Nombre, $"«{t}» no es un número."); return null; }
        if (v < minimo) { p.Error(f.Numero, c.Nombre, $"Debe ser mayor o igual a {minimo}."); return null; }
        return v;
    }

    protected static int? Entero(Fila f, Columna c, Preparacion p, bool obligatorio, int? predeterminado = null, int minimo = 0)
    {
        var d = Numero(f, c, p, obligatorio, predeterminado, minimo);
        if (d is null) return null;
        if (d != Math.Floor(d.Value)) { p.Error(f.Numero, c.Nombre, "Debe ser un número entero."); return null; }
        return (int)d.Value;
    }

    protected static bool? SiNo(Fila f, Columna c, Preparacion p, bool predeterminado = false)
    {
        var t = Clave(Texto(f, c));
        if (t == string.Empty) return predeterminado;
        if (t is "si" or "s" or "1" or "true" or "verdadero" or "x") return true;
        if (t is "no" or "n" or "0" or "false" or "falso") return false;
        p.Error(f.Numero, c.Nombre, "Escriba «sí» o «no».");
        return null;
    }

    public static bool TryFecha(string? texto, out DateOnly fecha)
    {
        fecha = default;
        if (string.IsNullOrWhiteSpace(texto)) return false;
        var t = texto.Trim();
        string[] formatos = ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "yyyy/MM/dd", "dd.MM.yyyy"];
        if (DateOnly.TryParseExact(t, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha)) return true;
        // Número de serie de Excel (fecha guardada como número).
        if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var serie) && serie is > 20000 and < 80000)
        {
            fecha = DateOnly.FromDateTime(DateTime.FromOADate(serie));
            return true;
        }
        return false;
    }

    protected static T? Opcion<T>(Fila f, Columna c, Preparacion p, IReadOnlyDictionary<string, T> valores, string queEs, bool obligatorio = true)
        where T : struct
    {
        var t = Texto(f, c);
        if (t is null)
        {
            if (obligatorio) p.Error(f.Numero, c.Nombre, "Es obligatorio.");
            return null;
        }
        if (valores.TryGetValue(Clave(t), out var v)) return v;
        p.Error(f.Numero, c.Nombre, $"No existe {queEs} «{t}».");
        return null;
    }

    /// <summary>Valida los atributos del DTO (los mismos que usa el formulario) y los registra como errores de la fila.</summary>
    protected static bool ValidarDto(object dto, Fila f, Preparacion p, IReadOnlyDictionary<string, string>? columnaPorPropiedad = null)
    {
        var resultados = new List<ValidationResult>();
        if (Validator.TryValidateObject(dto, new ValidationContext(dto), resultados, true)) return true;
        foreach (var r in resultados)
        {
            var propiedad = r.MemberNames.FirstOrDefault() ?? string.Empty;
            var columna = columnaPorPropiedad?.GetValueOrDefault(propiedad) ?? propiedad;
            p.Error(f.Numero, columna, r.ErrorMessage ?? "Valor no válido.");
        }
        return false;
    }

    /// <summary>Marca filas cuyo valor clave se repite dentro del archivo.</summary>
    protected static bool Duplicado(HashSet<string> vistos, string clave, Fila f, Columna c, Preparacion p, string texto)
    {
        if (vistos.Add(clave)) return false;
        p.Error(f.Numero, c.Nombre, $"«{texto}» está repetido en el archivo.");
        return true;
    }

    protected static Dictionary<string, T> Indice<T>(IEnumerable<(string Clave, T Valor)> pares)
    {
        var d = new Dictionary<string, T>();
        foreach (var (k, v) in pares)
        {
            var c = Clave(k);
            if (c.Length > 0) d.TryAdd(c, v);
        }
        return d;
    }
}

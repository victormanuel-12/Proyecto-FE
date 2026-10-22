using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using SPIn.Common.Excepciones;

namespace SPIn.Services.Importacion;

/// <summary>
/// Lee un .xlsx (hoja «Datos» o la primera) o un .csv (separado por ; o ,) y devuelve encabezados y filas como texto.
/// Las fechas se convierten a yyyy-MM-dd y los números a formato invariante, para que el importador los interprete igual.
/// </summary>
public static class LectorTabla
{
    public const int MaximoFilas = 20000;

    public static (List<string> Encabezados, List<(int Numero, List<string> Celdas)> Filas) Leer(Stream contenido, string nombreArchivo)
    {
        var extension = Path.GetExtension(nombreArchivo).ToLowerInvariant();
        return extension switch
        {
            ".xlsx" => LeerExcel(contenido),
            ".csv" or ".txt" => LeerCsv(contenido),
            ".xls" => throw new GenericException("El formato .xls (Excel 97-2003) no es compatible. En Excel use «Guardar como» → «Libro de Excel (.xlsx)»."),
            _ => throw new GenericException("Suba un archivo .xlsx o .csv.")
        };
    }

    private static (List<string>, List<(int, List<string>)>) LeerExcel(Stream contenido)
    {
        XLWorkbook libro;
        try { libro = new XLWorkbook(contenido); }
        catch (Exception) { throw new GenericException("No se pudo leer el archivo. Verifique que sea un Excel (.xlsx) válido y que no tenga contraseña."); }

        using (libro)
        {
            var hoja = libro.Worksheets.FirstOrDefault(h => h.Name.Trim().Equals("Datos", StringComparison.OrdinalIgnoreCase))
                       ?? libro.Worksheets.First();
            var usadas = hoja.RangeUsed();
            if (usadas is null) throw new GenericException("La hoja está vacía.");

            var primeraFila = usadas.FirstRow().RowNumber();
            var ultimaFila = usadas.LastRow().RowNumber();
            var ultimaColumna = usadas.LastColumn().ColumnNumber();
            if (ultimaFila - primeraFila > MaximoFilas)
                throw new GenericException($"El archivo supera el máximo de {MaximoFilas:#,0} filas. Divídalo en partes.");

            var encabezados = Enumerable.Range(1, ultimaColumna).Select(c => Texto(hoja.Cell(primeraFila, c))).ToList();
            var filas = new List<(int, List<string>)>();
            for (var f = primeraFila + 1; f <= ultimaFila; f++)
            {
                var celdas = Enumerable.Range(1, ultimaColumna).Select(c => Texto(hoja.Cell(f, c))).ToList();
                if (celdas.All(string.IsNullOrWhiteSpace)) continue;
                filas.Add((f, celdas));
            }
            return (encabezados, filas);
        }
    }

    private static string Texto(IXLCell celda)
    {
        try
        {
            var v = celda.Value;
            if (v.IsBlank) return string.Empty;
            if (v.IsDateTime) return v.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (v.IsNumber) return v.GetNumber().ToString("0.##########", CultureInfo.InvariantCulture);
            if (v.IsBoolean) return v.GetBoolean() ? "sí" : "no";
            if (v.IsText) return v.GetText().Trim();
            if (v.IsError) return "#ERROR";
            return celda.GetFormattedString().Trim();
        }
        catch (Exception)
        {
            return celda.GetFormattedString().Trim();
        }
    }

    private static (List<string>, List<(int, List<string>)>) LeerCsv(Stream contenido)
    {
        using var memoria = new MemoryStream();
        contenido.CopyTo(memoria);
        var bytes = memoria.ToArray();

        // UTF-8 (con o sin BOM); si no es UTF-8 válido, Windows-1252/Latin-1 (lo que guarda Excel en español).
        string texto;
        try { texto = new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException) { texto = Encoding.Latin1.GetString(bytes); }
        texto = texto.TrimStart('﻿');

        var lineas = DividirRegistros(texto);
        var primera = lineas.FindIndex(l => !string.IsNullOrWhiteSpace(l.Texto));
        if (primera < 0) throw new GenericException("El archivo está vacío.");
        if (lineas.Count - primera - 1 > MaximoFilas)
            throw new GenericException($"El archivo supera el máximo de {MaximoFilas:#,0} filas. Divídalo en partes.");

        var cabecera = lineas[primera].Texto;
        var separador = Contar(cabecera, ';') >= Contar(cabecera, ',') && Contar(cabecera, ';') > 0 ? ';'
                       : Contar(cabecera, '\t') > Contar(cabecera, ',') ? '\t' : ',';

        var encabezados = Campos(cabecera, separador);
        var filas = new List<(int, List<string>)>();
        foreach (var (numero, linea) in lineas.Skip(primera + 1))
        {
            if (string.IsNullOrWhiteSpace(linea)) continue;
            var celdas = Campos(linea, separador);
            if (celdas.All(string.IsNullOrWhiteSpace)) continue;
            filas.Add((numero, celdas));
        }
        return (encabezados, filas);
    }

    private static int Contar(string s, char c) => s.Count(x => x == c);

    /// <summary>Divide en registros respetando saltos de línea dentro de comillas. Devuelve el número de línea inicial.</summary>
    private static List<(int Numero, string Texto)> DividirRegistros(string texto)
    {
        var registros = new List<(int, string)>();
        var actual = new StringBuilder();
        var enComillas = false;
        var linea = 1;
        var inicio = 1;
        for (var i = 0; i < texto.Length; i++)
        {
            var c = texto[i];
            if (c == '"') enComillas = !enComillas;
            if ((c == '\n' || c == '\r') && !enComillas)
            {
                if (c == '\r' && i + 1 < texto.Length && texto[i + 1] == '\n') i++;
                registros.Add((inicio, actual.ToString()));
                actual.Clear();
                linea++;
                inicio = linea;
                continue;
            }
            if (c == '\n') linea++;
            actual.Append(c);
        }
        if (actual.Length > 0) registros.Add((inicio, actual.ToString()));
        return registros;
    }

    private static List<string> Campos(string linea, char separador)
    {
        var campos = new List<string>();
        var actual = new StringBuilder();
        var enComillas = false;
        for (var i = 0; i < linea.Length; i++)
        {
            var c = linea[i];
            if (enComillas)
            {
                if (c == '"' && i + 1 < linea.Length && linea[i + 1] == '"') { actual.Append('"'); i++; }
                else if (c == '"') enComillas = false;
                else actual.Append(c);
            }
            else if (c == '"') enComillas = true;
            else if (c == separador) { campos.Add(actual.ToString().Trim()); actual.Clear(); }
            else actual.Append(c);
        }
        campos.Add(actual.ToString().Trim());
        return campos;
    }
}

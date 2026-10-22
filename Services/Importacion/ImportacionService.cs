using ClosedXML.Excel;
using Microsoft.Extensions.Caching.Memory;
using SPIn.Common.Excepciones;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Response;
using SPIn.Repository;

namespace SPIn.Services.Importacion;

public interface IImportacionService
{
    List<TipoImportacionResponse> Tipos();
    Task<(byte[] Contenido, string NombreArchivo)> PlantillaAsync(string tipo);
    Task<GenericResponse<VistaPreviaImportacionResponse>> PrevisualizarAsync(string tipo, Stream archivo, string nombreArchivo, OpcionesImportacion opciones);
    Task<GenericResponse<ResultadoImportacionResponse>> ConfirmarAsync(string token);
}

/// <summary>
/// Importación en dos pasos:
/// 1) Vista previa: se lee el archivo y se valida fila por fila sin guardar nada. El archivo queda en memoria 30 minutos.
/// 2) Confirmación: se vuelve a validar (los datos pudieron cambiar) y se ejecuta todo en UNA transacción,
///    usando los mismos servicios que las pantallas. Si una fila falla, no se guarda ninguna.
/// </summary>
public class ImportacionService(
    IEnumerable<IImportador> importadores,
    IImportacionRepository repo,
    IPrediccionRepository prediccion,
    IMemoryCache cache,
    ITenantProvider tenant,
    ILogger<ImportacionService> logger) : IImportacionService
{
    public const long TamanoMaximo = 5 * 1024 * 1024;
    private const int MaximoErroresMostrados = 300;
    private static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(30);

    private IImportador Importador(string tipo) =>
        importadores.FirstOrDefault(i => i.Tipo == tipo) ?? throw GenericException.NoEncontrado("Tipo de importación no reconocido.");

    public List<TipoImportacionResponse> Tipos() => importadores.OrderBy(i => i.Orden).Select(i => new TipoImportacionResponse
    {
        Tipo = i.Tipo,
        Titulo = i.Titulo,
        Descripcion = i.Descripcion,
        Orden = i.Orden,
        Requisito = i.Requisito,
        Modos = i.Modos.ToList(),
        PideFecha = i.TextoFecha is not null,
        TextoFecha = i.TextoFecha,
        Columnas = i.Columnas.Select(c => new ColumnaImportacionResponse
        {
            Nombre = c.Nombre, Obligatoria = c.Obligatoria, Descripcion = c.Descripcion, Ejemplo = c.Ejemplo
        }).ToList()
    }).ToList();

    // ------------------------------------------------------------------ plantilla
    public async Task<(byte[] Contenido, string NombreArchivo)> PlantillaAsync(string tipo)
    {
        var imp = Importador(tipo);
        using var libro = new XLWorkbook();

        var datos = libro.Worksheets.Add("Datos");
        for (var c = 0; c < imp.Columnas.Count; c++)
        {
            var col = imp.Columnas[c];
            var celda = datos.Cell(1, c + 1);
            celda.Value = col.Nombre;
            celda.Style.Font.Bold = true;
            celda.Style.Font.FontColor = XLColor.White;
            celda.Style.Fill.BackgroundColor = col.Obligatoria ? XLColor.FromHtml("#0E3B4A") : XLColor.FromHtml("#4F6B75");
            celda.CreateComment().AddText(col.Descripcion);
        }
        for (var f = 0; f < imp.Ejemplos.Count; f++)
            for (var c = 0; c < imp.Ejemplos[f].Length; c++)
                datos.Cell(f + 2, c + 1).SetValue(imp.Ejemplos[f][c]);   // como texto: el lector acepta ambos
        datos.Range(2, 1, Math.Max(2, imp.Ejemplos.Count + 1), imp.Columnas.Count).Style.Font.FontColor = XLColor.FromHtml("#5B6770");
        datos.SheetView.FreezeRows(1);
        for (var c = 0; c < imp.Columnas.Count; c++)
        {
            var largo = imp.Ejemplos.Select(e => c < e.Length ? e[c].Length : 0).Append(imp.Columnas[c].Nombre.Length).Max();
            datos.Column(c + 1).Width = Math.Clamp(largo + 4, 12, 45);
        }

        var ins = libro.Worksheets.Add("Instrucciones");
        ins.Cell(1, 1).Value = $"Importar {imp.Titulo.ToLowerInvariant()}";
        ins.Cell(1, 1).Style.Font.Bold = true;
        ins.Cell(1, 1).Style.Font.FontSize = 14;
        ins.Cell(2, 1).Value = imp.Descripcion;
        var fila = 3;
        if (imp.Requisito is not null) ins.Cell(fila++, 1).Value = "Requisito: " + imp.Requisito;
        ins.Cell(fila++, 1).Value = "Las filas de ejemplo (en gris) son solo una guía: reemplácelas o bórrelas antes de subir el archivo.";
        ins.Cell(fila++, 1).Value = "Columnas en azul oscuro: obligatorias. Puede usar punto o coma como decimal, sin separador de miles.";
        fila++;
        string[] titulos = ["Columna", "Obligatoria", "Qué escribir", "Ejemplo"];
        for (var c = 0; c < titulos.Length; c++)
        {
            ins.Cell(fila, c + 1).Value = titulos[c];
            ins.Cell(fila, c + 1).Style.Font.Bold = true;
            ins.Cell(fila, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E6EEF0");
        }
        foreach (var col in imp.Columnas)
        {
            fila++;
            ins.Cell(fila, 1).Value = col.Nombre;
            ins.Cell(fila, 2).Value = col.Obligatoria ? "Sí" : "No";
            ins.Cell(fila, 3).Value = col.Descripcion;
            ins.Cell(fila, 4).SetValue(col.Ejemplo);
        }
        ins.Column(1).Width = 22;
        ins.Column(2).Width = 12;
        ins.Column(3).Width = 70;
        ins.Column(4).Width = 22;

        var valores = await imp.ValoresValidosAsync();
        if (valores.Count > 0)
        {
            var hoja = libro.Worksheets.Add("Valores válidos");
            for (var c = 0; c < valores.Count; c++)
            {
                var (titulo, lista) = valores[c];
                hoja.Cell(1, c + 1).Value = titulo;
                hoja.Cell(1, c + 1).Style.Font.Bold = true;
                hoja.Cell(1, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E6EEF0");
                if (lista.Count == 0) hoja.Cell(2, c + 1).Value = "(aún no hay registros)";
                for (var f = 0; f < lista.Count; f++) hoja.Cell(f + 2, c + 1).SetValue(lista[f]);
                hoja.Column(c + 1).Width = 40;
            }
        }

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return (memoria.ToArray(), $"plantilla-{imp.Tipo}.xlsx");
    }

    // ------------------------------------------------------------------ vista previa
    public async Task<GenericResponse<VistaPreviaImportacionResponse>> PrevisualizarAsync(string tipo, Stream archivo, string nombreArchivo, OpcionesImportacion opciones)
    {
        var imp = Importador(tipo);
        var empresaId = tenant.EmpresaId ?? throw GenericException.Prohibido("Su usuario no tiene una empresa asignada.");

        var (encabezados, crudas) = LectorTabla.Leer(archivo, nombreArchivo);
        var (filas, noReconocidas, faltantes) = Mapear(imp, encabezados, crudas);
        if (faltantes.Count > 0)
        {
            throw GenericException.Campo("archivo",
                $"Faltan columnas obligatorias: {string.Join(", ", faltantes)}. Descargue la plantilla para ver el formato.");
        }
        if (filas.Count == 0) throw GenericException.Campo("archivo", "El archivo no tiene filas con datos debajo de los encabezados.");

        var prep = await imp.PrepararAsync(filas, opciones);
        var token = Guid.NewGuid().ToString("N");
        cache.Set(Clave(token), new ArchivoPendiente(empresaId, tenant.UsuarioId ?? string.Empty, tipo, nombreArchivo, filas, opciones), Vigencia);

        var vista = new VistaPreviaImportacionResponse
        {
            Token = token,
            Tipo = tipo,
            Archivo = nombreArchivo,
            TotalFilas = filas.Count,
            FilasConError = prep.Errores.Select(e => e.Fila).Distinct().Count(),
            Nuevos = prep.Nuevos,
            Actualizados = prep.Actualizados,
            Omitidos = prep.Omitidos,
            Operaciones = prep.Pasos.Count,
            ColumnasNoReconocidas = noReconocidas,
            Errores = prep.Errores.OrderBy(e => e.Fila).Take(MaximoErroresMostrados).ToList(),
            Muestra = prep.Pasos.Take(8).Select(p => $"Fila {p.Fila}: {p.Descripcion}").ToList()
        };
        var mensaje = vista.FilasConError > 0
            ? $"{vista.FilasConError} fila(s) con errores. Corríjalas en el archivo y vuelva a subirlo."
            : vista.Operaciones == 0 ? "No hay nada que importar: todas las filas se omitirían." : "Archivo revisado sin errores.";
        return new GenericResponse<VistaPreviaImportacionResponse>(true, vista, mensaje);
    }

    private static (List<Fila> Filas, List<string> NoReconocidas, List<string> Faltantes) Mapear(
        IImportador imp, List<string> encabezados, List<(int Numero, List<string> Celdas)> crudas)
    {
        var porClave = new Dictionary<string, Columna>();
        foreach (var c in imp.Columnas)
        {
            porClave.TryAdd(ImportadorBase.Clave(c.Nombre), c);
            foreach (var a in c.Alias) porClave.TryAdd(ImportadorBase.Clave(a), c);
        }

        var indice = new Dictionary<int, string>();   // posición → clave de columna
        var noReconocidas = new List<string>();
        for (var i = 0; i < encabezados.Count; i++)
        {
            var k = ImportadorBase.Clave(encabezados[i]);
            if (k.Length == 0) continue;
            if (porClave.TryGetValue(k, out var col) && !indice.ContainsValue(ImportadorBase.Clave(col.Nombre)))
                indice[i] = ImportadorBase.Clave(col.Nombre);
            else noReconocidas.Add(encabezados[i]);
        }
        var faltantes = imp.Columnas.Where(c => c.Obligatoria && !indice.ContainsValue(ImportadorBase.Clave(c.Nombre))).Select(c => c.Nombre).ToList();

        var filas = crudas.Select(r =>
        {
            var valores = new Dictionary<string, string>();
            foreach (var (pos, clave) in indice)
                if (pos < r.Celdas.Count) valores[clave] = r.Celdas[pos];
            return new Fila(r.Numero, valores);
        }).Where(f => f.Valores.Values.Any(v => !string.IsNullOrWhiteSpace(v))).ToList();

        return (filas, noReconocidas, faltantes);
    }

    // ------------------------------------------------------------------ confirmación
    public async Task<GenericResponse<ResultadoImportacionResponse>> ConfirmarAsync(string token)
    {
        if (!cache.TryGetValue(Clave(token), out ArchivoPendiente? pendiente) || pendiente is null)
            throw GenericException.Conflicto("La vista previa venció (30 minutos) o ya se importó. Vuelva a subir el archivo.");
        if (pendiente.EmpresaId != tenant.EmpresaId || pendiente.Usuario != (tenant.UsuarioId ?? string.Empty))
            throw GenericException.Prohibido("Esta vista previa pertenece a otro usuario.");

        var imp = Importador(pendiente.Tipo);
        var resultado = await repo.EnTransaccionAsync(async () =>
        {
            var prep = await imp.PrepararAsync(pendiente.Filas, pendiente.Opciones);
            if (prep.Errores.Count > 0)
            {
                var e = prep.Errores.OrderBy(x => x.Fila).First();
                throw new GenericException($"Los datos cambiaron desde la vista previa. Fila {e.Fila} ({e.Columna}): {e.Mensaje} Vuelva a subir el archivo.");
            }
            foreach (var paso in prep.Pasos)
            {
                try { await paso.Ejecutar(); }
                catch (GenericException ex)
                {
                    var detalle = ex.Errores is { Count: > 0 } ? string.Join(" ", ex.Errores.Values.SelectMany(v => v)) : ex.Message;
                    throw new GenericException($"Fila {paso.Fila}: {detalle} No se importó ninguna fila.", ex.StatusCode == 404 ? 400 : ex.StatusCode);
                }
            }
            if (prep.AlFinal is not null) await prep.AlFinal();
            return new ResultadoImportacionResponse
            {
                Tipo = imp.Tipo, Nuevos = prep.Nuevos, Actualizados = prep.Actualizados, Omitidos = prep.Omitidos, Operaciones = prep.Pasos.Count
            };
        });

        cache.Remove(Clave(token));
        // Historial de ventas nuevo: el motor predictivo se reentrena en el siguiente minuto (Entrega 8).
        if (imp.Tipo == "ventas") await prediccion.MarcarReentrenoPendienteAsync();
        logger.LogInformation("Importación {Tipo} de {Archivo}: {Nuevos} nuevos, {Actualizados} actualizados, {Omitidos} omitidos",
            imp.Tipo, pendiente.Archivo, resultado.Nuevos, resultado.Actualizados, resultado.Omitidos);

        var partes = new List<string>();
        if (resultado.Nuevos > 0) partes.Add($"{resultado.Nuevos} nuevo(s)");
        if (resultado.Actualizados > 0) partes.Add($"{resultado.Actualizados} actualizado(s)");
        if (resultado.Omitidos > 0) partes.Add($"{resultado.Omitidos} omitido(s)");
        return new GenericResponse<ResultadoImportacionResponse>(true, resultado, $"Importación completada: {string.Join(", ", partes)}.");
    }

    private static string Clave(string token) => $"spin:importacion:{token}";
}

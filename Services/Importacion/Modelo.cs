using SPIn.Dtos.Response;

namespace SPIn.Services.Importacion;

/// <summary>Definición de una columna del archivo. Se reconoce sin importar mayúsculas, tildes, espacios ni guiones.</summary>
public record Columna(string Nombre, bool Obligatoria, string Descripcion, string Ejemplo, params string[] Alias);

/// <summary>Una fila del archivo ya leída: número de fila real (como la ve Excel) y valores por columna normalizada.</summary>
public class Fila(int numero, Dictionary<string, string> valores)
{
    public int Numero { get; } = numero;
    public Dictionary<string, string> Valores { get; } = valores;
}

public class OpcionesImportacion
{
    public string? Modo { get; set; }
    public DateOnly? Fecha { get; set; }
}

public class Paso(int fila, string descripcion, Func<Task> ejecutar)
{
    public int Fila { get; } = fila;
    public string Descripcion { get; } = descripcion;
    public Func<Task> Ejecutar { get; } = ejecutar;
}

/// <summary>Resultado de revisar el archivo: errores por fila y lista de operaciones a ejecutar al confirmar.</summary>
public class Preparacion
{
    public List<ErrorImportacionResponse> Errores { get; } = [];
    public List<Paso> Pasos { get; } = [];
    public int Nuevos { get; set; }
    public int Actualizados { get; set; }
    public int Omitidos { get; set; }
    /// <summary>Se ejecuta después de todos los pasos (por ejemplo, guardar en bloque).</summary>
    public Func<Task>? AlFinal { get; set; }

    public void Error(int fila, string columna, string mensaje) =>
        Errores.Add(new ErrorImportacionResponse { Fila = fila, Columna = columna, Mensaje = mensaje });
}

/// <summary>Archivo leído y guardado en memoria entre la vista previa y la confirmación.</summary>
public record ArchivoPendiente(int EmpresaId, string Usuario, string Tipo, string Archivo, List<Fila> Filas, OpcionesImportacion Opciones);

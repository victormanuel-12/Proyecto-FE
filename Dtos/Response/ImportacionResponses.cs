namespace SPIn.Dtos.Response;

public class ColumnaImportacionResponse
{
    public string Nombre { get; set; } = string.Empty;
    public bool Obligatoria { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Ejemplo { get; set; } = string.Empty;
}

public class TipoImportacionResponse
{
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    /// <summary>Orden sugerido de carga (1 = primero).</summary>
    public int Orden { get; set; }
    public string? Requisito { get; set; }
    /// <summary>Modos posibles cuando el registro ya existe (valor, texto). El primero es el predeterminado.</summary>
    public List<OpcionTextoResponse> Modos { get; set; } = [];
    public bool PideFecha { get; set; }
    public string? TextoFecha { get; set; }
    public List<ColumnaImportacionResponse> Columnas { get; set; } = [];
}

public class OpcionTextoResponse
{
    public string Valor { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
}

public class ErrorImportacionResponse
{
    public int Fila { get; set; }
    public string Columna { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}

public class VistaPreviaImportacionResponse
{
    public string Token { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Archivo { get; set; } = string.Empty;
    public int TotalFilas { get; set; }
    public int FilasConError { get; set; }
    public int Nuevos { get; set; }
    public int Actualizados { get; set; }
    public int Omitidos { get; set; }
    /// <summary>Total de operaciones que se ejecutarán (en ventas se agrupan filas del mismo día, producto y canal).</summary>
    public int Operaciones { get; set; }
    public bool PuedeConfirmar => FilasConError == 0 && Operaciones > 0;
    public List<string> ColumnasNoReconocidas { get; set; } = [];
    public List<ErrorImportacionResponse> Errores { get; set; } = [];
    /// <summary>Primeras operaciones, para que el usuario confirme que se leyó bien.</summary>
    public List<string> Muestra { get; set; } = [];
}

public class ResultadoImportacionResponse
{
    public string Tipo { get; set; } = string.Empty;
    public int Nuevos { get; set; }
    public int Actualizados { get; set; }
    public int Omitidos { get; set; }
    public int Operaciones { get; set; }
}

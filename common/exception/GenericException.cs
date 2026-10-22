namespace SPIn.Common.Excepciones;

/// <summary>
/// Error de negocio que se muestra al usuario tal cual, con su código HTTP.
/// Si trae <see cref="Errores"/>, el frontend marca esos campos del formulario.
/// </summary>
public class GenericException(string message, int statusCode = StatusCodes.Status400BadRequest) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    /// <summary>Errores por campo (clave en camelCase, igual al atributo name del formulario).</summary>
    public IReadOnlyDictionary<string, string[]>? Errores { get; private init; }

    public static GenericException NoEncontrado(string mensaje) => new(mensaje, StatusCodes.Status404NotFound);
    public static GenericException Conflicto(string mensaje) => new(mensaje, StatusCodes.Status409Conflict);
    public static GenericException Prohibido(string mensaje) => new(mensaje, StatusCodes.Status403Forbidden);

    /// <summary>Error de validación de negocio asociado a un campo del formulario.</summary>
    public static GenericException Campo(string campo, string mensaje) =>
        new("Revise los datos ingresados.")
        {
            Errores = new Dictionary<string, string[]> { [campo] = [mensaje] }
        };
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SPIn.Dtos.Response;

namespace SPIn.Common.Excepciones;

/// <summary>
/// Si el modelo recibido por un controlador API no es válido, responde 400 con los errores por campo.
/// Las claves se devuelven en camelCase para que coincidan con el atributo name de los formularios.
/// </summary>
public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var esApi = context.ActionDescriptor.EndpointMetadata.OfType<ApiControllerAttribute>().Any();
        if (!esApi || context.ModelState.IsValid) return;

        var errores = new Dictionary<string, string[]>();
        foreach (var (clave, entrada) in context.ModelState)
        {
            if (entrada.Errors.Count == 0) continue;
            var campo = NormalizarClave(clave);

            // Errores de una fila de una lista (ej.: Detalles[2].Cantidad): se agrupan en "detalles" indicando la fila.
            var fila = System.Text.RegularExpressions.Regex.Match(clave, @"^\$?\.?(\w+)\[(\d+)\]\.");
            var prefijo = string.Empty;
            if (fila.Success)
            {
                campo = char.ToLowerInvariant(fila.Groups[1].Value[0]) + fila.Groups[1].Value[1..];
                prefijo = $"Fila {int.Parse(fila.Groups[2].Value) + 1}: ";
            }
            // Los errores de conversión del JSON (clave "$.campo" o con excepción) traen mensajes técnicos en inglés:
            // se reemplazan por un mensaje comprensible.
            var esErrorDeFormato = clave.StartsWith('$');
            var mensajes = entrada.Errors
                .Select(e => esErrorDeFormato || e.Exception is not null || string.IsNullOrWhiteSpace(e.ErrorMessage)
                    ? "El valor ingresado no tiene un formato válido."
                    : e.ErrorMessage)
                .Select(m => prefijo + m)
                .Distinct()
                .ToArray();
            errores[campo] = errores.TryGetValue(campo, out var previos) ? [.. previos, .. mensajes] : mensajes;
        }

        context.Result = new BadRequestObjectResult(
            new GenericResponse<Dictionary<string, string[]>>(false, errores, "Revise los datos ingresados."));
    }

    public void OnActionExecuted(ActionExecutedContext context) { }

    private static string NormalizarClave(string clave)
    {
        if (string.IsNullOrEmpty(clave) || clave == "$") return "general";
        var ultimo = clave.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? clave;
        ultimo = ultimo.TrimStart('$');
        var corchete = ultimo.IndexOf('[');
        if (corchete > 0) ultimo = ultimo[..corchete];
        if (string.IsNullOrEmpty(ultimo)) return "general";
        return char.ToLowerInvariant(ultimo[0]) + ultimo[1..];
    }
}

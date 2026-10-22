using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SPIn.Dtos.Response;

namespace SPIn.Common.Excepciones;

/// <summary>
/// Convierte las excepciones de los controladores API en un GenericResponse.
/// Nunca devuelve detalles internos al navegador; el detalle queda solo en el log.
/// </summary>
public class GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var esApi = context.ActionDescriptor.EndpointMetadata.OfType<ApiControllerAttribute>().Any();
        if (!esApi) return;

        int status;
        string mensaje;
        IReadOnlyDictionary<string, string[]>? errores = null;

        switch (context.Exception)
        {
            case GenericException ge:
                status = ge.StatusCode;
                mensaje = ge.Message;
                errores = ge.Errores;
                break;
            case DbUpdateException dbe:
                logger.LogWarning(dbe, "Conflicto al guardar en la base de datos en {Ruta}", context.HttpContext.Request.Path);
                status = StatusCodes.Status409Conflict;
                mensaje = "No se pudo guardar porque los datos entran en conflicto con otro registro (por ejemplo, un código repetido).";
                break;
            default:
                logger.LogError(context.Exception, "Error no controlado en {Ruta}", context.HttpContext.Request.Path);
                status = StatusCodes.Status500InternalServerError;
                mensaje = "Ocurrió un error inesperado. Intente nuevamente; si persiste, comuníquese con el administrador.";
                break;
        }

        object cuerpo = errores is null
            ? new GenericResponse<object>(false, mensaje)
            : new GenericResponse<IReadOnlyDictionary<string, string[]>>(false, errores, mensaje);
        context.Result = new ObjectResult(cuerpo) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}

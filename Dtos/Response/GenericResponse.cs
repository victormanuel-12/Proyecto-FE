namespace SPIn.Dtos.Response;

/// <summary>Formato único de respuesta de la API (igual que en SGMG).</summary>
public class GenericResponse<T>
{
    public bool Success { get; set; } = true;
    public string? Message { get; set; }
    public T? Data { get; set; }

    public GenericResponse() { }

    public GenericResponse(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public GenericResponse(bool success, T data, string message)
    {
        Success = success;
        Data = data;
        Message = message;
    }

    public GenericResponse(T data)
    {
        Data = data;
    }
}

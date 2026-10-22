namespace SPIn.Repository;

/// <summary>Consultas de apoyo para la importación: catálogos indexados por código o nombre.</summary>
public interface IImportacionRepository
{
    Task<List<(int Id, string Nombre)>> CategoriasAsync();
    Task<List<(int Id, string Codigo, string Nombre)>> UnidadesAsync();
    Task<List<(int Id, string Codigo, string Nombre, bool Activo)>> InsumosAsync();
    Task<List<(int Id, string? Ruc, string RazonSocial, bool Activo)>> ProveedoresAsync();
    Task<List<(int Id, int InsumoId, int ProveedorId)>> CondicionesAsync();
    Task<List<(int Id, string Nombre)>> FormatosAsync();
    Task<List<(int Id, string Codigo, string Nombre, int UnidadesPorPaquete, bool Activo)>> ProductosAsync();
    Task<List<(int Id, string Codigo, Models.TipoAlmacen Tipo, bool Activo)>> AlmacenesAsync();
    Task<HashSet<(int InsumoId, int AlmacenId)>> InsumosConMovimientosAsync();
    Task<HashSet<(int ProductoId, int AlmacenId)>> ProductosConMovimientosAsync();
    Task<HashSet<int>> ProductosConRecetaAsync();
    /// <summary>Insumos que no se consumen por producción (no pueden ir en una receta).</summary>
    Task<HashSet<int>> InsumosNoProduccionAsync();
    /// <summary>Día + producto + canal que ya tienen historial (ventas sin descuento de stock, no anuladas).</summary>
    Task<HashSet<(DateOnly Fecha, int ProductoId, Models.CanalVenta Canal)>> ClavesHistorialVentasAsync(DateOnly desde, DateOnly hasta);
    Task<T> EnTransaccionAsync<T>(Func<Task<T>> accion);
}

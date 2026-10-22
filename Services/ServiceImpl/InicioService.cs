using SPIn.Common;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Response;
using SPIn.Repository;

namespace SPIn.Services.ServiceImpl;

public class InicioService(
    IPlantaRepository plantas,
    IAlmacenRepository almacenes,
    ILineaRepository lineas,
    IUsuarioRepository usuarios,
    IEmpresaRepository empresas,
    IInsumoRepository insumos,
    IProveedorRepository proveedores,
    IProductoRepository productos,
    IInventarioRepository inventario,
    IVentaRepository ventas,
    ITenantProvider tenant) : IInicioService
{
    public async Task<GenericResponse<ResumenInicioResponse>> ResumenAsync()
    {
        var empresaId = tenant.EmpresaId ?? 0;
        var empresa = await empresas.ObtenerAsync(empresaId);

        return new GenericResponse<ResumenInicioResponse>(new ResumenInicioResponse
        {
            EmpresaNombre = empresa?.NombreComercial ?? empresa?.RazonSocial ?? string.Empty,
            Plantas = await plantas.ContarAsync(),
            Almacenes = await almacenes.ContarAsync(),
            Lineas = await lineas.ContarAsync(),
            Usuarios = await usuarios.ContarActivosAsync(empresaId),
            Insumos = await insumos.ContarAsync(),
            Proveedores = await proveedores.ContarAsync(),
            ProductosConReceta = await productos.ContarConRecetaAsync(Reloj.Hoy),
            InsumosConStock = await inventario.InsumosConStockAsync(),
            ProductosConVentas = await ventas.ProductosConVentasAsync()
        });
    }
}

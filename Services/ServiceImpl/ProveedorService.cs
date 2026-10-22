using System.Globalization;
using System.Text;
using SPIn.Common;
using SPIn.Common.Excepciones;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;
using SPIn.Repository.RepositoryImpl;

namespace SPIn.Services.ServiceImpl;

public class ProveedorService(IProveedorRepository repo, IPedidoRepository pedidos, ILogger<ProveedorService> logger) : IProveedorService
{
    public async Task<GenericResponse<ListadoResponse<ProveedorResponse>>> ListarAsync(FiltroProveedorRequest filtro) =>
        new(await repo.ListarAsync(filtro));

    public async Task<GenericResponse<List<OpcionResponse>>> OpcionesAsync(bool soloActivos) =>
        new(await repo.OpcionesAsync(soloActivos));

    public async Task<GenericResponse<ProveedorResponse>> ObtenerAsync(int id) =>
        new(await repo.ObtenerResumenAsync(id) ?? throw GenericException.NoEncontrado("El proveedor no existe."));

    public async Task<GenericResponse<ProveedorResponse>> CrearAsync(ProveedorRequest request)
    {
        var proveedor = new Proveedor { Activo = true };
        await CopiarYValidarAsync(request, proveedor, null);
        repo.Agregar(proveedor);
        await repo.GuardarAsync();
        logger.LogInformation("Proveedor {ProveedorId} creado", proveedor.Id);
        return new GenericResponse<ProveedorResponse>(true, (await repo.ObtenerResumenAsync(proveedor.Id))!, "Proveedor registrado.");
    }

    public async Task<GenericResponse<ProveedorResponse>> ActualizarAsync(int id, ProveedorRequest request)
    {
        var proveedor = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("El proveedor no existe.");
        await CopiarYValidarAsync(request, proveedor, id);
        await repo.GuardarAsync();
        return new GenericResponse<ProveedorResponse>(true, (await repo.ObtenerResumenAsync(id))!, "Proveedor actualizado.");
    }

    public async Task<GenericResponse<ProveedorResponse>> CambiarEstadoAsync(int id, bool activo)
    {
        var proveedor = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("El proveedor no existe.");
        proveedor.Activo = activo;
        await repo.GuardarAsync();

        var mensaje = "Proveedor activado.";
        if (!activo)
        {
            var principales = await repo.ContarInsumosPrincipalAsync(id);
            mensaje = principales == 0
                ? "Proveedor desactivado."
                : $"Proveedor desactivado. Era el proveedor principal de {principales} insumo(s): asígneles otro desde Insumos.";
        }
        return new GenericResponse<ProveedorResponse>(true, (await repo.ObtenerResumenAsync(id))!, mensaje);
    }

    public async Task<GenericResponse<object>> EliminarAsync(int id)
    {
        var proveedor = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("El proveedor no existe.");
        if (await repo.TieneCondicionesAsync(id))
            throw GenericException.Conflicto("No se puede eliminar: el proveedor tiene insumos asignados. Puede desactivarlo.");
        if (await pedidos.ProveedorUsadoAsync(id))
            throw GenericException.Conflicto("No se puede eliminar: el proveedor tiene pedidos. Puede desactivarlo.");
        repo.Eliminar(proveedor);
        await repo.GuardarAsync();
        return new GenericResponse<object>(true, "Proveedor eliminado.");
    }

    private async Task CopiarYValidarAsync(ProveedorRequest r, Proveedor p, int? id)
    {
        var pais = NormalizarPais(r.Pais);
        string? ruc = null;

        if (pais == ProveedorRepositoryImpl.PaisLocal)
        {
            ruc = r.Ruc?.Trim();
            if (string.IsNullOrEmpty(ruc))
                throw GenericException.Campo("ruc", "El RUC es obligatorio para proveedores del Perú.");
            if (!Validaciones.RucValido(ruc))
                throw GenericException.Campo("ruc", "El RUC no es válido: revise los 11 dígitos.");
            if (await repo.ExisteRucAsync(ruc, id))
                throw GenericException.Campo("ruc", "Ya existe un proveedor con ese RUC.");
        }

        p.Pais = pais;
        p.Ruc = ruc;
        p.RazonSocial = r.RazonSocial.Trim();
        p.NombreComercial = Validaciones.TextoOpcional(r.NombreComercial);
        p.Contacto = Validaciones.TextoOpcional(r.Contacto);
        p.Telefono = Validaciones.TextoOpcional(r.Telefono);
        p.Email = Validaciones.TextoOpcional(r.Email)?.ToLowerInvariant();
    }

    /// <summary>"peru", "PERÚ" o "Perú " se guardan como "Perú"; el resto con mayúscula inicial.</summary>
    private static string NormalizarPais(string pais)
    {
        var limpio = pais.Trim();
        var sinTildes = new string(limpio.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        if (string.Equals(sinTildes, "peru", StringComparison.OrdinalIgnoreCase))
            return ProveedorRepositoryImpl.PaisLocal;
        return char.ToUpperInvariant(limpio[0]) + limpio[1..];
    }
}

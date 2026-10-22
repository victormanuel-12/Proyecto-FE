using SPIn.Common;
using SPIn.Common.Excepciones;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;

namespace SPIn.Services.ServiceImpl;

public class EmpresaService(IEmpresaRepository repo, ITenantProvider tenant, ILogger<EmpresaService> logger) : IEmpresaService
{
    public async Task<GenericResponse<ListadoResponse<EmpresaResponse>>> ListarAsync(FiltroListadoRequest filtro) =>
        new(await repo.ListarAsync(filtro));

    public async Task<GenericResponse<List<OpcionResponse>>> OpcionesAsync() =>
        new(await repo.OpcionesAsync());

    public async Task<GenericResponse<EmpresaResponse>> ObtenerAsync(int id) =>
        new(await repo.ObtenerResumenAsync(id) ?? throw GenericException.NoEncontrado("La empresa no existe."));

    public async Task<GenericResponse<EmpresaResponse>> CrearAsync(EmpresaRequest request)
    {
        var ruc = request.Ruc.Trim();
        ValidarRuc(ruc);
        if (await repo.ExisteRucAsync(ruc, null))
            throw GenericException.Conflicto("Ya existe una empresa con ese RUC.");

        var empresa = new Empresa
        {
            Ruc = ruc,
            RazonSocial = request.RazonSocial.Trim(),
            NombreComercial = Validaciones.TextoOpcional(request.NombreComercial),
            Activo = true
        };
        repo.Agregar(empresa);
        await repo.GuardarAsync();
        await repo.CrearCatalogoInicialAsync(empresa.Id);
        logger.LogInformation("Empresa {EmpresaId} creada por {Usuario}", empresa.Id, tenant.UsuarioNombre);

        return new GenericResponse<EmpresaResponse>(true, (await repo.ObtenerResumenAsync(empresa.Id))!, "Empresa registrada.");
    }

    public async Task<GenericResponse<EmpresaResponse>> ActualizarAsync(int id, EmpresaRequest request)
    {
        var empresa = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("La empresa no existe.");
        var ruc = request.Ruc.Trim();
        ValidarRuc(ruc);
        if (await repo.ExisteRucAsync(ruc, id))
            throw GenericException.Conflicto("Ya existe otra empresa con ese RUC.");

        empresa.Ruc = ruc;
        empresa.RazonSocial = request.RazonSocial.Trim();
        empresa.NombreComercial = Validaciones.TextoOpcional(request.NombreComercial);
        await repo.GuardarAsync();

        return new GenericResponse<EmpresaResponse>(true, (await repo.ObtenerResumenAsync(id))!, "Empresa actualizada.");
    }

    public async Task<GenericResponse<EmpresaResponse>> CambiarEstadoAsync(int id, bool activo)
    {
        var empresa = await repo.ObtenerAsync(id) ?? throw GenericException.NoEncontrado("La empresa no existe.");
        if (!activo && empresa.Id == tenant.EmpresaId)
            throw new GenericException("No puede desactivar la empresa a la que pertenece su usuario.");

        empresa.Activo = activo;
        await repo.GuardarAsync();
        logger.LogInformation("Empresa {EmpresaId} {Estado} por {Usuario}", id, activo ? "activada" : "desactivada", tenant.UsuarioNombre);

        var mensaje = activo
            ? "Empresa activada."
            : "Empresa desactivada. Sus usuarios ya no podrán iniciar sesión.";
        return new GenericResponse<EmpresaResponse>(true, (await repo.ObtenerResumenAsync(id))!, mensaje);
    }

    private static void ValidarRuc(string ruc)
    {
        if (!Validaciones.RucValido(ruc))
            throw new GenericException("El RUC no es válido: revise los 11 dígitos (debe empezar con 10, 15, 17 o 20).");
    }
}

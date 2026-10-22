using SPIn.Common.Seguridad;
using SPIn.Repository;

namespace SPIn.Services.Prediccion;

/// <summary>Revisa las alertas de cada empresa cada 10 minutos, aunque nadie tenga el sistema abierto (Entrega 9).</summary>
public class AlertasProgramadas(IServiceScopeFactory scopes, ILogger<AlertasProgramadas> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken detener)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(45), detener); } catch (OperationCanceledException) { return; }
        while (!detener.IsCancellationRequested)
        {
            try
            {
                List<int> empresas;
                using (var scope = scopes.CreateScope())
                    empresas = await scope.ServiceProvider.GetRequiredService<IPrediccionRepository>().EmpresasActivasAsync();
                foreach (var empresaId in empresas)
                {
                    using var scope = scopes.CreateScope();
                    scope.ServiceProvider.GetRequiredService<TenantProvider>().Establecer(empresaId, "sistema");
                    try { await scope.ServiceProvider.GetRequiredService<IAlertaService>().SincronizarAsync(forzar: true); }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogWarning(ex, "No se pudieron revisar las alertas de la empresa {EmpresaId}", empresaId);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error en la revisión programada de alertas");
            }
            try { await Task.Delay(Intervalo, detener); } catch (OperationCanceledException) { return; }
        }
    }
}

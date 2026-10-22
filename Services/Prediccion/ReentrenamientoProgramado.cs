using SPIn.Common.Seguridad;
using SPIn.Repository;

namespace SPIn.Services.Prediccion;

/// <summary>
/// Servicio en segundo plano que revisa cada minuto, empresa por empresa, si toca reentrenar:
/// - al iniciar el sistema, si el último entrenamiento es anterior a la última fecha programada;
/// - cada lunes (o cada día) a la hora configurada;
/// - después de importar ventas o de cambiar los parámetros del modelo.
/// Cada empresa se procesa en su propio scope, con su empresa fijada, para respetar el filtro multiempresa.
/// </summary>
public class ReentrenamientoProgramado(IServiceScopeFactory scopes, ILogger<ReentrenamientoProgramado> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken detener)
    {
        // Deja que la aplicación termine de iniciar (migraciones y carga inicial).
        try { await Task.Delay(TimeSpan.FromSeconds(20), detener); } catch (OperationCanceledException) { return; }

        var primeraVuelta = true;
        while (!detener.IsCancellationRequested)
        {
            try
            {
                await RevisarAsync(primeraVuelta, detener);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error al revisar el reentrenamiento programado");
            }
            primeraVuelta = false;
            try { await Task.Delay(Intervalo, detener); } catch (OperationCanceledException) { return; }
        }
    }

    private async Task RevisarAsync(bool alIniciar, CancellationToken detener)
    {
        List<int> empresas;
        using (var scope = scopes.CreateScope())
        {
            empresas = await scope.ServiceProvider.GetRequiredService<IPrediccionRepository>().EmpresasActivasAsync();
        }

        foreach (var empresaId in empresas)
        {
            if (detener.IsCancellationRequested) return;
            using var scope = scopes.CreateScope();
            scope.ServiceProvider.GetRequiredService<TenantProvider>().Establecer(empresaId, "sistema");
            try
            {
                var servicio = scope.ServiceProvider.GetRequiredService<IPrediccionService>();
                if (await servicio.EntrenarSiCorrespondeAsync(alIniciar))
                    logger.LogInformation("Pronóstico reentrenado automáticamente para la empresa {EmpresaId}", empresaId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "No se pudo reentrenar el pronóstico de la empresa {EmpresaId}", empresaId);
            }
        }
    }
}

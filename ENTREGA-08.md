# Entrega 8: Motor predictivo y configuración

**Fecha:** 2026-10-21 · **Destino:** .NET 10 · **Paquetes nuevos:** `Microsoft.ML 5.0.0` y `Microsoft.ML.TimeSeries 5.0.0` (ML.NET de Microsoft, licencia MIT, gratuito). Funcionan dentro de la aplicación: no se usa ningún servicio en la nube ni clave de API.

## Qué «IA» usa y cómo decide

| Paso | Qué hace |
|---|---|
| 1. Serie semanal | Por cada producto suma los paquetes vendidos de lunes a domingo (ventas no anuladas, importadas o registradas). La semana en curso no se usa porque está incompleta. |
| 2. Cuatro métodos candidatos | **SSA (Singular Spectrum Analysis) de ML.NET**: el modelo de aprendizaje automático, que separa la serie en tendencia y ciclos y los proyecta con un intervalo de confianza. **Holt**: suavizamiento exponencial con tendencia. **Promedio móvil** de 8 semanas. **Estacional**: la misma semana del año anterior, ajustada al nivel actual (requiere más de un año de historia). |
| 3. Validación | Reserva las últimas semanas reales (8 por defecto), entrena cada método con las anteriores y compara lo que pronosticó con lo que de verdad se vendió: **MAPE** (error porcentual promedio). |
| 4. Elección | Usa el método con **menor MAPE** y lo reentrena con toda la historia para pronosticar las próximas semanas (12 por defecto), con su rango probable al 90 %. Si un producto tiene poca historia, usa el promedio de lo disponible y lo indica. |
| 5. Reposición | Pronóstico de paquetes × receta vigente (con merma) = consumo diario de cada insumo. Con el stock, lo que está en camino, el tiempo de entrega del proveedor (± su variación) y el nivel de servicio del insumo calcula **stock de seguridad**, **punto de reorden**, **fecha de agotamiento**, **fecha límite para pedir** y **cantidad sugerida** (ajustada al pedido mínimo y al múltiplo). |

Fórmulas de la reposición (se muestran con los números de cada insumo en la pantalla):

```
Stock de seguridad = z × √(L × σd² + d² × σL²)      z según el nivel de servicio (97 % → 1.881)
Punto de reorden   = d × L + stock de seguridad
Posición           = stock disponible + en camino
Cantidad sugerida  = (punto de reorden + d × días de revisión) − posición, redondeada al múltiplo y al pedido mínimo
```

Es honesto con el resultado: si con sus datos un método simple acierta más que SSA, elige el método simple y lo muestra en la comparación. Con la historia de ejemplo (14 meses), el promedio móvil gana (MAPE ≈ 9 % frente a ≈ 12 % de SSA); con dos años o más, SSA y el método estacional pueden captar el verano y las fiestas.

## Cuándo se reentrena

| Momento | Qué pasa |
|---|---|
| Cada lunes a las 6:00 (hora de Perú) | Reentrenamiento automático (configurable: semanal, diario o solo manual, y la hora) |
| Al iniciar SPIn | Si el último entrenamiento es anterior a la última fecha programada (por ejemplo, la PC estaba apagada el lunes) |
| En el minuto siguiente a importar ventas | El historial nuevo se aprovecha de inmediato |
| Al cambiar el horizonte, la validación o la historia mínima | Se recalcula con los nuevos parámetros |
| Botón «Recalcular pronóstico» | Cuando usted quiera (Planificador, Compras, Administrador) |

Registrar partes de producción, recepciones, movimientos o pedidos **no reentrena**: la reposición se recalcula al instante en cada consulta con el stock del momento y el pronóstico vigente.

## Pantallas nuevas

| Pantalla | Para qué |
|---|---|
| **Planificar → Pronóstico de demanda** | Estado del motor (último entrenamiento, origen, duración, error promedio ponderado, próximo reentrenamiento, avisos). Productos con método elegido, MAPE con calificación (bueno / aceptable / bajo), historia, promedio reciente y próximas 4 semanas. Al pulsar un producto: **gráfico** con ventas reales, validación (lo que el modelo habría pronosticado), pronóstico y rango del 90 %; comparación de los 4 métodos y tabla semanal. Historial de los últimos entrenamientos. |
| **Planificar → Reposición de insumos** | Insumos **urgentes** (se agotan antes de que llegue un pedido), **por pedir** (bajo el punto de reorden), cubiertos (con fecha límite para pedir) y sin datos. Stock y en camino, consumo diario y de dónde sale, cobertura en días, punto de reorden, cantidad sugerida con costo. «Ver cómo se calculó» muestra cada fórmula con sus números. **Crear pedido** abre el pedido ya lleno con proveedor, insumo y cantidad. |
| **Administración → Configuración** | Semanas a pronosticar, de validación e historia mínima; frecuencia y hora del reentrenamiento; días entre compras; tiempo de entrega por defecto; **umbral del conteo físico** (antes fijo en 5 %). |

## Cómo aplicarla

El zip trae el **proyecto completo** (Entregas 0 a 8).

- **Si ya tiene la Entrega 7 funcionando:**
  1. Reemplace los archivos por los de este zip, **conservando** su carpeta `Data/Migrations` y su `.env`.
  2. `.\scripts\migracion.ps1 -Nombre Entrega08` (crea 4 tablas).
  3. `docker compose up --build -d` (o `dotnet run`). La primera compilación descarga ML.NET (unos 30 MB). La imagen de Docker instala `libgomp1`, que ML.NET necesita en Linux.
- **Si empieza de cero:** siga el README. Basta una migración, por ejemplo `Inicial`.

## Archivos

**Nuevos**
```
Models/Prediccion.cs                                ConfiguracionEmpresa, EjecucionPronostico, PronosticoProducto, PronosticoSemana
Dtos/Request/PrediccionRequests.cs, Dtos/Response/PrediccionResponses.cs
Repository/IPrediccionRepository.cs, RepositoryImpl/PrediccionRepositoryImpl.cs
Services/Prediccion/MetodosPronostico.cs            SSA (ML.NET), Holt, promedio móvil, estacional, MAPE, z del nivel de servicio
Services/Prediccion/MotorPronostico.cs              validación y elección del método
Services/Prediccion/ReentrenamientoProgramado.cs    servicio en segundo plano (revisa cada minuto)
Services/InterfacesPrediccion.cs, ServiceImpl/PrediccionService.cs, ServiceImpl/ReposicionService.cs
Controllers/Api/PrediccionApiControllers.cs         /api/pronostico, /api/reposicion, /api/configuracion
Views/Pronostico, Views/Reposicion, Views/Configuracion
wwwroot/js/pronostico.js, reposicion.js, configuracion.js
ENTREGA-08.md
```

**Modificados**
```
SPIn.csproj                          + Microsoft.ML y Microsoft.ML.TimeSeries 5.0.0
Dockerfile                           + libgomp1 (Intel MKL de ML.NET en Linux)
Data/ApplicationDbContext.cs         + 4 tablas con filtro por empresa
common/seguridad/TenantProvider.cs   la tarea en segundo plano fija la empresa de cada entrenamiento
common/seguridad/Roles.cs            + Pronostico
Program.cs                           + servicios, repositorio y servicio en segundo plano
Services/ServiceImpl/ConteoService.cs   umbral desde Configuración
Services/Importacion/ImportacionService.cs   reentrena tras importar ventas
wwwroot/js/pedidos.js                pedido prellenado desde Reposición
Views/Shared/_Layout.cshtml, _Iconos.cshtml   menú Planificar y Configuración; 3 iconos
Views/Home/Index.cshtml, wwwroot/css/spin.css, README.md
ejemplos/06-stock-insumos.xlsx       stock de preformas y CO₂ más bajo, para que la demostración muestre insumos por pedir
```

## Verificación

| Verificación | Estado |
|---|---|
| Sintaxis de todos los archivos C# (tree-sitter) | Hecha: 0 errores |
| API de ML.NET 5.0.0 contrastada con su código fuente (`ForecastBySsa`, `CreateTimeSeriesEngine`, regla «serie > 2 × ventana», dependencia de Intel MKL) | Hecha |
| Lógica del motor replicada en Python y ejecutada con el historial de ejemplo: selección por MAPE, banda del 90 %, z del nivel de servicio (igual a la normal estándar hasta 1e-6), stock de seguridad, punto de reorden y cantidad sugerida | Hecha: sin valores no numéricos; MAPE 9.1 % y 9.2 % |
| Navegador, Entrega 8: 34 comprobaciones (estado del motor, recalcular, fallo informado sin perder el último pronóstico, gráfico con 4 capas, comparación de métodos, reposición por situación, cálculo explicado, pedido prellenado, configuración con validación, 3 pantallas en móvil) | Hecha: 34 de 34 |
| Regresión de las Entregas 1 a 7 | Hecha: 22, 34, 26, 34, 49 y 50 |
| `dotnet build`, migración y **entrenamiento real con ML.NET** | **Pendiente: lo ejecuta usted** (este entorno no tiene acceso a NuGet ni al SDK). Si SSA no pudiera ejecutarse en su equipo, el motor sigue con los otros métodos y lo avisa en «Pronóstico de demanda»: el sistema no se detiene. |

## Prueba manual sugerida (15 minutos)

1. Importe los archivos 01 a 08 de `ejemplos/` (cree antes `LUR-INS` y `LUR-PT`). Al minuto de importar las ventas, el pronóstico se entrena solo.
2. **Pronóstico de demanda:** revise el método elegido y el MAPE de cada producto; abra Cola 500 ml y vea el gráfico y la comparación de métodos. Pulse **Recalcular pronóstico** y revise el historial.
3. **Reposición de insumos:** las preformas de 500 ml deben salir como **urgentes** y el CO₂ por pedir. Pulse «Ver cómo se calculó» y luego **Crear pedido**.
4. Guarde el pedido y vuelva a Reposición: la posición incluye lo que está en camino y la situación cambia.
5. Registre un **parte de producción** y vuelva a Reposición: el stock y la cobertura bajan al instante.
6. **Configuración:** cambie el horizonte a 16 semanas y guarde; al minuto se reentrena. Cambie el umbral del conteo a 10 % y compruébelo en un conteo físico.

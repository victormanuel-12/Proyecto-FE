# Entrega 9: Alertas y panel

**Fecha:** 2026-10-21 · **Destino:** .NET 10 · **Paquetes:** sin cambios (no se agregó ninguno).

## Qué se puede hacer ahora

| Pantalla | Para qué |
|---|---|
| **Inicio (panel)** | Lo que requiere atención hoy (las 6 alertas más graves, cada una con su botón para resolverla) y el estado de la cadena: ventas de los últimos 7 días frente a los 7 previos, producción de la semana y lo pendiente de programar, pedidos en camino y atrasados, fecha y error del pronóstico; **cobertura de insumos** (días que alcanza el stock frente al tiempo de entrega del proveedor, en barras); **productos** con stock listo para despacho, días de venta que cubre, próximas 4 semanas pronosticadas y producido/programado de la semana; **próximas llegadas** de pedidos. Para los administradores, la configuración inicial queda arriba y se pliega sola al completarse. |
| **Alertas** | Activas, críticas y altas, descartadas y resueltas (30 días); filtro por tipo y búsqueda. Cada alerta explica la causa con números y enlaza a la pantalla donde se atiende. Se puede **descartar** (con motivo) o reactivar. |
| **Campana en la cabecera** | En todas las pantallas: número de alertas críticas y altas (rojo si hay críticas), con el detalle al pasar el cursor. Se actualiza cada 5 minutos y al descartar. |
| **Búsqueda desde enlaces** | Los enlaces de alertas y del panel abren la pantalla ya filtrada (por ejemplo, `Reposición` con el insumo buscado). |

## Reglas de alerta

| Alerta | Severidad | Se activa cuando | Se resuelve cuando |
|---|---|---|---|
| Insumo por agotarse | Crítica | Stock + en camino no alcanza para cubrir el tiempo de entrega del proveedor | Llega o se pide lo suficiente |
| Insumo por pedir | Alta | Stock + en camino ≤ punto de reorden (Entrega 8) | Se crea el pedido o sube el stock |
| Insumo sin proveedor | Alta | Necesita reposición y no tiene proveedor con condiciones | Se asigna proveedor o deja de necesitarla |
| Bajo el stock mínimo / sin stock | Media / Alta | Stock < mínimo registrado (sin otra alerta de reposición) | Sube el stock o se ajusta el mínimo |
| Sobre el stock máximo | Informativa | Stock > máximo + 10 % | Baja el stock |
| Pedido atrasado | Media (Alta desde 3 días) | Pasó la fecha estimada y sigue en camino | Se recibe o cancela |
| Pedido por llegar | Informativa | Llega hoy, mañana o pasado mañana | Llega o se atrasa |
| Producto terminado bajo / sin stock | Alta / Crítica | Cubre menos de 3 días de venta (promedio de 28 días) | Se produce o baja la venta |
| Programación vencida | Media | Producción programada para una fecha pasada sin parte | Se registra el parte o se cancela |
| Pronóstico sin entrenar / fallido / con más de 8 días | Media / Alta / Media | Según el último entrenamiento | Se entrena correctamente |
| Pronóstico con error alto | Informativa | MAPE > 30 % en un producto | Mejora el error |
| Conteo físico abierto | Informativa | Abierto hace más de 7 días | Se aprueba o anula |

Funcionamiento:

- Cada condición tiene una **clave estable** (por ejemplo, `insumo-reposicion:12`): no se duplica, se actualiza con los números del momento y **se resuelve sola** cuando la causa desaparece.
- Una alerta **descartada** no cuenta en la campana; si la situación empeora (por ejemplo, de «por pedir» a «por agotarse»), vuelve a activarse.
- La revisión se hace al abrir el panel, las alertas o cualquier pantalla (como máximo una vez por minuto) y, además, **cada 10 minutos en segundo plano** aunque nadie tenga SPIn abierto.
- Se conservan las resueltas de los últimos 90 días.

## Cómo aplicarla

El zip trae el **proyecto completo** (Entregas 0 a 9).

- **Si ya tiene la Entrega 8 funcionando:**
  1. Reemplace los archivos por los de este zip, **conservando** su carpeta `Data/Migrations` y su `.env`.
  2. `.\scripts\migracion.ps1 -Nombre Entrega09` (crea la tabla `Alertas`).
  3. `docker compose up --build -d` (o `dotnet run`).
- **Si empieza de cero:** siga el README. Basta una migración, por ejemplo `Inicial`.

## Archivos

**Nuevos**
```
Models/Alerta.cs                                   Alerta, severidad, estado, tipos y etiquetas
Dtos/Response/AlertaResponses.cs                   alertas y panel
Repository/IAlertaRepository.cs, RepositoryImpl/AlertaRepositoryImpl.cs
Services/InterfacesAlertas.cs                      IAlertaService, IPanelService
Services/ServiceImpl/AlertaService.cs              reglas, sincronización y descarte
Services/ServiceImpl/PanelService.cs               datos del panel
Services/Prediccion/AlertasProgramadas.cs          revisión cada 10 minutos en segundo plano
Controllers/Api/AlertaApiControllers.cs            /api/alertas, /api/alertas/resumen, /api/panel
Views/Alerta/Index.cshtml, wwwroot/js/alertas.js, wwwroot/js/panel.js
ENTREGA-09.md
```

**Modificados**
```
Data/ApplicationDbContext.cs                tabla Alertas con filtro por empresa
Dtos/Request/PrediccionRequests.cs          + filtros de alertas y descarte
Services/InterfacesPrediccion.cs, ReposicionService.cs   + CalcularTodoAsync (lo usan alertas y panel)
Controllers/VistasController.cs             + /alertas
Program.cs                                  + servicios y revisión programada
Views/Home/Index.cshtml, wwwroot/js/inicio.js   el inicio es el panel; la configuración inicial se pliega al completarse
Views/Shared/_Layout.cshtml, _Iconos.cshtml campana en la cabecera, menú Alertas, icono
wwwroot/js/spin.js                          campana y búsqueda desde la URL en todos los listados
wwwroot/js/reposicion.js                    respeta ?buscar= y ?estado=
wwwroot/css/spin.css                        panel, alertas, campana, móvil
README.md
```

## Verificación

| Verificación | Estado |
|---|---|
| Sintaxis de todos los archivos C# (tree-sitter) | Hecha: 0 errores |
| Interfaces implementadas y registradas en DI | Hecha |
| Navegador, Entrega 9: 22 comprobaciones (campana con conteo y color, alertas por severidad con enlaces, bloque «Esta semana», barras de cobertura con marca de entrega, productos con cobertura y producción, próximas llegadas, configuración plegada, filtros de alertas, descartar con motivo, reactivar, resueltas sin acciones, búsqueda desde la URL, panel sin alertas, móvil) | Hecha: 22 de 22 |
| Regresión de las Entregas 1 a 8 | Hecha: 22, 34, 26, 34, 49, 50 y 34 |
| `dotnet build`, migración y ejecución real | **Pendiente: la ejecuta usted** (este entorno no tiene acceso a NuGet ni al SDK de .NET) |

## Prueba manual sugerida (10 minutos)

1. Con los datos de `ejemplos/` cargados y el pronóstico entrenado, abra **Inicio**: debe ver la alerta crítica de PRE-500, la de CO₂ por pedir, y la campana en rojo.
2. Pulse «Ver reposición» en la alerta de PRE-500: se abre filtrada. Cree el pedido sugerido.
3. Vuelva a **Inicio** (espere un minuto o recargue): la alerta de PRE-500 baja de severidad o se resuelve porque ahora hay stock en camino.
4. En **Alertas**, descarte la de CO₂ con un motivo: desaparece de la campana. Revise «Descartadas» y reactívela.
5. Programe producción para hoy y no registre el parte: al día siguiente aparece la alerta «Programación vencida».
6. Registre una venta grande que descuente casi todo el producto terminado: aparece «Producto terminado bajo».

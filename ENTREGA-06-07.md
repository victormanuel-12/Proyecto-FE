# Entregas 6 y 7: Producción y pedidos en camino

**Fecha:** 2026-10-21 · **Destino:** .NET 10 · **Paquetes:** sin cambios (no se agregó ninguno).

## Qué se puede hacer ahora

| Pantalla | Para qué |
|---|---|
| **Producir → Producción · Programación** | Programar qué producto y cuántos paquetes se embotellan en cada línea y día. Solo se ofrecen los productos cuyo formato embotella la línea y que tienen receta. Antes de guardar se ven las botellas, los pallets, las **horas de línea** a velocidad nominal y los **insumos necesarios** frente al stock y lo que está en camino. Se edita o cancela mientras esté pendiente. Las fechas pasadas sin parte quedan marcadas. |
| **Producir → Producción · Partes** | Registrar lo realmente producido: fecha, turno, línea, producto, paquetes, horas, almacén de insumos y de producto terminado (de la misma planta). Antes de registrar se ve qué insumos se descontarán y **cuáles faltan**. Al guardar: «Salida a producción» de cada insumo (receta vigente en la fecha, con merma) y «Entrada de producción» de los paquetes. Se calcula la **eficiencia** de la línea. Se registra desde cero o desde una programación (queda «Ejecutada»). **Anular** con motivo devuelve los insumos y retira los paquetes. |
| **Abastecer → Pedidos en camino** | Pedidos a proveedores con los insumos que cada uno vende: precio y **pedido mínimo** sugeridos, aviso de **múltiplo de compra**, equivalencia en unidad de consumo, total y **llegada estimada** calculada con el tiempo de entrega. Vistas: en camino, **atrasados** (con días de atraso), recibidos, cancelados. **Recepción** parcial o total con guía de remisión: genera «Entradas por compra» en el kardex. Anular una recepción, editar un pedido sin recepciones y cancelar con motivo. |
| **Stock de insumos** | Nueva columna **En camino** (lo pendiente de los pedidos, en unidad de consumo), enlazada a los pedidos. |

## Reglas de negocio

| Regla | Dónde |
|---|---|
| La línea debe estar activa y embotellar el formato del producto. Los almacenes del parte deben ser de la planta de la línea (insumos → tipo insumos; paquetes → tipo producto terminado) | ProduccionService |
| El parte no admite fecha futura ni de hace más de 60 días. La programación es de hoy en adelante (máx. 1 año) | ProduccionService |
| Se usa la **receta vigente en la fecha del parte** (versiones de la Entrega 3) y su merma | ProduccionService + RecetaService |
| Si falta **cualquier** insumo, no se registra nada y se informa la lista completa de faltantes. Todo el parte es una sola transacción | ProduccionService |
| Si se indican horas, no se aceptan más botellas que la velocidad nominal × horas + 10 % | ProduccionService |
| Un parte se vincula a la programación pendiente del mismo día, línea y producto (o a la elegida) | ProduccionService |
| Anular un parte: primero retira los paquetes (si ya se vendieron, no se puede); luego devuelve cada insumo y la programación vuelve a pendiente | ProduccionService |
| Los movimientos generados por un parte o una recepción **no se revierten** desde Movimientos: se corrigen anulando el parte o la recepción | InventarioService |
| Solo se piden insumos asignados al proveedor (condiciones de compra) y una fila por insumo | PedidoService |
| No se recibe más de lo pendiente. Las cantidades del pedido van en unidad de compra y entran al kardex convertidas con el factor del insumo | PedidoService |
| Los insumos se reciben en almacenes de insumos o de cuarentena, nunca en producto terminado | PedidoService |
| Estado del pedido: En camino → Recibido parcial → Recibido. «En camino» y «atrasado» alimentan el stock en camino que usará el pronóstico (Entrega 8) | PedidoService, PedidoRepository |
| Cancelar conserva lo ya recibido; el saldo deja de figurar en camino | PedidoService |
| No se eliminan líneas o productos con producción, ni proveedores o insumos con pedidos | LineaService, ProductoService, ProveedorService, InsumoService |
| Permisos: programar y anular partes: SuperAdmin, Administrador, Planificador. Registrar partes: además Jefe de almacén. Crear, editar y cancelar pedidos: SuperAdmin, Administrador, Compras. Recibir y anular recepciones: SuperAdmin, Administrador, Jefe de almacén, Compras. Todos consultan | Roles |

## Cómo aplicarla

El zip trae el **proyecto completo** (Entregas 0 a 7).

- **Si ya tiene la Entrega 5 funcionando:**
  1. Reemplace los archivos por los de este zip, **conservando** su carpeta `Data/Migrations` y su `.env`.
  2. `.\scripts\migracion.ps1 -Nombre Entrega06y07` (crea 7 tablas y agrega 2 columnas a `MovimientosStock` y 1 a `MovimientosProducto`).
  3. `docker compose up --build -d` (o `dotnet run`).
- **Si empieza de cero:** siga el README. Basta una migración, por ejemplo `Inicial`.

## Archivos

**Nuevos**
```
Models/Operacion.cs                                PlanProduccion, ParteProduccion, ParteConsumo, PedidoCompra,
                                                   PedidoDetalle, Recepcion, RecepcionDetalle
Dtos/Request/OperacionRequests.cs, Dtos/Response/OperacionResponses.cs
Repository/IOperacionRepository.cs                IProduccionOperacionRepository, IPedidoRepository
Repository/RepositoryImpl/ProduccionOperacionRepositoryImpl.cs, PedidoRepositoryImpl.cs
Services/InterfacesOperacion.cs                    IProduccionService, IPedidoService
Services/ServiceImpl/ProduccionService.cs, PedidoService.cs
Controllers/Api/OperacionApiControllers.cs         /api/produccion/..., /api/pedidos/...
Views/Produccion/Index.cshtml, Views/Pedido/Index.cshtml
wwwroot/js/produccion.js, pedidos.js
ENTREGA-06-07.md
```

**Modificados**
```
Models/Enumeraciones.cs                    + Turno, EstadoPlan, EstadoParte, EstadoPedido, EtiquetasOperacion
Models/Inventario.cs, Models/Ventas.cs     + enlace del movimiento al parte o a la recepción que lo generó
Data/ApplicationDbContext.cs               + 7 tablas con filtro por empresa
common/seguridad/Roles.cs                  + PartesProduccion, Recepcion
Services/InterfacesInventario.cs, InterfacesVentas.cs, InventarioService.cs, ProductoTerminadoService.cs
                                           AplicarAsync enlaza parte/recepción; no se revierten a mano; stock «en camino»
Repository/RepositoryImpl/InventarioRepositoryImpl.cs   «Reversible» considera partes y recepciones
Dtos/Response/InventarioResponses.cs       + EnCamino
Services/ServiceImpl/LineaService, ProductoService, ProveedorService, InsumoService   reglas de eliminación
Controllers/VistasController.cs            + /produccion, /pedidos
Program.cs                                 + repositorios y servicios
Views/Shared/_Layout.cshtml, _Iconos.cshtml   menú Producción y Pedidos en camino; 3 iconos
Views/Stock/Index.cshtml, wwwroot/js/stock.js   columna En camino
Views/Home/Index.cshtml                    textos
wwwroot/css/spin.css                       requerimiento, editor de pedidos, recepciones, móvil
README.md
```

## Verificación

| Verificación | Estado |
|---|---|
| Sintaxis de todos los archivos C# (tree-sitter) | Hecha: 0 errores |
| Interfaces implementadas (todos los métodos) y registradas en DI | Hecha |
| Navegador, Entregas 6 y 7: **50 comprobaciones** con servidor simulado con las mismas reglas: filtrado de productos por formato de la línea, horas de línea, requerimiento con faltantes, rechazo por insumos insuficientes y por capacidad, parte desde programación (queda ejecutada), descuento con merma (100 000 → 87 940 preformas por 12 000 botellas con 0,5 %), anulación que restaura todo, llegada estimada = pedido + entrega, pedido mínimo y múltiplo, recepción parcial (+50 000 UND), exceso rechazado, guía obligatoria, anulación de recepción, edición, cancelación, atrasados y 3 vistas en móvil | Hecha: 50 de 50 |
| Regresión de las Entregas 1 a 5 | Hecha: 22, 34, 26, 34 y 49 |
| Error encontrado y corregido en las pruebas | Al elegir un proveedor sin insumos asignados, el cálculo del total fallaba en el navegador |
| `dotnet build`, migración y ejecución real | **Pendiente: la ejecuta usted** (este entorno no tiene acceso a NuGet ni al SDK de .NET) |

## Prueba manual sugerida (15 minutos)

Requisitos: línea con el formato del producto, almacenes `LUR-INS` y `LUR-PT` en la misma planta, receta y stock de insumos (puede usar los archivos de `ejemplos/` de la Entrega 5).

1. **Producción → Programar producción:** línea 1, Cola 500 ml, 1 000 paquetes. Revise horas e insumos necesarios.
2. En **Programación**, pulse ✓ para registrar el parte. Cambie a 100 000 paquetes: debe marcar los faltantes y el servidor debe rechazarlo.
3. Vuelva a 1 000 paquetes, 1 hora, y registre. Revise **Kardex** de preformas («Salida a producción», referencia «Parte de producción N° 1») y **Producto terminado** (+1 000).
4. Intente revertir esa salida desde **Movimientos**: debe indicar que se anula desde Producción. Anule el parte y verifique que todo vuelve.
5. **Pedidos en camino → Nuevo pedido** a SMI PET: preformas 105 millares (aviso de múltiplo). Guárdelo con 110.
6. **Stock de insumos:** la columna «En camino» debe mostrar +110 000 UND.
7. **Recibir:** 50 millares con guía. El pedido queda «Recibido parcial» y el kardex muestra +50 000 UND.
8. En el detalle, **anule la recepción**; luego **cancele** el pedido y búsquelo en «Cancelados».

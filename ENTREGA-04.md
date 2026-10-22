# Entrega 4: Stock, movimientos, kardex y conteo físico

**Fecha:** 2026-10-21 · **Destino:** .NET 10 · **Paquetes:** sin cambios (10.0.13)

## Qué se puede hacer ahora

| Pantalla | Para qué |
|---|---|
| **Stock** | Ver cuánto hay de cada insumo, en total y por almacén, con su equivalente en unidad de compra. Una barra muestra dónde está el stock frente al mínimo y al máximo. Se filtra por «sin stock», «bajo el mínimo» y «sobre el máximo». |
| **Movimientos** | Registrar stock inicial, entradas por compra, salidas por consumo, ajustes y mermas. Antes de guardar se ve el **stock actual y el resultante**, y si no alcanza. Se puede ingresar la cantidad en unidad de compra (cajas) y SPIn la convierte. Un error se corrige **revirtiendo** el movimiento, con motivo obligatorio. |
| **Kardex** | Reporte por insumo, almacén y periodo: saldo inicial + entradas − salidas = saldo final, y cada movimiento con fecha, hora, N°, documento, detalle, usuario y **saldo acumulado**. Tiene periodos rápidos, **exportación a Excel** (CSV que abre directo) e **impresión** en A4 horizontal, sin el menú. |
| **Conteo físico** | Crear un conteo por almacén (y categoría) e imprimir la **hoja de conteo** sin el stock del sistema, para no influir. Registrar lo contado con la diferencia en vivo, guardar el avance y **aprobar**: SPIn genera los ajustes en el kardex con la referencia «Conteo N° X». |
| **Insumos** | El panel de detalle ahora muestra el stock por almacén y enlaza al kardex. |

## Reglas de negocio

| Regla | Dónde |
|---|---|
| El stock solo cambia con movimientos, a través de un único punto (`InventarioService.AplicarAsync`), que también usarán producción (Entrega 6) y recepción de pedidos (Entrega 7) | InventarioService |
| El stock **nunca queda negativo**. Cada registro corre en una transacción con bloqueo de escritura, para que dos registros simultáneos no lo dejen negativo | InventarioService + `EnTransaccionAsync` |
| Los movimientos no se editan ni se borran. Revertir crea el movimiento inverso, enlazado al original, y el original queda marcado. No se revierte una reversión ni un ajuste de conteo | InventarioService |
| El «Stock inicial» solo vale si el insumo no tiene movimientos en ese almacén | InventarioService |
| Salidas, ajustes y mermas exigen motivo, que queda en el kardex | InventarioService |
| Los insumos no se guardan en almacenes de producto terminado | InventarioService, ConteoService |
| Fecha y hora de cada movimiento en hora de Perú (lo pone el servidor, no el usuario), para que el kardex no se desordene | `Reloj.Ahora` |
| Un solo conteo abierto por almacén. Para aprobar deben estar todos contados (0 si no hay) | ConteoService |
| Ajuste del conteo = contado − stock **al aprobar**. Si hubo movimientos durante el conteo, se avisa en la fila | ConteoService + conteo-detalle.js |
| Diferencias mayores al 5 % solo las aprueba un Administrador. El umbral se podrá configurar en la Entrega 8 | ConteoService |
| No se elimina un insumo ni un almacén con movimientos. No se desactiva un almacén con stock. Un almacén con movimientos no puede pasar a ser de producto terminado | InsumoService, AlmacenService |

## Cómo aplicarla

El zip trae el **proyecto completo** (Entregas 0 a 4).

- **Si ya tiene la Entrega 3 funcionando:**
  1. Reemplace los archivos por los de este zip, **conservando** su carpeta `Data/Migrations` y su `.env`.
  2. `.\scripts\migracion.ps1 -Nombre Entrega04`
  3. `docker compose up --build -d` (o `dotnet run`).
- **Si empieza de cero:** siga el README. Basta una migración, por ejemplo `Inicial`.

## Archivos

**Nuevos**
```
Models/Inventario.cs                              Stock, MovimientoStock, ConteoFisico, ConteoDetalle
Dtos/Request/InventarioRequests.cs
Dtos/Response/InventarioResponses.cs
Repository/IInventarioRepository.cs
Repository/RepositoryImpl/InventarioRepositoryImpl.cs
Services/InterfacesInventario.cs                  IInventarioService, IConteoService
Services/ServiceImpl/InventarioService.cs         Movimientos, reversión, kardex
Services/ServiceImpl/ConteoService.cs             Conteo físico
Controllers/Api/InventarioApiController.cs        /api/stock, /api/movimientos, /api/kardex (+csv), /api/conteos
Views/Stock/Index.cshtml
Views/Movimiento/Index.cshtml
Views/Kardex/Index.cshtml
Views/Conteo/Index.cshtml, Detalle.cshtml, Hoja.cshtml
wwwroot/js/stock.js, movimiento.js, kardex.js, conteos.js, conteo-detalle.js, hoja.js
wwwroot/css/hoja.css                              Hoja de conteo para imprimir
ENTREGA-04.md
```

**Modificados**
```
Models/Enumeraciones.cs                   + TipoMovimiento, EstadoConteo, EtiquetasInventario
Data/ApplicationDbContext.cs              + 4 tablas con filtro por empresa e índices
common/seguridad/Roles.cs                 + grupo Almacen (SuperAdmin, Administrador, JefeAlmacen)
Repository/IAlmacenRepository.cs (+Impl)  + opciones de almacenes para selectores
Services/Interfaces.cs, AlmacenService.cs + opciones; reglas de stock al desactivar, eliminar o cambiar de tipo
Controllers/Api/AlmacenApiController.cs   + /api/almacenes/opciones para cualquier usuario
Services/ServiceImpl/InsumoService.cs     + stock en el detalle; no elimina insumos con movimientos
Dtos/Response/CatalogoResponses.cs        + Stock por almacén en InsumoResponse
Services/ServiceImpl/InicioService.cs, Dtos/Response/Respuestas.cs   + insumos con stock
Controllers/VistasController.cs           + /stock, /movimientos, /kardex, /conteos, /conteos/{id}, /conteos/{id}/hoja
Program.cs                                + registro de repositorio y servicios
Views/Shared/_Layout.cshtml, _Iconos.cshtml   menú Almacenar ampliado, 7 iconos
Views/Home/Index.cshtml, wwwroot/js/inicio.js paso 8 «Cargar el stock inicial»
wwwroot/css/spin.css                      + nivel de stock, tipos de movimiento, kardex, conteo, impresión
wwwroot/js/spin.js                        + fechas, mensajes 403 del servidor, filtros por fecha, segmento configurable
wwwroot/js/insumo.js                      + stock en el panel de detalle
README.md
```

## Verificación

| Verificación | Estado |
|---|---|
| Sintaxis de los 90 archivos C# (tree-sitter) | Hecha: 0 errores |
| Interfaces implementadas, registradas en DI y `using` necesarios | Hecha |
| Navegador, Entrega 4: 34 comprobaciones con un servidor simulado que aplica las mismas reglas. Incluye conversión de 150 CJ a 300 000 UND, stock inicial repetido, salida mayor al stock, motivo obligatorio, reversión, saldos del kardex fila a fila, impresión, conteo con pendientes, mensaje 403 de diferencia mayor al 5 %, aprobación con 3 ajustes y stock final igual a lo contado, y vista móvil | Hecha: 34 de 34 |
| Regresión en el navegador de las Entregas 1, 2 y 3 | Hecha: 22, 34 y 26 de 26 |
| Errores de usabilidad encontrados y corregidos en las pruebas | 1) Al completar el último pendiente, la tabla del conteo quedaba vacía. 2) Las tarjetas de tipo de movimiento se veían con recuadros anidados. 3) El sombreado de la columna Saldo del kardex era irregular. |
| `dotnet build`, migración y ejecución real | **Pendiente: la ejecuta usted** (este entorno no tiene acceso a NuGet ni al SDK de .NET) |

## Prueba manual sugerida (10 minutos)

1. **Movimientos → Registrar movimiento → Stock inicial:** preformas, 150 en **CJ**. La vista previa debe decir 300 000 UND.
2. Registre otra vez «Stock inicial» del mismo insumo: debe rechazarlo.
3. **Salida por consumo** mayor al stock: la vista previa avisa y el servidor la rechaza.
4. **Entrada por compra** con guía, luego una **salida** y **revierta** la salida. Debe quedar tachada.
5. **Kardex** de preformas, este mes: revise saldo inicial + entradas − salidas = saldo final, **Exportar a Excel** e **Imprimir**.
6. **Stock:** revise la barra de nivel y el filtro «Bajo el mínimo».
7. **Conteo físico → Nuevo conteo** en el almacén de insumos, **Hoja de conteo**, registre cantidades con una diferencia mayor al 5 %:
   - Con un **Jefe de almacén**, aprobar debe pedir a un Administrador.
   - Con el **Administrador**, aprobar genera los ajustes. Véalos en el kardex con «Conteo N° 1».
8. **Almacenes:** intente desactivar uno con stock: debe impedirlo.

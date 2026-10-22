# Entrega 5: Ventas, producto terminado e importación desde Excel

**Fecha:** 2026-10-21 · **Destino:** .NET 10 · **Paquete nuevo:** `ClosedXML 0.105.0` (licencia MIT, gratuito) para leer y generar Excel.

## Qué se puede hacer ahora

| Pantalla | Para qué |
|---|---|
| **Vender → Ventas y despachos** | Registrar ventas por día, producto y canal (mayorista, tradicional, moderno, distribuidor, otro), en **paquetes o botellas** (SPIn convierte). Con «Descontar del stock» la venta sale del almacén de producto terminado; sin él, queda **solo como historial**. Gráfico de **demanda semanal** (13, 26 o 52 semanas) por producto o total, con aviso de cuántas semanas de historial hay. Filtros por producto, canal, origen, fechas y anuladas, con totales del periodo. **Anular** con motivo: si descontó stock, los paquetes vuelven. |
| **Almacenar → Producto terminado** | Stock en paquetes, botellas y pallets por producto y almacén, **venta diaria** promedio (28 días) y **cobertura en días**. Filtro con/sin stock. Movimientos con saldo: stock inicial, ajustes y mermas (motivo obligatorio). Historial de movimientos filtrable por producto. |
| **Administración → Importar datos** | Carga desde **.xlsx o .csv** de 8 tipos, en orden: proveedores, insumos, condiciones de compra, productos, recetas, stock inicial de insumos, stock inicial de producto terminado e **historial de ventas**. Plantilla descargable con hoja de instrucciones y **valores válidos de su empresa**. **Vista previa** con errores por fila y columna antes de guardar; al confirmar, todo se guarda junto o nada. |
| **Inicio** | Paso 9: «Cargar el historial de ventas». |

## Reglas de negocio

| Regla | Dónde |
|---|---|
| El stock de producto terminado solo cambia por un punto único (`ProductoTerminadoService.AplicarAsync`), que también usará producción en la Entrega 6, y **nunca queda negativo** | ProductoTerminadoService |
| La venta no puede tener fecha futura. Si descuenta stock, exige un almacén **activo de producto terminado** | VentaService |
| Ventas y movimientos no se borran: se **anulan** (con motivo). Anular una venta que descontó stock crea un «Ajuste positivo» enlazado y marca la salida como revertida | VentaService |
| Una venta anulada deja de contar en el historial de demanda (gráfico y pronóstico) | VentaRepository |
| «Stock inicial» de producto terminado solo si no hay movimientos del producto en ese almacén. Ajustes y mermas exigen motivo. La entrada por producción la genera el sistema (Entrega 6) | ProductoTerminadoService |
| No se elimina un producto con ventas o movimientos. No se desactiva un almacén con stock de producto terminado; no se elimina ni cambia de tipo si tiene movimientos | ProductoService, AlmacenService |
| Importación: se reconocen encabezados sin importar mayúsculas, tildes o espacios; números con punto o coma decimal; fechas `dd/mm/aaaa`, `aaaa-mm-dd` o fecha de Excel; «sí/no» | ImportadorBase, LectorTabla |
| Importación: máximo 5 MB y 20 000 filas. La vista previa dura 30 minutos y solo la confirma el mismo usuario | ImportacionService |
| Importación: al confirmar se **vuelve a validar** y se ejecuta en **una transacción** usando los mismos servicios de las pantallas (mismas reglas). Si una fila falla, se informa «Fila N: …» y no se guarda nada | ImportacionService |
| Historial de ventas: varias filas del mismo día, producto y canal se suman en un registro. Si ese día ya tenía historial: **reemplazar**, **sumar** u **omitir** (lo elige el usuario). No descuenta stock | ImportadorVentas |
| Recetas: se agrupan por SKU y crean una versión con la fecha «Vigente desde» elegida (mismas reglas de versiones de la Entrega 3) | ImportadorRecetas |
| Solo SuperAdmin y Administrador importan. Registran ventas: SuperAdmin, Administrador, Planificador y Jefe de almacén. Movimientos de producto terminado: SuperAdmin, Administrador y Jefe de almacén | Roles |

## Cómo aplicarla

El zip trae el **proyecto completo** (Entregas 0 a 5).

- **Si ya tiene la Entrega 4 funcionando:**
  1. Reemplace los archivos por los de este zip, **conservando** su carpeta `Data/Migrations` y su `.env`.
  2. `.\scripts\migracion.ps1 -Nombre Entrega05` (crea las tablas `Ventas`, `StocksProducto` y `MovimientosProducto`).
  3. `docker compose up --build -d` (o `dotnet run`). La primera compilación descarga ClosedXML.
- **Si empieza de cero:** siga el README. Basta una migración, por ejemplo `Inicial`.

## Archivos de ejemplo para la demostración

En la carpeta `ejemplos/` hay 8 archivos listos para importar **en este orden** (01 a 08): 5 proveedores, 10 insumos, 9 condiciones de compra, 2 productos (Cola 500 ml × 12 y Naranja 1.5 L × 6), sus recetas, stock inicial y **14 meses de ventas diarias** con estacionalidad (pico en verano y en fiestas).

Antes de importar 06 y 07, cree los almacenes **`LUR-INS`** (insumos) y **`LUR-PT`** (producto terminado), o cambie el código en esos archivos.

## Archivos

**Nuevos**
```
Models/Ventas.cs                                   Venta, StockProducto, MovimientoProducto
Dtos/Request/VentasRequests.cs
Dtos/Response/VentasResponses.cs, ImportacionResponses.cs
Repository/IVentaRepository.cs, IImportacionRepository.cs (+ RepositoryImpl)
Services/InterfacesVentas.cs                       IVentaService, IProductoTerminadoService
Services/ServiceImpl/VentaService.cs, ProductoTerminadoService.cs
Services/Importacion/                              Modelo, LectorTabla (xlsx/csv), ImportadorBase,
                                                   ImportadoresCatalogo, ImportadoresOperacion, ImportacionService
Controllers/Api/VentasApiControllers.cs            /api/ventas, /api/producto-terminado, /api/importar
Views/Venta/Index.cshtml, Views/ProductoTerminado/Index.cshtml, Views/Importacion/Index.cshtml
wwwroot/js/venta.js, producto-terminado.js, importacion.js
ejemplos/01-proveedores.xlsx … 08-historial-ventas.xlsx
ENTREGA-05.md
```

**Modificados**
```
SPIn.csproj                                        + ClosedXML
Models/Enumeraciones.cs                            + CanalVenta, OrigenVenta, TipoMovimientoProducto
Data/ApplicationDbContext.cs                       + 3 tablas con filtro por empresa
common/seguridad/Roles.cs                          + Ventas, PuedeVentas, PuedeImportar
Repository/InventarioRepositoryImpl.cs             transacción reutilizable dentro de la importación
Repository/IAlmacenRepository (+Impl), AlmacenService, AlmacenApiController   + opción soloProductoTerminado; reglas de producto terminado
Repository/IProduccionRepository (+Impl), ProductoService, ProductoApiController + /api/productos/opciones; no elimina con ventas
Dtos/Response/ListadoResponse.cs                   + ProductoOpcionResponse
Services/ServiceImpl/InicioService.cs, Dtos/Response/Respuestas.cs   + productos con ventas
Controllers/VistasController.cs                    + /ventas, /producto-terminado, /importar
Program.cs                                         + servicios, importadores y caché en memoria
Views/Shared/_Layout.cshtml, _Iconos.cshtml        menú Vender, Producto terminado, Importar datos; 4 iconos
Views/Home/Index.cshtml, wwwroot/js/inicio.js      paso 9
wwwroot/js/spin.js                                 envío de archivos (FormData) y aviso de archivo grande
wwwroot/css/spin.css                               gráfico, importación, tipos de movimiento
.dockerignore, README.md
```

## Verificación

| Verificación | Estado |
|---|---|
| Sintaxis de los 108 archivos C# (tree-sitter) | Hecha: 0 errores |
| Interfaces implementadas y registradas en DI | Hecha |
| Navegador, Entrega 5: **49 comprobaciones** con servidor simulado con las mismas reglas: gráfico 26/52 semanas, conversión 6 000 botellas = 500 paquetes, rechazo por stock insuficiente, venta que descuenta (300 → 180), venta pasada como historial, CSRF, anulación que devuelve stock (→ 300), filtro de anuladas, cobertura en días, merma sin motivo, merma sin stock, stock inicial repetido, importación con errores por fila, envío multipart, confirmación con token, inicio 9/9 y 3 pantallas en móvil sin desplazamiento horizontal | Hecha: 49 de 49 |
| Regresión de las Entregas 1 a 4 | Hecha: 22, 34, 26 y 34 de 34 |
| Encabezados de los 8 archivos de ejemplo contra las columnas de cada importador, categorías, unidades y RUC (dígito verificador) | Hecha |
| `dotnet build`, migración, lectura real de Excel con ClosedXML y ejecución | **Pendiente: la ejecuta usted** (este entorno no tiene acceso a NuGet ni al SDK de .NET) |

## Prueba manual sugerida (15 minutos)

1. **Almacenes:** cree `LUR-INS` (insumos) y `LUR-PT` (producto terminado).
2. **Importar datos:** suba en orden los archivos 01 a 08 de `ejemplos/`. En cada uno revise la vista previa y confirme.
3. Suba de nuevo `02-insumos.xlsx` con «Omitir los que ya existen»: la vista previa debe indicar 10 omitidos y nada que importar.
4. Edite una celda de `04-productos.xlsx` con un formato que no existe y súbalo: debe marcar la fila y la columna, sin guardar nada.
5. **Ventas:** revise el gráfico de 52 semanas (debe verse el pico de verano). Registre una venta de hoy de 1 200 **botellas** de Cola: debe descontar 100 paquetes de `LUR-PT`.
6. Registre una venta mayor al stock: debe rechazarla.
7. **Anule** la venta del punto 5 y verifique en **Producto terminado** que el stock volvió y que hay un «Ajuste positivo» enlazado.
8. **Producto terminado:** registre una merma sin motivo (debe pedirlo) y con motivo.
9. Con un usuario **Gerencia**, entre a Ventas: puede ver, pero no registrar ni importar.

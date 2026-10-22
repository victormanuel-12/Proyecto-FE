# Entrega 3: Productos y recetas (lista de materiales)

**Fecha:** 2026-10-21 · **Destino:** .NET 10 · **Paquetes:** sin cambios (10.0.13)

## Qué se puede hacer ahora

1. **Registrar productos (SKU)** con su formato (PET 500 ml, 3 L, vidrio, lata…), botellas por paquete y paquetes por pallet. El formulario muestra al instante cuántas botellas tiene un pallet.
2. **Definir la receta** de cada producto: qué insumos consume **por botella**, **por paquete** o **por pallet**, con su **merma** esperada. Ejemplo: 1 preforma y 1 tapa por botella, 18 g de film por paquete, 1 parihuela por pallet.
3. **Versionar la receta por fecha.** Si se cambia la etiqueta desde el 1 de diciembre, se crea la versión 2 desde esa fecha y la versión 1 sigue rigiendo hasta el 30 de noviembre. Así, la producción pasada se calcula con la receta que tenía en su día. El editor explica antes de guardar qué va a pasar.
4. **Copiar la receta** de otro producto parecido y ajustarla.
5. **Calcular el consumo:** «¿cuánto de cada insumo necesito para 2 pallets?». Responde en unidad de consumo (con y sin merma) y en unidad de compra (cajas, rollos). Es el mismo cálculo que usarán producción (Entrega 6) y el pronóstico (Entrega 8).
6. Ver qué **líneas de producción** pueden embotellar cada producto, según su formato.

## Cómo aplicarla

El zip trae el **proyecto completo** (Entregas 0 a 3).

- **Si ya tiene la Entrega 2 funcionando:**
  1. Reemplace los archivos por los de este zip, **conservando** su carpeta `Data/Migrations` y su `.env`.
  2. `.\scripts\migracion.ps1 -Nombre Entrega03`
  3. `docker compose up --build -d` (o `dotnet run`).
- **Si empieza de cero:** siga el README. Basta una migración, por ejemplo `Inicial`.

## Archivos

**Nuevos**
```
Models/Producto.cs                               Producto, Receta (versión), RecetaDetalle
common/Reloj.cs                                  Fecha de negocio en hora de Perú (Docker corre en UTC)
Dtos/Request/ProduccionRequests.cs               Producto, receta, cálculo de consumo
Dtos/Response/ProduccionResponses.cs
Repository/IProduccionRepository.cs              IProductoRepository, IRecetaRepository
Repository/RepositoryImpl/ProductoRepositoryImpl.cs
Repository/RepositoryImpl/RecetaRepositoryImpl.cs
Services/InterfacesProduccion.cs                 IProductoService, IRecetaService
Services/ServiceImpl/ProductoService.cs
Services/ServiceImpl/RecetaService.cs            Versionado y cálculo de consumo (explosión de materiales)
Controllers/Api/ProductoApiController.cs         /api/productos...
Views/Producto/Index.cshtml                      Listado, panel, formulario, editor de receta, calculadora
wwwroot/js/producto.js
ENTREGA-03.md
```

**Modificados**
```
Models/Enumeraciones.cs                    + NivelReceta (por botella, paquete, pallet)
Data/ApplicationDbContext.cs               + tablas Productos, Recetas, RecetaDetalles con filtro por empresa
common/seguridad/Roles.cs                  + grupo Produccion (SuperAdmin, Administrador, Planificador)
common/exception/ValidationFilter.cs       Errores de filas de una lista: "Fila 3: ..."
Services/ServiceImpl/InsumoService.cs      No se elimina un insumo usado en recetas; aviso al desactivarlo
Services/ServiceImpl/InicioService.cs      + productos con receta
Dtos/Response/Respuestas.cs                + ProductosConReceta
Controllers/VistasController.cs            + página /productos
Program.cs                                 + registro de repositorios y servicios
Views/Shared/_Layout.cshtml                + menú «Producir»
Views/Shared/_Iconos.cshtml                + 4 iconos
Views/Home/Index.cshtml, wwwroot/js/inicio.js   paso 7 de la configuración inicial
wwwroot/css/spin.css                       + editor de receta, historial, calculadora
wwwroot/js/spin.js                         + formato de cantidades con hasta 6 decimales
README.md
```

## Reglas de negocio

| Regla | Dónde |
|---|---|
| SKU único por empresa | ProductoService |
| Una fila por insumo en cada receta | RecetaService + producto.js |
| Solo insumos activos y de consumo «por producción» (lubricante y soda cáustica se consumen por horas de operación) | RecetaService, endpoint `insumos-receta` |
| Cantidad > 0; merma entre 0 % y 50 % | RecetaDetalleRequest + producto.js |
| Misma fecha de inicio que la última versión: se corrige esa versión. Fecha posterior: versión nueva y la anterior se cierra el día previo | RecetaService |
| Fecha anterior a la última versión: no permitida (cambiaría producciones ya registradas) | RecetaService + aviso en el editor |
| No se elimina un producto con receta (se desactiva). No se elimina un insumo que esté en alguna receta | ProductoService, InsumoService |
| Consumo = cantidad × (botellas, paquetes o pallets) × (1 + merma) | RecetaService.ExplotarAsync |

## Verificación

| Verificación | Estado |
|---|---|
| Sintaxis de los 81 archivos C# (tree-sitter) | Hecha: 0 errores |
| Interfaces implementadas y registradas en DI | Hecha |
| Navegador, Entrega 3 (26 comprobaciones: listado, panel, historial, calculadora con 2 pallets = 2 016 botellas y 2 046.24 preformas con 1.5 % de merma, editor con validación por fila, versión nueva y explicación, copiar receta, producto nuevo, móvil) | Hecha: 26 de 26 |
| Regresión de las Entregas 1 y 2 en el navegador | Hecha: 22 de 22 y 34 de 34 |
| `dotnet build`, migración y ejecución real | **Pendiente: la ejecuta usted** (este entorno no tiene acceso a NuGet ni al SDK de .NET) |

## Prueba manual sugerida

1. **Insumos:** registre (si no los tiene) una preforma 500 ml, una tapa, CO₂ en KG y film termoencogible en KG. Todos con consumo «por producción».
2. **Productos:** cree `COL-500-12`, PET 500 ml, 12 × 84.
3. **Definir receta:** preforma 1 por botella (merma 1.5 %), tapa 1 por botella, CO₂ 0.0042 por botella, film 0.018 por paquete.
4. **Calcular consumo:** 2 pallets debe dar 2 016 botellas y 2 046.24 preformas.
5. **Cambiar receta** con fecha del próximo mes: el editor debe decir que se crea la versión 2 y hasta cuándo rige la 1.
6. Cree `LIM-500-12` y use **Copiar** desde `COL-500-12`.
7. Intente eliminar la preforma en Insumos: no debe permitirlo, porque está en una receta.

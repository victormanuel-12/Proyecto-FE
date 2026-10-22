# Entrega 2: Insumos, categorías, unidades y proveedores

**Fecha:** 2026-10-21 · **Destino:** .NET 10 · **Paquetes:** sin cambios (10.0.13)

## Cómo aplicarla

El zip trae el **proyecto completo** (Entregas 0, 1 y 2).

- **Si todavía no ejecutó la Entrega 1:** use esta carpeta tal cual y siga el README (migración `Entrega01` y luego `Entrega02`, o una sola migración `Inicial`).
- **Si ya tiene la Entrega 1 funcionando y con datos:**
  1. Reemplace todos los archivos por los de este zip, **excepto** la carpeta `Data/Migrations` y su archivo `.env`.
  2. Cree la migración de esta entrega:
     ```powershell
     .\scripts\migracion.ps1 -Nombre Entrega02
     ```
  3. `docker compose up --build -d` (o `dotnet run`). Las tablas nuevas se crean al iniciar y sus datos se conservan.
  4. Al iniciar, cada empresa recibe categorías y unidades sugeridas (solo si aún no tiene).

## Archivos

**Nuevos**
```
Models/Catalogo.cs                                  CategoriaInsumo, UnidadMedida, Insumo, Proveedor, ProveedorInsumo
Data/CatalogoInicial.cs                             Categorías y unidades sugeridas por empresa
Dtos/Request/CatalogoRequests.cs                    Solicitudes y filtros validados
Dtos/Response/CatalogoResponses.cs                  Respuestas
Repository/ICatalogoRepository.cs                   5 interfaces de repositorio
Repository/RepositoryImpl/CategoriaRepositoryImpl.cs
Repository/RepositoryImpl/UnidadRepositoryImpl.cs
Repository/RepositoryImpl/InsumoRepositoryImpl.cs
Repository/RepositoryImpl/ProveedorRepositoryImpl.cs
Repository/RepositoryImpl/CondicionCompraRepositoryImpl.cs
Services/InterfacesCatalogo.cs                      5 interfaces de servicio
Services/ServiceImpl/CategoriaService.cs
Services/ServiceImpl/UnidadService.cs
Services/ServiceImpl/InsumoService.cs
Services/ServiceImpl/ProveedorService.cs
Services/ServiceImpl/CondicionCompraService.cs
Controllers/Api/CatalogoApiControllers.cs           /api/categorias, /api/unidades, /api/insumos, /api/proveedores, /api/condiciones
Views/Insumo/Index.cshtml                           Listado + panel de detalle + formularios
Views/Proveedor/Index.cshtml
Views/Catalogo/Index.cshtml                         Categorías y unidades en una sola página
wwwroot/js/insumo.js
wwwroot/js/proveedor.js
wwwroot/js/catalogo.js
ENTREGA-02.md
```

**Modificados**
```
Models/Enumeraciones.cs              + TipoConsumo, Moneda, EtiquetasCatalogo
Data/ApplicationDbContext.cs         + 5 tablas, filtros por empresa, decimal guardado como REAL (SQLite)
Data/DbInitializer.cs                + catálogo sugerido para empresas existentes
common/exception/GenericException.cs + errores por campo (GenericException.Campo)
common/exception/GlobalExceptionFilter.cs  devuelve esos errores por campo
common/seguridad/Roles.cs            + grupos Catalogo y Abastecimiento
Repository/IEmpresaRepository.cs (+Impl)   + catálogo inicial al crear una empresa
Services/ServiceImpl/EmpresaService.cs     llama al catálogo inicial
Services/ServiceImpl/InicioService.cs      + conteo de insumos y proveedores
Dtos/Response/Respuestas.cs          + Insumos y Proveedores en el resumen de Inicio
Controllers/VistasController.cs      + páginas /insumos, /proveedores, /categorias-unidades
Program.cs                           + registro de repositorios y servicios
Views/Shared/_Layout.cshtml          + menú Almacenar y Abastecer
Views/Shared/_Iconos.cshtml          + 5 iconos
Views/Home/Index.cshtml              6 pasos de configuración
wwwroot/css/spin.css                 + panel lateral, secciones de formulario, interruptores, dos columnas
wwwroot/js/spin.js                   + clic en fila, formato de moneda, ocultar acciones en solo consulta
wwwroot/js/inicio.js                 + pasos de proveedores e insumos
README.md
```

## Reglas de negocio implementadas

| Regla | Dónde |
|---|---|
| Código de insumo único por empresa; código en mayúsculas | InsumoService |
| Si unidad de compra = unidad de consumo, el factor debe ser 1 (el formulario lo fija) | InsumoService + insumo.js |
| Stock máximo ≥ mínimo; capacidad ≥ máximo | InsumoService |
| Nivel de servicio entre 50 % y 99.9 % (98 % sugerido para críticos) | InsumoRequest + insumo.js |
| Solo se eligen categorías y unidades activas | InsumoService |
| Proveedor del Perú: RUC obligatorio, válido y único; extranjero: sin RUC | ProveedorService |
| Un solo proveedor principal por insumo; el primero que se asigna lo es | CondicionCompraService |
| Al quitar el principal, pasa a principal el activo que entrega más rápido | CondicionCompraService |
| Variación de entrega ≤ tiempo de entrega | CondicionCompraService |
| No se elimina lo que está en uso (categoría o unidad con insumos, insumo o proveedor con condiciones): se desactiva | Servicios |
| Lectura para todos los roles; edición según el rol, validada en cada endpoint | Controladores API |

## Verificación

| Verificación | Estado |
|---|---|
| Sintaxis de los 70 archivos C# (analizador tree-sitter) | Hecha: 0 errores |
| Interfaces ↔ implementaciones ↔ registro en DI | Hecha: todas registradas e implementadas |
| Frontend de la Entrega 2 en Chromium con la API simulada (34 comprobaciones: listado, filtros, panel de detalle, agregar proveedor, error por campo, marcar principal, quitar, alta de insumo con equivalencia y validaciones, proveedor nacional y extranjero, categorías y unidades, panel en móvil) | Hecha: 34 de 34 |
| Regresión de la Entrega 1 en el navegador | Hecha: 22 de 22 |
| `dotnet build`, migración y ejecución real | **Pendiente: la ejecuta usted.** Este entorno sigue sin acceso a NuGet ni al SDK de .NET |

## Prueba manual sugerida

1. **Proveedores:** cree uno del Perú con RUC `20100113610` y uno de China sin RUC.
2. **Categorías y unidades:** verifique que ya están las sugeridas. Cree una categoría duplicada: debe marcar el error en el campo.
3. **Insumos:** cree `PRF-3000-CR`, unidad de compra Caja, de consumo Unidad, factor 2000, crítico. Al guardar se abre el panel: agregue el proveedor chino (US$, 45 ± 10 días, mínimo 600 CJ, de a 10).
4. Agregue el proveedor peruano y márquelo como principal. Quítelo: el chino vuelve a ser principal.
5. Ingrese con un usuario **Gerencia**: debe ver insumos y proveedores sin botones de edición.

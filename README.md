# SPIn: Entregas 0 a 10

Sistema Predictivo de Inventarios para embotelladoras de gaseosas.
ASP.NET Core MVC (.NET 10) · EF Core 10 + SQLite · ASP.NET Core Identity · HTML/CSS/JS propios.

## Qué incluye esta entrega

| Módulo | Qué funciona |
|---|---|
| Base (Entrega 0) | Proyecto .NET 10, multiempresa (cada empresa ve solo sus datos), auditoría automática, filtros de errores y validación, protección CSRF, cabeceras de seguridad, diseño completo, Docker |
| 1.1 Seguridad y usuarios | Inicio y cierre de sesión, bloqueo tras 5 intentos, cambio de contraseña, CRUD de usuarios con roles, restablecer contraseña, activar y desactivar (la sesión se cierra en 1 minuto como máximo) |
| 1.2 Empresa y estructura | CRUD de empresas (solo superadministrador, con validación del RUC por dígito verificador), plantas, almacenes (insumos, producto terminado, cuarentena) y líneas de producción con tipo de envase, velocidad y formatos |
| 1.3 Catálogo de insumos (Entrega 2) | Insumos con unidades de compra y consumo, factor de conversión, política de stock, nivel de servicio, tipo de consumo y características. Categorías y unidades editables, con valores sugeridos para embotelladoras |
| 1.4 Productos y recetas (Entrega 3) | Productos (SKU) con formato y empaque. Receta por producto (insumos por botella, paquete o pallet, con merma), versionada por fecha, con historial. Copiar la receta de otro producto. Calculadora de consumo de insumos |
| 1.6 Stock y movimientos (Entrega 4) | Stock por insumo y almacén con nivel frente al mínimo y máximo. Movimientos (stock inicial, entradas, salidas, ajustes, mermas) que no se editan ni borran: se revierten. Kardex con saldo acumulado, exportable a Excel e imprimible |
| 1.12 Conteo físico (Entrega 4) | Conteos por almacén con hoja imprimible, registro de lo contado, diferencias en vivo, aprobación con ajustes automáticos en el kardex |
| Ventas y producto terminado (Entrega 5) | Ventas por día, producto y canal, en paquetes o botellas; descuentan stock de producto terminado o quedan como historial; anulación con motivo; gráfico de demanda semanal. Stock de producto terminado con cobertura en días y sus movimientos |
| Producción (Entrega 6) | Programación por línea y día con horas e insumos necesarios. Partes de producción que descuentan insumos según la receta vigente (con merma) y suman producto terminado, con eficiencia de línea y anulación |
| Pedidos en camino (Entrega 7) | Pedidos a proveedores con precio, pedido mínimo, múltiplo y llegada estimada; vistas en camino y atrasados; recepción parcial con guía que entra al kardex; stock «en camino» en Stock de insumos |
| Alertas y panel (Entrega 9) | Inicio con lo que requiere atención, ventas y producción de la semana, cobertura de insumos y de producto terminado, y próximas llegadas. Alertas automáticas (insumos por agotarse o por pedir, pedidos atrasados, producto terminado bajo, programación vencida, pronóstico) que se resuelven solas; campana en la cabecera |
| Datos de demostración y pruebas (Entrega 10) | Con `SPIN_DEMO=true` la primera vez que arranca carga una embotelladora completa (30 meses de ventas, 90 días de operación, pedidos en camino, alertas y modelo entrenado) con fechas relativas al día de hoy. Pruebas automáticas (`dotnet test`) y scripts de respaldo y restauración. Ver `ENTREGA-10.md` |
| Motor predictivo (Entrega 8) | Pronóstico de demanda por producto con ML.NET (SSA) comparado con Holt, promedio móvil y estacional; elige el de menor error (MAPE). Reposición de insumos: stock de seguridad, punto de reorden, fecha de agotamiento y cantidad sugerida, con pedido prellenado. Reentrenamiento semanal automático y Configuración |
| Importación desde Excel (Entrega 5) | Proveedores, insumos, condiciones, productos, recetas, stock inicial e historial de ventas desde .xlsx o .csv, con plantilla, vista previa de errores y guardado todo o nada. Archivos de ejemplo en `ejemplos/` |
| 1.5 Proveedores (Entrega 2) | Proveedores nacionales (RUC validado) y extranjeros. Condiciones de compra por insumo (precio, moneda, entrega ± variación, pedido mínimo, múltiplo) y un proveedor principal por insumo |

Roles: `SuperAdmin`, `Administrador`, `JefeAlmacen`, `Planificador`, `Compras`, `Gerencia`.
Permisos de la Entrega 2: todos los roles pueden **consultar** insumos y proveedores. Editan insumos, categorías y unidades: `SuperAdmin`, `Administrador`, `JefeAlmacen` y `Compras`. Editan proveedores y condiciones de compra: `SuperAdmin`, `Administrador` y `Compras`. Editan productos y recetas (Entrega 3): `SuperAdmin`, `Administrador` y `Planificador`. Registran movimientos y conteos (Entrega 4): `SuperAdmin`, `Administrador` y `JefeAlmacen`; un conteo con diferencias mayores al 5 % solo lo aprueba un `Administrador`. Registran ventas (Entrega 5): `SuperAdmin`, `Administrador`, `Planificador` y `JefeAlmacen`. Importan datos: `SuperAdmin` y `Administrador`. Producción (Entrega 6): programan y anulan partes `SuperAdmin`, `Administrador` y `Planificador`; registran partes además `JefeAlmacen`. Pedidos (Entrega 7): crean y cancelan `SuperAdmin`, `Administrador` y `Compras`; reciben además `JefeAlmacen`.

---

## Opción A: ejecutar con Docker (recomendada)

Requisito: **Docker Desktop** en ejecución.

Todos los comandos se escriben en **PowerShell**, desde la carpeta `SPIn` (donde está `SPIn.csproj`).

### 1. Crear el archivo `.env`

```powershell
Copy-Item .env.example .env
notepad .env      # cambie SPIN_ADMIN_CLAVE (mín. 8 caracteres, con mayúscula, minúscula y número)
```

### 2. Crear la migración inicial (solo la primera vez)

```powershell
.\scripts\migracion.ps1 -Nombre Entrega01
```

- Si tiene instalado el SDK de .NET 10, el script lo usa. Si no, usa un contenedor de Docker con el SDK.
- Resultado esperado: `Migración 'Entrega01' creada en Data/Migrations.`
- Si PowerShell bloquea el script, ejecute primero: `Set-ExecutionPolicy -Scope Process Bypass`

### 3. Construir y levantar

```powershell
docker compose up --build -d
docker compose logs -f spin      # espere "Now listening on: http://[::]:8080" y salga con Ctrl+C
```

### 4. Ingresar

Abra **http://localhost:8080** e ingrese con `SPIN_ADMIN_EMAIL` y `SPIN_ADMIN_CLAVE` del `.env`.

### Comandos útiles

```powershell
docker compose down                # detener (los datos se conservan en el volumen spin-datos)
docker compose up --build -d       # volver a construir tras copiar una nueva entrega
docker compose down -v             # BORRAR todos los datos y empezar de cero
```

---

## Opción B: ejecutar con el SDK de .NET 10 (sin Docker)

```powershell
dotnet --list-sdks                          # debe aparecer una versión 10.x
dotnet tool restore                         # instala dotnet-ef 10.0.13 en el proyecto
dotnet restore
dotnet ef migrations add Entrega01 --output-dir Data/Migrations   # solo la primera vez
dotnet build
dotnet run --launch-profile http
```

Abra **http://localhost:5080**. En desarrollo, el superadministrador es:

- Correo: `admin@spin.local`
- Contraseña: `Spin.Admin2026`

Estos datos están en `appsettings.Development.json`. Son solo para su PC: cámbielos antes de usar el sistema con datos reales.

La base queda en `datos/spin.db` y las migraciones se aplican solas al iniciar.

---

## Primeros pasos dentro del sistema

1. **Inicio**: muestra la configuración inicial en 9 pasos, con datos reales de su base.
2. **Plantas**: registre al menos una (por ejemplo, `LUR`, Planta Lurín).
3. **Almacenes**: uno de insumos, uno de producto terminado y uno de cuarentena por planta.
4. **Líneas de producción**: elija el envase (PET, vidrio, lata) y los formatos que embotella.
5. **Usuarios**: cree el equipo con su rol y una contraseña temporal.
6. **Proveedores**: registre los proveedores nacionales (con RUC) y los importadores.
7. **Insumos**: registre cada insumo y, en su panel de detalle, agregue sus proveedores y marque el principal.
8. **Productos y recetas**: registre cada SKU y defina su receta. Pruébela con «Calcular consumo».
9. **Movimientos**: cargue el «Stock inicial» de cada insumo en su almacén (o haga un **Conteo físico**). Revise **Stock** y **Kardex**.
10. **Ventas**: importe el historial de ventas (ideal: 1 año o más) y el stock de **Producto terminado**.

11. **Planificación**: revise el **Pronóstico de demanda** y la **Reposición de insumos** (se entrena solo cada lunes y al importar ventas).
12. **Día a día**: registre la **Producción** (partes) y los **Pedidos en camino** con su recepción.

Atajo: los pasos 6 a 10 se pueden cargar desde Excel en **Importar datos**. Pruébelo con los archivos de `ejemplos/`.

## Datos de demostración (Entrega 10)

Ponga `SPIN_DEMO=true` en `.env` y levante con una base vacía (`docker compose down -v` borra la actual). Usuarios `administrador@andina.demo`, `almacen@`, `planificacion@`, `compras@`, `gerencia@andina.demo`, clave `Demo.Spin2026`. Detalles en `ENTREGA-10.md`.

## Cada entrega siguiente

1. Copie los archivos nuevos o reemplazados que indique la entrega.
2. Cree su migración: `.\scripts\migracion.ps1 -Nombre Entrega02` (el nombre lo indica cada entrega).
3. `docker compose up --build -d`. Los datos existentes se conservan.

## Estructura (igual que SGMG)

```
Controllers/          Vistas (devuelven la página)
Controllers/Api/      API JSON que consume el frontend (/api/...)
Services/             Interfaces + ServiceImpl (reglas de negocio)
Repository/           Interfaces + RepositoryImpl (consultas EF Core)
Dtos/Request|Response GenericResponse<T>, ListadoResponse<T>, solicitudes validadas
Models/               Entidades
Data/                 ApplicationDbContext, DbInitializer, Migrations
common/exception      GenericException, GlobalExceptionFilter, ValidationFilter
common/seguridad      Roles, multiempresa (TenantProvider), claims, mensajes de Identity
Views/                Razor (HTML) por módulo
wwwroot/css/spin.css  Sistema visual (variables de color en :root)
wwwroot/js/           spin.js (común) + un archivo por módulo
```

## Problemas frecuentes

| Síntoma | Causa y solución |
|---|---|
| `El proyecto no tiene migraciones` al iniciar | Falta el paso 2: `.\scripts\migracion.ps1 -Nombre Entrega01` |
| `The model for context 'ApplicationDbContext' has pending changes` | Copió una entrega nueva y falta su migración: cree la migración que indica la entrega |
| `Defina SPIN_ADMIN_EMAIL en el archivo .env` | No existe `.env`: `Copy-Item .env.example .env` |
| No puedo ingresar con el admin | Se crea solo cuando la base está vacía. Si cambió la clave en `.env` después, use la anterior o borre los datos con `docker compose down -v` |
| El puerto 8080 está ocupado | En `.env` cambie `SPIN_PUERTO=8081` y abra http://localhost:8081 |
| `dotnet ef` no se reconoce | Ejecute `dotnet tool restore` en la carpeta del proyecto |
| Errores de compilación en `obj/` después de usar el script con Docker | Borre las carpetas `bin` y `obj` y vuelva a compilar |

## Seguridad aplicada

- Contraseñas con hash de ASP.NET Core Identity. Ninguna clave real va en el repositorio: el `.env` está en `.gitignore`.
- Autorización por rol en **cada endpoint** (el menú oculto no es la única protección).
- Multiempresa con filtro global de EF Core: un usuario no puede leer ni modificar datos de otra empresa, aunque cambie el id en la URL.
- CSRF en formularios y en `fetch` (cabecera `X-CSRF-TOKEN`). Cookies `HttpOnly` y `SameSite`.
- Errores sin detalles internos hacia el navegador. El detalle queda en el log, con su referencia.
- Cabeceras `Content-Security-Policy`, `X-Frame-Options`, `X-Content-Type-Options` y `Referrer-Policy`.
- Docker ejecuta la aplicación con el usuario sin privilegios de la imagen oficial.

Pendiente para producción (fuera del alcance local): HTTPS con un proxy inverso y certificado.

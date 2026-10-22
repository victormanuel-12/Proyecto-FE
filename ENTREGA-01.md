# Entrega 0 y 1: notas de entrega

**Fecha:** 2026-10-21 · **Destino:** .NET 10 (SDK 10.x) · **Paquetes:** EF Core 10.0.13, Identity EF Core 10.0.13, dotnet-ef 10.0.13 (versiones estables verificadas en nuget.org el 2026-10-21)

## Acción sobre los archivos

Es un **proyecto nuevo**: copie la carpeta `SPIn` completa. No reemplaza nada de SGMG.

## Migración de esta entrega

```powershell
.\scripts\migracion.ps1 -Nombre Entrega01
```

## Verificación

| Verificación | Estado |
|---|---|
| Versiones de paquetes compatibles con .NET 10 | Hecha (consultadas en nuget.org) |
| Frontend en navegador real (Chromium) con la API simulada: listado, paginación, franja de estados, búsqueda, estado vacío, modal con errores por campo, error 409, alta, CSRF enviado, confirmación, cancelar con Esc, botón deshabilitado por dependencias, líneas con envase y formatos, error de red con Reintentar, vista móvil sin desplazamiento horizontal | Hecha: 22 de 22 comprobaciones correctas |
| Algoritmo de RUC probado con RUC reales | Hecha |
| `dotnet build` / `dotnet run` / creación de la migración | **Pendiente: la ejecuta usted.** El entorno donde se preparó la entrega no tiene acceso a NuGet ni al SDK de .NET |
| Pruebas automatizadas del backend (xUnit) | Planificadas para la Entrega 10 |

Si algún paso falla, copie el mensaje de error completo y lo corrijo en esta misma entrega.

## Prueba manual sugerida (5 minutos)

1. Ingrese como superadministrador.
2. Plantas: cree `LUR` y luego intente crear otra con `lur` (debe decir que ya existe).
3. Almacenes: cree uno de insumos en LUR. Intente desactivar LUR (debe impedirlo).
4. Líneas: cree `L1` PET con 500 ml y 3 L.
5. Usuarios: cree un Jefe de almacén e ingrese con él en otra ventana privada (debe ver solo el Inicio).
6. Desactive a ese usuario: en 1 minuto como máximo su sesión se cierra.
7. Empresas: cree una segunda empresa y un Administrador para ella. Ingrese con él: no debe ver las plantas de la primera.

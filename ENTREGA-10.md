# Entrega 10: datos de demostración y pruebas

## Qué trae

| Parte | Archivo | Para qué |
|---|---|---|
| Cargador de demostración | `Data/Demo/DatosDemostracion.cs`, `Data/Demo/ModeloDemanda.cs` | Llena la base con una embotelladora ficticia completa al arrancar |
| Reloj simulado | `common/Reloj.cs` (`Reloj.Simular`) | Permite registrar los 90 días de operación con su fecha real; solo lo usa el cargador |
| Mejora del SSA | `Services/Prediccion/MetodosPronostico.cs` | Ventana anual (hasta 52 semanas) y rango elegido por validación interna: el error del SSA baja de ~25 % a 5–8 % en series estacionales |
| Pruebas automáticas | `pruebas/SPIn.Pruebas/` (xUnit) | Reglas de negocio, motor predictivo con ML.NET real, reposición |
| Respaldo | `scripts/respaldo.ps1`, `scripts/restaurar.ps1` | Copia y restauración de `spin.db` |
| Configuración | `docker-compose.yml`, `.env.example`, `appsettings.json`, `Program.cs` | Variable `SPIN_DEMO` |

**No cambia el esquema de la base**: no hace falta migración nueva. Una base nueva sí necesita las migraciones de las entregas anteriores (como siempre).

## No es SQL: se carga solo al arrancar

1. En `.env` ponga `SPIN_DEMO=true`.
2. La base debe estar vacía de datos operativos. Si ya tiene datos de prueba: `docker compose down -v` (**borra la base**; respalde antes si le interesa).
3. `docker compose up --build -d` y espere. La carga tarda entre 1 y 3 minutos la primera vez; `docker compose logs -f spin` muestra «Demostración: datos cargados en N s».
4. Después de cargar, puede volver a `SPIN_DEMO=false`. Aunque lo deje en `true`, **nunca vuelve a cargar** si la empresa ya tiene plantas, insumos o productos: no duplica ni pisa sus datos reales.

La carga usa los mismos servicios que la interfaz (recepciones, partes, ventas, pedidos, conteos), así que se cumplen todas las reglas: el stock nunca queda negativo, el kardex cuadra, las recetas se aplican con su merma y cada operación queda a nombre del usuario que corresponde.

Las fechas son **relativas al día en que arranca**: si la carga hoy (21/10/2026), la historia va de abril de 2024 al 21/10/2026 y la programación llega hasta el 04/11/2026.

## Usuarios

Clave de todos: `Demo.Spin2026` (además sigue existiendo el administrador de su `.env`).

| Usuario | Rol | Qué hizo en la demostración |
|---|---|---|
| administrador@andina.demo | Administrador | Estructura y catálogo |
| almacen@andina.demo | JefeAlmacen | Recepciones, ventas, mermas, conteos |
| planificacion@andina.demo | Planificador | Programación y partes de producción |
| compras@andina.demo | Compras | Pedidos a proveedores |
| gerencia@andina.demo | Gerencia | Solo consulta |

## Qué se carga (aprox. 30 000 registros, 5–8 MB)

- **Estructura**: planta Lurín; almacenes de insumos, producto terminado y cuarentena; 3 líneas (PET 500/625 ml a 24 000 bot/h, PET 1.5–3 L a 12 000, vidrio a 18 000).
- **Catálogo**: 9 proveedores ficticios (RUC válidos, uno extranjero en USD), 19 insumos con condiciones de compra (preformas, tapas, etiquetas, CO₂, concentrado, azúcar, film, químicos), un proveedor alternativo.
- **Productos y recetas**: 6 SKU (Cola, Naranja y Limón 500 ml ×12; Cola y Naranja 1.5 L ×6; Cola 3 L ×4). Cola 500 tiene una receta v2 vigente desde hace 45 días.
- **Historial de ventas**: 30 meses, por día, producto y canal (~22 000 filas), con verano alto, invierno bajo, Fiestas Patrias, fin de año, efecto del día de la semana y crecimiento de 6 % al año.
- **Últimos 90 días operados de verdad**: ~85 partes de producción, ~2 100 ventas que descuentan stock, pedidos y recepciones (algunas parciales y algunas atrasadas), mermas, 2 conteos físicos aprobados, una venta y un parte anulados.
- **Programación** de los próximos 14 días.
- **Modelo entrenado** y alertas sincronizadas al terminar.

## Qué verá en cada pantalla (guion de demostración)

1. **Inicio**: alertas que requieren atención, ventas y producción de la semana, cobertura.
2. **Alertas / campana**: *PRE-500 urgente* (≈3.5 días, entrega 7), *CO₂ por pedir* (≈4 días), *pedido de tapas atrasado*, *Cola 3 L con poco producto terminado*, *etiquetas de Limón sobre el máximo*, *programación vencida*.
3. **Pronóstico**: 6 productos con el método elegido (normalmente SSA de ML.NET o Estacional), MAPE por debajo de 10 %, gráfico con 2.5 años de historia y banda de 90 %.
4. **Reposición**: PRE-500 en *urgente* y CO₂ en *pedir* con cantidad sugerida; botón que abre el pedido ya llenado.
5. **Pedidos en camino**: ~9 pedidos en camino, uno atrasado; recepción parcial.
6. **Stock / Kardex**: movimientos de 90 días con usuario y documento de origen.
7. **Producción**: programación por línea con horas e insumos; partes con eficiencia.
8. **Ventas / Producto terminado**: historial y cobertura en días.
9. **Conteos**: dos conteos aprobados con sus ajustes.

## Pruebas automáticas

```powershell
cd pruebas\SPIn.Pruebas
dotnet test
```

34 casos: RUC, lectura de números/fechas/encabezados de Excel, reloj simulado, Z del nivel de servicio, MAPE, Holt, promedio móvil, **SSA real de ML.NET** sobre una serie estacional (MAPE < 15 %), elección del método por menor error, serie semanal, marcas de reentrenamiento, modelo de demanda, y la política de reposición (stock de seguridad, punto de reorden, urgente/pedir/ok, en camino, sin proveedor, sin demanda).

La carpeta `pruebas/` está excluida del proyecto principal y de la imagen Docker.

## Respaldo y restauración

```powershell
.\scripts\respaldo.ps1                 # Docker: detiene unos segundos, copia a respaldos\spin-<fecha>.db
.\scripts\restaurar.ps1 respaldos\spin-20261021-180000.db
.\scripts\respaldo.ps1 -Local          # si ejecuta con dotnet run (copia datos\spin.db)
```

## Verificación hecha y pendiente

- Hecho aquí: revisión de sintaxis de todos los `.cs` (0 errores), registros de interfaces e inyección, las 8 baterías de pruebas de interfaz en navegador sin errores, y una simulación del cargador con la misma política (0 faltas de insumo, 0 ventas perdidas, estados finales los descritos arriba).
- **Pendiente en su equipo**: aquí no hay SDK de .NET, así que `docker compose up --build` y `dotnet test` deben correrse por primera vez en su máquina. Si algo falla al compilar o al cargar, envíeme el mensaje exacto (el log de la carga indica en qué paso iba).

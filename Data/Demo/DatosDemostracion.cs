using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SPIn.Common;
using SPIn.Common.Excepciones;
using SPIn.Common.Seguridad;
using SPIn.Dtos.Request;
using SPIn.Models;
using SPIn.Repository;
using SPIn.Services;

namespace SPIn.Data.Demo;

/// <summary>
/// Datos de demostración (Entrega 10). Se activan con <c>Demo:Cargar = true</c> (en Docker: SPIN_DEMO=true en .env)
/// y solo se cargan si la empresa inicial está vacía. Usan los mismos servicios que las pantallas, así que respetan
/// todas las reglas (stock nunca negativo, kardex cuadrado, recetas por versión). Las fechas se calculan desde hoy:
///   · 30 meses de historial de ventas (para que el pronóstico aprenda la estacionalidad);
///   · 90 días de operación día a día: producción, ventas con despacho, compras y recepciones, conteos y mermas;
///   · estado final con situaciones para todas las alertas, programación futura y el pronóstico entrenado.
/// </summary>
public static class DatosDemostracion
{
    public const string ClaveUsuarios = "Demo.Spin2026";

    public static async Task CargarAsync(IServiceProvider servicios)
    {
        var logger = servicios.GetRequiredService<ILoggerFactory>().CreateLogger("SPIn.Demo");
        int empresaId;
        using (var scope = servicios.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var empresa = await db.Empresas.OrderBy(e => e.Id).FirstOrDefaultAsync(e => e.Activo);
            if (empresa is null)
            {
                logger.LogWarning("Demostración: no hay una empresa activa.");
                return;
            }
            empresaId = empresa.Id;
            var tieneDatos = await db.Plantas.IgnoreQueryFilters().AnyAsync(p => p.EmpresaId == empresaId)
                             || await db.Insumos.IgnoreQueryFilters().AnyAsync(i => i.EmpresaId == empresaId)
                             || await db.Productos.IgnoreQueryFilters().AnyAsync(p => p.EmpresaId == empresaId);
            if (tieneDatos)
            {
                logger.LogInformation("Demostración: la empresa {Empresa} ya tiene datos; no se carga nada.", empresa.RazonSocial);
                return;
            }
        }

        var reloj = System.Diagnostics.Stopwatch.StartNew();
        logger.LogInformation("Demostración: cargando datos (1 a 3 minutos)…");
        try
        {
            await new Carga(servicios, empresaId, logger).EjecutarAsync();
            logger.LogInformation("Demostración: datos cargados en {Segundos:0} s. Usuarios de prueba con clave {Clave}.", reloj.Elapsed.TotalSeconds, ClaveUsuarios);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demostración: la carga se interrumpió. Borre los datos (docker compose down -v) y vuelva a iniciar con SPIN_DEMO=true.");
        }
    }

    // ======================================================================================================
    private sealed class Carga(IServiceProvider servicios, int empresaId, ILogger logger)
    {
        private readonly DateOnly _hoy = Reloj.Hoy;
        private DateOnly Inicio => _hoy.AddDays(-912);     // 30 meses de historial
        private DateOnly InicioOperacion => _hoy.AddDays(-90);
        private ModeloDemanda _demanda = null!;

        // ids
        private int _almIns, _almPt, _almCua;
        private readonly Dictionary<string, int> _lineas = [];
        private readonly Dictionary<string, int> _proveedores = [];
        private readonly Dictionary<string, InsumoDemo> _insumos = [];
        private readonly Dictionary<string, ProductoDemo> _productos = [];
        private readonly List<PedidoSimulado> _pedidos = [];
        private int _guia = 4100, _documento = 120000;

        private sealed record InsumoDemo(string Codigo, string Nombre, string Categoria, string UCompra, string UConsumo, decimal Factor,
            decimal Minimo, decimal Maximo, decimal? Capacidad, bool Critico, bool Importado, decimal Servicio, TipoConsumo Tipo,
            decimal? ConsumoDiario, bool Lote, string Proveedor, decimal Precio, Moneda Moneda, int Entrega, int Variacion,
            decimal PedidoMinimo, decimal Multiplo)
        {
            public int Id { get; set; }
            public int ProveedorId { get; set; }
        }

        private sealed record ProductoDemo(string Codigo, string Nombre, string Sabor, string Formato, int PorPaquete, int PorPallet,
            double Base, string Linea)
        {
            public int Id { get; set; }
        }

        private sealed class PedidoSimulado
        {
            public int Id { get; set; }
            public DateOnly Llegada { get; set; }
            public bool Parcial { get; set; }
            public bool Recibido { get; set; }
        }

        // ---------------------------------------------------------------- utilidades de ejecución
        /// <summary>Ejecuta en un scope nuevo (contexto de EF limpio), con la empresa fijada y, si se indica, la hora simulada.</summary>
        private async Task<T> EnScopeAsync<T>(Func<IServiceProvider, Task<T>> accion, DateTime? hora = null, string usuario = Admin)
        {
            using var scope = servicios.CreateScope();
            scope.ServiceProvider.GetRequiredService<TenantProvider>().Establecer(empresaId, usuario);
            using (hora is DateTime h ? Reloj.Simular(h) : null)
            {
                return await accion(scope.ServiceProvider);
            }
        }

        private Task EnScopeAsync(Func<IServiceProvider, Task> accion, DateTime? hora = null, string usuario = Admin) =>
            EnScopeAsync<bool>(async sp => { await accion(sp); return true; }, hora, usuario);

        // Quién registra cada operación (usuarios de demostración por rol).
        private const string Admin = "administrador@andina.demo";
        private const string Almacen = "almacen@andina.demo";
        private const string Planificacion = "planificacion@andina.demo";
        private const string Compras = "compras@andina.demo";

        private static DateTime A(DateOnly d, int hora, int minuto = 0) => d.ToDateTime(new TimeOnly(hora, minuto));

        private static T Dato<T>(Dtos.Response.GenericResponse<T> r, string que) =>
            r.Data ?? throw new InvalidOperationException($"Demostración: no se obtuvo {que}.");

        // ---------------------------------------------------------------- orden general
        public async Task EjecutarAsync()
        {
            _demanda = new ModeloDemanda(Inicio, 2026);
            await EstructuraAsync();
            await UsuariosAsync();
            await CatalogoAsync();
            await ProductosYRecetasAsync();
            await HistorialVentasAsync();
            await StockInicialAsync();
            await OperacionAsync();
            await ProgramacionFuturaAsync();
            await EntrenarYAlertarAsync();
        }

        // ---------------------------------------------------------------- 1. estructura
        private async Task EstructuraAsync()
        {
            await EnScopeAsync(async sp =>
            {
                var plantas = sp.GetRequiredService<IPlantaService>();
                var almacenes = sp.GetRequiredService<IAlmacenService>();
                var lineas = sp.GetRequiredService<ILineaService>();
                var db = sp.GetRequiredService<ApplicationDbContext>();

                var planta = Dato(await plantas.CrearAsync(new PlantaRequest { Codigo = "LUR", Nombre = "Planta Lurín", Direccion = "Av. Industrial 1450, Lurín, Lima" }), "la planta").Id;
                _almIns = Dato(await almacenes.CrearAsync(new AlmacenRequest { Codigo = "LUR-INS", Nombre = "Almacén de insumos Lurín", PlantaId = planta, Tipo = TipoAlmacen.Insumos }), "almacén").Id;
                _almPt = Dato(await almacenes.CrearAsync(new AlmacenRequest { Codigo = "LUR-PT", Nombre = "Producto terminado Lurín", PlantaId = planta, Tipo = TipoAlmacen.ProductoTerminado }), "almacén").Id;
                _almCua = Dato(await almacenes.CrearAsync(new AlmacenRequest { Codigo = "LUR-CUA", Nombre = "Cuarentena Lurín", PlantaId = planta, Tipo = TipoAlmacen.Cuarentena }), "almacén").Id;

                var formatos = await db.Formatos.AsNoTracking().ToDictionaryAsync(f => f.Nombre, f => f.Id);
                int F(string nombre) => formatos.TryGetValue(nombre, out var id) ? id : throw new InvalidOperationException($"Falta el formato {nombre}.");
                _lineas["L1"] = Dato(await lineas.CrearAsync(new LineaRequest
                {
                    Codigo = "L1-PET", Nombre = "Línea 1 · PET personal", PlantaId = planta, TipoEnvase = TipoEnvase.Pet, VelocidadNominal = 24000,
                    FormatoIds = [F("PET 500 ml"), F("PET 625 ml")]
                }), "línea").Id;
                _lineas["L2"] = Dato(await lineas.CrearAsync(new LineaRequest
                {
                    Codigo = "L2-PET", Nombre = "Línea 2 · PET familiar", PlantaId = planta, TipoEnvase = TipoEnvase.Pet, VelocidadNominal = 12000,
                    FormatoIds = [F("PET 1.5 L"), F("PET 2.25 L"), F("PET 3 L")]
                }), "línea").Id;
                _lineas["L3"] = Dato(await lineas.CrearAsync(new LineaRequest
                {
                    Codigo = "L3-VID", Nombre = "Línea 3 · Vidrio retornable", PlantaId = planta, TipoEnvase = TipoEnvase.VidrioRetornable, VelocidadNominal = 18000,
                    FormatoIds = [F("Vidrio 296 ml"), F("Vidrio 1 L")]
                }), "línea").Id;
            }, A(Inicio.AddDays(-30), 9));
            logger.LogInformation("Demostración: estructura creada.");
        }

        // ---------------------------------------------------------------- 2. usuarios por rol
        private async Task UsuariosAsync()
        {
            (string Email, string Nombre, string Rol)[] usuarios =
            [
                ("administrador@andina.demo", "Miguel Torres Vega", Roles.Administrador),
                ("almacen@andina.demo", "Carlos Huamán Quispe", Roles.JefeAlmacen),
                ("planificacion@andina.demo", "Lucía Mendoza Ríos", Roles.Planificador),
                ("compras@andina.demo", "Jorge Salazar Paredes", Roles.Compras),
                ("gerencia@andina.demo", "Patricia Rojas Lazo", Roles.Gerencia)
            ];
            await EnScopeAsync(async sp =>
            {
                var um = sp.GetRequiredService<UserManager<ApplicationUser>>();
                foreach (var (email, nombre, rol) in usuarios)
                {
                    if (await um.FindByEmailAsync(email) is not null) continue;
                    var u = new ApplicationUser
                    {
                        UserName = email, Email = email, EmailConfirmed = true, NombreCompleto = nombre,
                        EmpresaId = empresaId, Activo = true, CreadoEn = DateTime.UtcNow
                    };
                    var r = await um.CreateAsync(u, ClaveUsuarios);
                    if (!r.Succeeded) throw new InvalidOperationException("No se pudo crear " + email + ": " + string.Join(" ", r.Errors.Select(e => e.Description)));
                    await um.AddToRoleAsync(u, rol);
                }
            });
        }

        // ---------------------------------------------------------------- 3. proveedores, insumos y condiciones
        private static string Ruc(string base10)
        {
            int[] pesos = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];
            var suma = 0;
            for (var i = 0; i < 10; i++) suma += (base10[i] - '0') * pesos[i];
            var resto = 11 - suma % 11;
            return base10 + (resto switch { 10 => 0, 11 => 1, _ => resto });
        }

        private async Task CatalogoAsync()
        {
            (string Clave, string Pais, string? Ruc, string Razon, string Comercial, string Contacto, string Telefono, string Email)[] proveedores =
            [
                ("PRE", "Perú", Ruc("2060112233"), "Preformas del Pacífico S.A.C.", "PrefoPac", "Rosa Quispe", "01 614 5000", "ventas@prefopac.demo"),
                ("PRE2", "Perú", Ruc("2060223344"), "Envases Plásticos Callao S.A.C.", "EnvaCallao", "Luis Ramos", "01 465 2200", "pedidos@envacallao.demo"),
                ("TAP", "Perú", Ruc("2060334455"), "Tapas y Cierres Andinos S.A.", "TapAndes", "Ana Torres", "01 326 7788", "ventas@tapandes.demo"),
                ("ETQ", "Perú", Ruc("2060445566"), "Etiquetas Impresas Lima S.A.C.", "EtiLima", "Diego Flores", "01 442 1010", "atencion@etilima.demo"),
                ("CO2", "Perú", Ruc("2060556677"), "Gases Industriales del Sur S.A.", "GasSur", "Elena Vargas", "01 717 3030", "co2@gassur.demo"),
                ("CON", "México", null, "Concentrados del Caribe S.A. de C.V.", "ConCaribe", "María López", "+52 55 5000 1000", "export@concaribe.demo"),
                ("AZU", "Perú", Ruc("2060667788"), "Azucarera Valle Norte S.A.A.", "Valle Norte", "Jorge Díaz", "074 482 000", "ventas@vallenorte.demo"),
                ("FIL", "Perú", Ruc("2060778899"), "Películas Plásticas Perú S.A.C.", "PeliPlast", "Sofía Chávez", "01 555 8080", "ventas@peliplast.demo"),
                ("QUI", "Perú", Ruc("2060889900"), "Química Industrial Lurín S.R.L.", "QuimLur", "Raúl Medina", "01 430 9090", "pedidos@quimlur.demo")
            ];

            InsumoDemo I(string c, string n, string cat, string uc, string ucon, decimal f, decimal min, decimal max, decimal? cap, bool crit, bool imp,
                decimal serv, TipoConsumo t, decimal? cd, bool lote, string prov, decimal precio, Moneda m, int ent, int var_, decimal pmin, decimal mult) =>
                new(c, n, cat, uc, ucon, f, min, max, cap, crit, imp, serv, t, cd, lote, prov, precio, m, ent, var_, pmin, mult);

            var lista = new[]
            {
                I("PRE-500", "Preforma PET 500 ml 20 g", "Preformas", "MIL", "UND", 1000, 40000, 400000, 600000, true, false, 97, TipoConsumo.PorProduccion, null, true, "PRE", 92.50m, Moneda.PEN, 7, 2, 50, 10),
                I("PRE-1500", "Preforma PET 1.5 L 42 g", "Preformas", "MIL", "UND", 1000, 12000, 100000, null, true, false, 97, TipoConsumo.PorProduccion, null, true, "PRE", 165m, Moneda.PEN, 7, 2, 20, 10),
                I("PRE-3000", "Preforma PET 3 L 62 g", "Preformas", "MIL", "UND", 1000, 3000, 25000, null, true, false, 95, TipoConsumo.PorProduccion, null, true, "PRE", 240m, Moneda.PEN, 7, 2, 5, 5),
                I("TAPA-28", "Tapa plástica 28 mm", "Tapas", "MIL", "UND", 1000, 60000, 500000, null, true, false, 97, TipoConsumo.PorProduccion, null, false, "TAP", 38m, Moneda.PEN, 5, 1, 100, 50),
                I("ETQ-COLA-500", "Etiqueta Cola 500 ml", "Etiquetas", "ROL", "UND", 5000, 25000, 250000, null, false, false, 95, TipoConsumo.PorProduccion, null, false, "ETQ", 210m, Moneda.PEN, 10, 3, 5, 1),
                I("ETQ-NAR-500", "Etiqueta Naranja 500 ml", "Etiquetas", "ROL", "UND", 5000, 10000, 100000, null, false, false, 95, TipoConsumo.PorProduccion, null, false, "ETQ", 210m, Moneda.PEN, 10, 3, 5, 1),
                I("ETQ-LIM-500", "Etiqueta Limón 500 ml", "Etiquetas", "ROL", "UND", 5000, 5000, 40000, null, false, false, 95, TipoConsumo.PorProduccion, null, false, "ETQ", 210m, Moneda.PEN, 10, 3, 5, 1),
                I("ETQ-COLA-1500", "Etiqueta Cola 1.5 L", "Etiquetas", "ROL", "UND", 5000, 8000, 80000, null, false, false, 95, TipoConsumo.PorProduccion, null, false, "ETQ", 230m, Moneda.PEN, 10, 3, 3, 1),
                I("ETQ-NAR-1500", "Etiqueta Naranja 1.5 L", "Etiquetas", "ROL", "UND", 5000, 4000, 40000, null, false, false, 95, TipoConsumo.PorProduccion, null, false, "ETQ", 230m, Moneda.PEN, 10, 3, 3, 1),
                I("ETQ-COLA-3000", "Etiqueta Cola 3 L", "Etiquetas", "ROL", "UND", 2500, 2500, 20000, null, false, false, 95, TipoConsumo.PorProduccion, null, false, "ETQ", 180m, Moneda.PEN, 10, 3, 2, 1),
                I("CO2", "Dióxido de carbono grado alimentario", "CO₂", "KG", "KG", 1, 200, 1500, 2000, true, false, 97, TipoConsumo.PorProduccion, null, false, "CO2", 2.10m, Moneda.PEN, 3, 1, 500, 100),
                I("CONC-COLA", "Concentrado sabor cola", "Concentrado y jarabe", "CJ", "L", 20, 300, 1500, null, true, true, 98, TipoConsumo.PorProduccion, null, true, "CON", 370m, Moneda.USD, 30, 7, 10, 5),
                I("CONC-NAR", "Concentrado sabor naranja", "Concentrado y jarabe", "CJ", "L", 20, 100, 600, null, true, true, 98, TipoConsumo.PorProduccion, null, true, "CON", 330m, Moneda.USD, 30, 7, 10, 5),
                I("CONC-LIM", "Concentrado sabor limón", "Concentrado y jarabe", "CJ", "L", 20, 30, 200, null, true, true, 98, TipoConsumo.PorProduccion, null, true, "CON", 330m, Moneda.USD, 30, 7, 5, 5),
                I("AZUCAR", "Azúcar rubia", "Azúcar", "SAC", "KG", 50, 3000, 30000, 40000, true, false, 95, TipoConsumo.PorProduccion, null, false, "AZU", 145m, Moneda.PEN, 4, 1, 100, 20),
                I("FILM-TERMO", "Film termoencogible", "Embalaje", "ROL", "KG", 25, 200, 1500, null, false, false, 90, TipoConsumo.PorProduccion, null, false, "FIL", 180m, Moneda.PEN, 6, 2, 10, 5),
                I("FILM-STRETCH", "Film stretch para pallets", "Embalaje", "ROL", "KG", 16, 30, 200, null, false, false, 90, TipoConsumo.PorProduccion, null, false, "FIL", 95m, Moneda.PEN, 6, 2, 5, 5),
                I("LUBRICANTE", "Lubricante de transportador grado alimentario", "Insumos químicos", "L", "L", 1, 40, 200, null, false, false, 90, TipoConsumo.PorOperacion, 3, false, "QUI", 28m, Moneda.PEN, 5, 1, 20, 10),
                I("SODA", "Soda cáustica en escamas", "Insumos químicos", "SAC", "KG", 25, 50, 400, null, false, false, 90, TipoConsumo.Manual, 5, false, "QUI", 95m, Moneda.PEN, 5, 1, 4, 2)
            };

            await EnScopeAsync(async sp =>
            {
                var db = sp.GetRequiredService<ApplicationDbContext>();
                var provServ = sp.GetRequiredService<IProveedorService>();
                var insServ = sp.GetRequiredService<IInsumoService>();
                var condServ = sp.GetRequiredService<ICondicionCompraService>();

                foreach (var p in proveedores)
                {
                    _proveedores[p.Clave] = Dato(await provServ.CrearAsync(new ProveedorRequest
                    {
                        Pais = p.Pais, Ruc = p.Ruc, RazonSocial = p.Razon, NombreComercial = p.Comercial, Contacto = p.Contacto, Telefono = p.Telefono, Email = p.Email
                    }), "proveedor").Id;
                }

                var categorias = await db.Categorias.AsNoTracking().ToDictionaryAsync(c => c.Nombre, c => c.Id);
                var unidades = await db.Unidades.AsNoTracking().ToDictionaryAsync(u => u.Codigo, u => u.Id);
                foreach (var i in lista)
                {
                    i.Id = Dato(await insServ.CrearAsync(new InsumoRequest
                    {
                        Codigo = i.Codigo, Nombre = i.Nombre, CategoriaId = categorias[i.Categoria], UnidadCompraId = unidades[i.UCompra],
                        UnidadConsumoId = unidades[i.UConsumo], FactorConversion = i.Factor, StockMinimo = i.Minimo, StockMaximo = i.Maximo,
                        CapacidadMaxima = i.Capacidad, EsCritico = i.Critico, EsImportado = i.Importado, NivelServicio = i.Servicio,
                        TipoConsumo = i.Tipo, ConsumoEstimadoDiario = i.ConsumoDiario, ControlaLote = i.Lote
                    }), "insumo").Id;
                    i.ProveedorId = _proveedores[i.Proveedor];
                    _insumos[i.Codigo] = i;
                    await condServ.CrearAsync(new CondicionCompraRequest
                    {
                        InsumoId = i.Id, ProveedorId = i.ProveedorId, Moneda = i.Moneda, PrecioUnitario = i.Precio, TiempoEntregaDias = i.Entrega,
                        VariacionEntregaDias = i.Variacion, PedidoMinimo = i.PedidoMinimo, MultiploCompra = i.Multiplo, EsPrincipal = true
                    });
                }
                // Proveedor alternativo de preformas (más caro y más rápido): muestra la comparación de condiciones.
                await condServ.CrearAsync(new CondicionCompraRequest
                {
                    InsumoId = _insumos["PRE-500"].Id, ProveedorId = _proveedores["PRE2"], Moneda = Moneda.PEN, PrecioUnitario = 96.80m,
                    TiempoEntregaDias = 4, VariacionEntregaDias = 1, PedidoMinimo = 20, MultiploCompra = 10, EsPrincipal = false
                });
            }, A(Inicio.AddDays(-20), 10));
            logger.LogInformation("Demostración: {Proveedores} proveedores y {Insumos} insumos.", _proveedores.Count, _insumos.Count);
        }

        // ---------------------------------------------------------------- 4. productos y recetas
        private async Task ProductosYRecetasAsync()
        {
            ProductoDemo[] productos =
            [
                new("COLA-500-12", "Cola Andina 500 ml × 12", "Cola", "PET 500 ml", 12, 84, 300, "L1"),
                new("NAR-500-12", "Naranja Andina 500 ml × 12", "Naranja", "PET 500 ml", 12, 84, 110, "L1"),
                new("LIM-500-12", "Limón Andina 500 ml × 12", "Limón", "PET 500 ml", 12, 84, 60, "L1"),
                new("COLA-1500-6", "Cola Andina 1.5 L × 6", "Cola", "PET 1.5 L", 6, 70, 160, "L2"),
                new("NAR-1500-6", "Naranja Andina 1.5 L × 6", "Naranja", "PET 1.5 L", 6, 70, 80, "L2"),
                new("COLA-3000-4", "Cola Andina 3 L × 4", "Cola", "PET 3 L", 4, 60, 70, "L2")
            ];

            await EnScopeAsync(async sp =>
            {
                var db = sp.GetRequiredService<ApplicationDbContext>();
                var prodServ = sp.GetRequiredService<IProductoService>();
                var formatos = await db.Formatos.AsNoTracking().ToDictionaryAsync(f => f.Nombre, f => f.Id);
                foreach (var p in productos)
                {
                    p.Id = Dato(await prodServ.CrearAsync(new ProductoRequest
                    {
                        Codigo = p.Codigo, Nombre = p.Nombre, Sabor = p.Sabor, FormatoId = formatos[p.Formato],
                        UnidadesPorPaquete = p.PorPaquete, PaquetesPorPallet = p.PorPallet
                    }), "producto").Id;
                    _productos[p.Codigo] = p;
                }
            }, A(Inicio.AddDays(-15), 10));

            // Recetas (cantidad por botella salvo que se indique; merma en %).
            RecetaDetalleRequest L(string insumo, decimal cantidad, decimal merma, NivelReceta nivel = NivelReceta.PorBotella) =>
                new() { InsumoId = _insumos[insumo].Id, Cantidad = cantidad, MermaPorcentaje = merma, Nivel = nivel };

            List<RecetaDetalleRequest> Personal(string sabor, decimal azucar, decimal co2) =>
            [
                L("PRE-500", 1, 0.5m), L("TAPA-28", 1, 0.8m), L($"ETQ-{sabor}-500", 1, 1m), L("CO2", co2, 3m),
                L($"CONC-{sabor}", 0.0025m, 0.5m), L("AZUCAR", azucar, 0.5m),
                L("FILM-TERMO", 0.035m, 2m, NivelReceta.PorPaquete), L("FILM-STRETCH", 0.35m, 1m, NivelReceta.PorPallet)
            ];
            List<RecetaDetalleRequest> Familiar(string sabor) =>
            [
                L("PRE-1500", 1, 0.5m), L("TAPA-28", 1, 0.8m), L($"ETQ-{sabor}-1500", 1, 1m), L("CO2", 0.009m, 3m),
                L($"CONC-{sabor}", 0.0075m, 0.5m), L("AZUCAR", 0.13m, 0.5m),
                L("FILM-TERMO", 0.05m, 2m, NivelReceta.PorPaquete), L("FILM-STRETCH", 0.35m, 1m, NivelReceta.PorPallet)
            ];
            var recetas = new (string Producto, List<RecetaDetalleRequest> Lineas)[]
            {
                ("COLA-500-12", Personal("COLA", 0.045m, 0.0042m)),
                ("NAR-500-12", Personal("NAR", 0.050m, 0.0040m)),
                ("LIM-500-12", Personal("LIM", 0.048m, 0.0040m)),
                ("COLA-1500-6", Familiar("COLA")),
                ("NAR-1500-6", Familiar("NAR")),
                ("COLA-3000-4", new List<RecetaDetalleRequest>
                {
                    L("PRE-3000", 1, 0.5m), L("TAPA-28", 1, 0.8m), L("ETQ-COLA-3000", 1, 1m), L("CO2", 0.016m, 3m),
                    L("CONC-COLA", 0.015m, 0.5m), L("AZUCAR", 0.26m, 0.5m),
                    L("FILM-TERMO", 0.06m, 2m, NivelReceta.PorPaquete), L("FILM-STRETCH", 0.35m, 1m, NivelReceta.PorPallet)
                })
            };
            await EnScopeAsync(async sp =>
            {
                var recServ = sp.GetRequiredService<IRecetaService>();
                foreach (var (producto, lineas) in recetas)
                {
                    await recServ.GuardarAsync(_productos[producto].Id, new RecetaRequest
                    {
                        VigenteDesde = Inicio, Comentario = "Receta estándar de lanzamiento", Detalles = lineas
                    });
                }
                // Versión 2 de Cola 500 ml: reformulación con menos azúcar y CO₂ (muestra el historial de versiones).
                await recServ.GuardarAsync(_productos["COLA-500-12"].Id, new RecetaRequest
                {
                    VigenteDesde = _hoy.AddDays(-45), Comentario = "Reformulación: 7 % menos azúcar y ajuste de carbonatación",
                    Detalles = Personal("COLA", 0.042m, 0.0040m)
                });
            }, A(Inicio.AddDays(-10), 11));
            logger.LogInformation("Demostración: {Productos} productos con receta.", _productos.Count);
        }

        // ---------------------------------------------------------------- 5. historial de ventas (antes de la operación)
        private async Task HistorialVentasAsync()
        {
            var desde = Inicio;
            var total = 0;
            while (desde < InicioOperacion)
            {
                var hasta = desde.AddDays(45) < InicioOperacion ? desde.AddDays(45) : InicioOperacion;
                var lote = new List<Venta>();
                for (var d = desde; d < hasta; d = d.AddDays(1))
                {
                    foreach (var p in _productos.Values)
                    {
                        foreach (var (canal, paquetes) in _demanda.Ventas(p.Base, d))
                        {
                            lote.Add(new Venta
                            {
                                Fecha = d, ProductoId = p.Id, Canal = canal, Paquetes = paquetes, Origen = OrigenVenta.Importacion,
                                AfectaStock = false, CreadoEn = A(d, 19), CreadoPor = "importación histórica"
                            });
                        }
                    }
                }
                await EnScopeAsync(async sp =>
                {
                    var repo = sp.GetRequiredService<IVentaRepository>();
                    foreach (var v in lote) repo.Agregar(v);
                    await repo.GuardarAsync();
                });
                total += lote.Count;
                desde = hasta;
            }
            logger.LogInformation("Demostración: {Ventas} registros de historial de ventas ({Desde:dd/MM/yyyy} a {Hasta:dd/MM/yyyy}).", total, Inicio, InicioOperacion.AddDays(-1));
        }

        // ---------------------------------------------------------------- 6. stock inicial
        private async Task StockInicialAsync()
        {
            var dia = InicioOperacion;
            var consumo = await ConsumoEsperadoAsync(dia);
            await EnScopeAsync(async sp =>
            {
                var inv = sp.GetRequiredService<IInventarioService>();
                foreach (var i in _insumos.Values)
                {
                    var d = (double)consumo.GetValueOrDefault(i.Id, i.ConsumoDiario ?? 0);
                    var dias = i.Codigo == "ETQ-LIM-500" ? 200 : i.Entrega + 18 + _demanda.Entero(0, 8); // etiqueta de limón: sobrestock a propósito
                    var cantidad = Math.Max((decimal)Math.Round(d * dias, 0), i.Minimo);
                    await inv.RegistrarAsync(new MovimientoRequest
                    {
                        Tipo = TipoMovimiento.StockInicial, AlmacenId = _almIns, InsumoId = i.Id, Cantidad = cantidad,
                        Documento = "INV-APERTURA", Motivo = "Inventario de apertura"
                    });
                }
                var pt = sp.GetRequiredService<IProductoTerminadoService>();
                foreach (var p in _productos.Values)
                {
                    await pt.RegistrarAsync(new MovimientoProductoRequest
                    {
                        Tipo = TipoMovimientoProducto.StockInicial, AlmacenId = _almPt, ProductoId = p.Id,
                        Paquetes = (decimal)Math.Round(_demanda.Esperado(p.Base, dia) * 6), Documento = "INV-APERTURA"
                    });
                }
            }, A(dia, 6, 30));
        }

        /// <summary>Consumo diario esperado de cada insumo (unidad de consumo) según la demanda esperada de los próximos 14 días.</summary>
        private async Task<Dictionary<int, decimal>> ConsumoEsperadoAsync(DateOnly dia)
        {
            var coef = await CoeficientesAsync(dia);
            var resultado = new Dictionary<int, decimal>();
            foreach (var p in _productos.Values)
            {
                var paquetesDia = Enumerable.Range(0, 14).Average(k => _demanda.Esperado(p.Base, dia.AddDays(k)));
                foreach (var (insumoId, porPaquete) in coef[p.Id])
                    resultado[insumoId] = resultado.GetValueOrDefault(insumoId) + porPaquete * (decimal)paquetesDia;
            }
            return resultado;
        }

        /// <summary>Insumo por paquete (con merma) de cada producto según la receta vigente en el día.</summary>
        private async Task<Dictionary<int, List<(int InsumoId, decimal PorPaquete)>>> CoeficientesAsync(DateOnly dia) =>
            await EnScopeAsync(async sp =>
            {
                var rec = sp.GetRequiredService<IRecetaService>();
                var r = new Dictionary<int, List<(int, decimal)>>();
                foreach (var p in _productos.Values)
                {
                    var ex = Dato(await rec.ExplotarAsync(p.Id, new ExplosionRequest { Cantidad = 1, Unidad = UnidadExplosion.Paquetes, Fecha = dia }), "explosión");
                    r[p.Id] = ex.Insumos.Select(i => (i.InsumoId, i.ConsumoTotal)).ToList();
                }
                return r;
            });

        // ---------------------------------------------------------------- 7. operación día a día (90 días)
        private async Task OperacionAsync()
        {
            var turno = 0;
            for (var dia = InicioOperacion; dia < _hoy; dia = dia.AddDays(1))
            {
                var faltan = _hoy.DayNumber - dia.DayNumber; // días hasta hoy
                await RecepcionesAsync(dia);
                await ProduccionDelDiaAsync(dia, faltan, ref_turno: turno++);
                await VentasDelDiaAsync(dia, faltan);
                await ConsumosYMermasAsync(dia, faltan);
                await ComprasDelDiaAsync(dia, faltan);
                if (dia == InicioOperacion.AddDays(30) || dia == InicioOperacion.AddDays(60)) await ConteoAsync(dia);
                if (faltan % 15 == 0) logger.LogInformation("Demostración: operación simulada hasta el {Dia:dd/MM/yyyy}.", dia);
            }
        }

        private async Task RecepcionesAsync(DateOnly dia)
        {
            foreach (var p in _pedidos.Where(p => !p.Recibido && p.Llegada <= dia).ToList())
            {
                await EnScopeAsync(async sp =>
                {
                    var servicio = sp.GetRequiredService<IPedidoService>();
                    var pedido = Dato(await servicio.ObtenerAsync(p.Id), "pedido");
                    var lineas = pedido.Detalles.Where(d => d.Pendiente > 0).Select(d => new RecepcionDetalleRequest
                    {
                        PedidoDetalleId = d.Id,
                        // Algunas llegadas son parciales (60 %); el saldo llega dos días después.
                        Cantidad = p.Parcial ? Math.Max(1, Math.Floor(d.Pendiente * 0.6m)) : d.Pendiente
                    }).ToList();
                    if (lineas.Count == 0) { p.Recibido = true; return; }
                    await servicio.RecibirAsync(p.Id, new RecepcionRequest
                    {
                        AlmacenId = _almIns, Guia = $"T001-{_guia++:000000}", Detalles = lineas,
                        Observacion = p.Parcial ? "Llegada parcial: el proveedor completa en dos días" : null
                    });
                    if (p.Parcial) { p.Parcial = false; p.Llegada = dia.AddDays(2); }
                    else p.Recibido = true;
                }, A(dia, 6, 30), Almacen);
            }
        }

        private async Task ProduccionDelDiaAsync(DateOnly dia, int faltan, int ref_turno)
        {
            if (dia.DayOfWeek == DayOfWeek.Sunday) return; // la planta no produce los domingos
            var coef = await CoeficientesAsync(dia);
            var hora = 7;
            foreach (var p in _productos.Values)
            {
                if (p.Codigo == "COLA-3000-4" && faltan <= 5) continue;   // deja el 3 L con poco stock al final (alerta)
                var stock = await EnScopeAsync(sp => sp.GetRequiredService<IVentaRepository>().PaquetesAsync(p.Id, _almPt));
                var semana = Enumerable.Range(0, 7).Sum(k => _demanda.Esperado(p.Base, dia.AddDays(k)));
                if ((double)stock >= semana * 4 / 7) continue;               // cubre 4 días: no se produce
                var lote = Math.Ceiling((semana * 9 / 7 - (double)stock) / 100) * 100;

                // No producir más de lo que permiten los insumos.
                var stocks = await EnScopeAsync(sp => sp.GetRequiredService<IProduccionOperacionRepository>()
                    .StockInsumosAsync(coef[p.Id].Select(c => c.InsumoId), _almIns));
                var posible = coef[p.Id].Where(c => c.PorPaquete > 0).Min(c => (double)(stocks.GetValueOrDefault(c.InsumoId) / c.PorPaquete));
                lote = Math.Min(lote, Math.Floor(posible * 0.98 / 100) * 100);
                if (lote < 100) { logger.LogDebug("Demostración: sin insumos para {Producto} el {Dia}", p.Codigo, dia); continue; }

                var linea = _lineas[p.Linea];
                var velocidad = p.Linea == "L1" ? 24000.0 : 12000.0;
                var horas = Math.Max(0.5, Math.Round(lote * p.PorPaquete / (velocidad * _demanda.Uniforme(0.84, 0.93)), 1));
                var turnoSimulado = (Turno)(1 + (ref_turno + hora) % 2);
                await EnScopeAsync(async sp =>
                {
                    var prod = sp.GetRequiredService<IProduccionService>();
                    var plan = Dato(await prod.CrearPlanAsync(new PlanRequest
                    {
                        Fecha = dia, LineaId = linea, ProductoId = p.Id, Paquetes = (decimal)lote, Comentario = "Reposición de stock de producto terminado"
                    }), "programación");
                    await prod.RegistrarParteAsync(new ParteRequest
                    {
                        Fecha = dia, Turno = turnoSimulado, LineaId = linea, ProductoId = p.Id, Paquetes = (decimal)lote,
                        HorasOperacion = (decimal)Math.Min(24, horas), AlmacenInsumosId = _almIns, AlmacenProductoId = _almPt, PlanId = plan.Id
                    });
                }, A(dia, hora, 15), Planificacion);
                hora = Math.Min(hora + 2, 20);
            }

            // Ejemplo de parte anulado (registrado en la línea equivocada).
            if (dia == InicioOperacion.AddDays(40))
            {
                await Seguro("parte anulado", () => EnScopeAsync(async sp =>
                {
                    var prod = sp.GetRequiredService<IProduccionService>();
                    var p = _productos["NAR-500-12"];
                    var parte = Dato(await prod.RegistrarParteAsync(new ParteRequest
                    {
                        Fecha = dia, Turno = Turno.Noche, LineaId = _lineas["L1"], ProductoId = p.Id, Paquetes = 200,
                        HorasOperacion = 0.5m, AlmacenInsumosId = _almIns, AlmacenProductoId = _almPt, Observacion = "Registro de prueba"
                    }), "parte");
                    await prod.AnularParteAsync(parte.Id, new MotivoRequest { Motivo = "Se registró dos veces el mismo lote" });
                }, A(dia, 21)));
            }
        }

        private async Task VentasDelDiaAsync(DateOnly dia, int faltan)
        {
            var minuto = 0;
            foreach (var p in _productos.Values)
            {
                var ventas = _demanda.Ventas(p.Base, dia);
                await EnScopeAsync(async sp =>
                {
                    var servicio = sp.GetRequiredService<IVentaService>();
                    var repo = sp.GetRequiredService<IVentaRepository>();
                    foreach (var (canal, paquetes) in ventas)
                    {
                        var disponible = await repo.PaquetesAsync(p.Id, _almPt);
                        var cantidad = Math.Min(paquetes, (int)Math.Floor(disponible));
                        if (cantidad <= 0) continue; // sin stock: venta perdida
                        using (Reloj.Simular(A(dia, 9 + minuto / 60 % 10, minuto % 60)))
                        {
                            await servicio.RegistrarAsync(new VentaRequest
                            {
                                Fecha = dia, ProductoId = p.Id, Canal = canal, Cantidad = cantidad, Documento = $"GR-{_documento++:000000}",
                                AfectaStock = true, AlmacenId = _almPt
                            });
                        }
                        minuto += 7;
                    }
                }, A(dia, 9), Almacen);
            }

            // Ejemplo de venta anulada.
            if (dia == InicioOperacion.AddDays(20))
            {
                await Seguro("venta anulada", () => EnScopeAsync(async sp =>
                {
                    var servicio = sp.GetRequiredService<IVentaService>();
                    var v = Dato(await servicio.RegistrarAsync(new VentaRequest
                    {
                        Fecha = dia, ProductoId = _productos["COLA-500-12"].Id, Canal = CanalVenta.Mayorista, Cantidad = 120,
                        Documento = $"GR-{_documento++:000000}", AfectaStock = true, AlmacenId = _almPt
                    }), "venta");
                    await servicio.AnularAsync(v.Id, new AnularVentaRequest { Motivo = "Pedido duplicado por el vendedor" });
                }, A(dia, 17)));
            }
        }

        private async Task ConsumosYMermasAsync(DateOnly dia, int faltan)
        {
            await EnScopeAsync(async sp =>
            {
                var inv = sp.GetRequiredService<IInventarioService>();
                var repo = sp.GetRequiredService<IProduccionOperacionRepository>();
                async Task<decimal> Stock(string codigo) =>
                    (await repo.StockInsumosAsync([_insumos[codigo].Id], _almIns)).GetValueOrDefault(_insumos[codigo].Id);

                async Task Salida(string codigo, TipoMovimiento tipo, decimal cantidad, string motivo)
                {
                    cantidad = Math.Min(Math.Round(cantidad, 2), await Stock(codigo));
                    if (cantidad <= 0) return;
                    await inv.RegistrarAsync(new MovimientoRequest
                    {
                        Tipo = tipo, AlmacenId = _almIns, InsumoId = _insumos[codigo].Id, Cantidad = cantidad, Motivo = motivo
                    });
                }

                // Insumos que no van en la receta: consumo semanal registrado a mano (cada viernes).
                if (dia.DayOfWeek == DayOfWeek.Friday)
                {
                    await Salida("LUBRICANTE", TipoMovimiento.SalidaConsumo, (decimal)Math.Round(_demanda.Uniforme(17, 24), 1), "Consumo semanal de transportadores");
                    await Salida("SODA", TipoMovimiento.SalidaConsumo, (decimal)Math.Round(_demanda.Uniforme(28, 40), 1), "Limpieza CIP semanal");
                }
                // Mermas ocasionales.
                if (faltan % 17 == 3) await Salida("PRE-500", TipoMovimiento.Merma, (decimal)Math.Round(_demanda.Uniforme(300, 900)), "Preformas deformadas en la alimentación de la sopladora");
                if (faltan % 23 == 5) await Salida("ETQ-COLA-500", TipoMovimiento.Merma, (decimal)Math.Round(_demanda.Uniforme(500, 1500)), "Bobina dañada por humedad");

                // Estado final para la demostración (ayer, después de producir y vender):
                if (faltan == 1)
                {
                    var consumo = await ConsumoEsperadoAsync(dia);
                    // Preformas 500 ml: lote defectuoso separado → quedan unos 3.5 días (se agotará antes del próximo pedido).
                    var pre = _insumos["PRE-500"];
                    var excedente = await Stock("PRE-500") - Math.Round(consumo[pre.Id] * 3.5m);
                    if (excedente > 0) await Salida("PRE-500", TipoMovimiento.Merma, excedente, "Lote con defecto de inyección: separado para devolución al proveedor");
                    // CO₂: fuga detectada → quedan unos 4 días (por debajo del punto de reorden, pero llega a tiempo si se pide hoy).
                    var co2 = _insumos["CO2"];
                    var exceso = await Stock("CO2") - Math.Round(consumo[co2.Id] * 4.2m);
                    if (exceso > 0) await Salida("CO2", TipoMovimiento.Merma, exceso, "Fuga en el tanque de CO₂ detectada en la inspección");
                }
            }, A(dia, 17, 30), Almacen);

            // Merma de producto terminado de ejemplo.
            if (faltan == 33)
            {
                await Seguro("merma de producto terminado", () => EnScopeAsync(sp => sp.GetRequiredService<IProductoTerminadoService>()
                    .RegistrarAsync(new MovimientoProductoRequest
                    {
                        Tipo = TipoMovimientoProducto.Merma, AlmacenId = _almPt, ProductoId = _productos["COLA-1500-6"].Id, Paquetes = 12,
                        Motivo = "Paquetes rotos por el montacargas"
                    }), A(dia, 18)));
            }

            // Programación vencida (sin parte) para la alerta.
            if (faltan == 2)
            {
                await Seguro("programación vencida", () => EnScopeAsync(sp => sp.GetRequiredService<IProduccionService>().CrearPlanAsync(new PlanRequest
                {
                    Fecha = dia, LineaId = _lineas["L1"], ProductoId = _productos["LIM-500-12"].Id, Paquetes = 300,
                    Comentario = "Lote promocional para supermercados"
                }), A(dia, 7)));
            }
        }

        private async Task ComprasDelDiaAsync(DateOnly dia, int faltan)
        {
            var consumo = await ConsumoEsperadoAsync(dia);
            var estado = await EnScopeAsync(async sp =>
            {
                var repo = sp.GetRequiredService<IProduccionOperacionRepository>();
                var stocks = await repo.StockInsumosAsync(_insumos.Values.Select(i => i.Id), _almIns);
                var enCamino = await sp.GetRequiredService<IPedidoRepository>().EnCaminoPorInsumoAsync();
                return (stocks, enCamino);
            });

            // Insumos a pedir hoy, agrupados por proveedor.
            var porProveedor = new Dictionary<int, List<(InsumoDemo Insumo, decimal Cantidad)>>();
            foreach (var i in _insumos.Values)
            {
                if (i.Codigo == "PRE-500" && faltan <= 14) continue;   // escenario final: preformas sin pedido en camino
                if (i.Codigo == "CO2" && faltan <= 10) continue;
                if (i.Codigo == "TAPA-28" && faltan <= 12) continue;   // solo el pedido atrasado de abajo
                var d = consumo.GetValueOrDefault(i.Id, i.ConsumoDiario ?? 0);
                if (d <= 0) continue;
                var posicion = estado.stocks.GetValueOrDefault(i.Id) + estado.enCamino.GetValueOrDefault(i.Id);
                if (posicion > d * (i.Entrega + i.Variacion + 4)) continue;
                var objetivo = d * (i.Entrega + 24) - posicion;
                var enCompra = Math.Ceiling(objetivo / i.Factor / i.Multiplo) * i.Multiplo;
                enCompra = Math.Max(enCompra, i.PedidoMinimo);
                if (!porProveedor.TryGetValue(i.ProveedorId, out var l)) porProveedor[i.ProveedorId] = l = [];
                l.Add((i, enCompra));
            }

            // Pedido de tapas atrasado (escenario final).
            if (faltan == 9)
            {
                var tapa = _insumos["TAPA-28"];
                porProveedor[tapa.ProveedorId] = [(tapa, 500m)];
            }

            foreach (var (proveedorId, lineas) in porProveedor)
            {
                var principal = lineas[0].Insumo;
                await Seguro("pedido", () => EnScopeAsync(async sp =>
                {
                    var pedido = Dato(await sp.GetRequiredService<IPedidoService>().CrearAsync(new PedidoRequest
                    {
                        ProveedorId = proveedorId, FechaPedido = dia, Moneda = principal.Moneda,
                        DocumentoProveedor = $"COT-{dia:yyMMdd}-{proveedorId}",
                        Detalles = lineas.Select(x => new PedidoDetalleRequest { InsumoId = x.Insumo.Id, Cantidad = x.Cantidad, PrecioUnitario = x.Insumo.Precio }).ToList()
                    }), "pedido");
                    var entrega = lineas.Max(x => x.Insumo.Entrega);
                    var variacion = lineas.Max(x => x.Insumo.Variacion);
                    var atraso = _demanda.Probabilidad(0.12) ? variacion + 2 : _demanda.Entero(-1, variacion);
                    var simulado = new PedidoSimulado
                    {
                        Id = pedido.Id,
                        Llegada = dia.AddDays(Math.Max(1, entrega + atraso)),
                        Parcial = _demanda.Probabilidad(0.10)
                    };
                    if (faltan == 9 && principal.Codigo == "TAPA-28") simulado.Recibido = true; // nunca llega: queda atrasado
                    _pedidos.Add(simulado);
                }, A(dia, 16), Compras));
            }
        }

        private async Task ConteoAsync(DateOnly dia)
        {
            await Seguro("conteo físico", () => EnScopeAsync(async sp =>
            {
                var servicio = sp.GetRequiredService<IConteoService>();
                var conteo = Dato(await servicio.CrearAsync(new ConteoCrearRequest
                {
                    AlmacenId = _almIns, SoloConStock = true, Observacion = "Conteo mensual de insumos"
                }), "conteo");
                // Diferencias pequeñas (≤ 0.6 %), como en un conteo real.
                var cantidades = conteo.Detalles.Select(d => new ConteoCantidadRequest
                {
                    InsumoId = d.InsumoId,
                    CantidadContada = _demanda.Probabilidad(0.35)
                        ? Math.Max(0, Math.Round(d.StockSistema * (decimal)(1 + _demanda.Uniforme(-0.006, 0.003)), 0))
                        : d.StockSistema
                }).ToList();
                await servicio.AprobarAsync(conteo.Id, new ConteoGuardarRequest { Cantidades = cantidades });
            }, A(dia, 18, 30), Almacen));
        }

        // ---------------------------------------------------------------- 8. programación futura
        private async Task ProgramacionFuturaAsync()
        {
            await EnScopeAsync(async sp =>
            {
                var prod = sp.GetRequiredService<IProduccionService>();
                for (var k = 0; k < 14; k++)
                {
                    var dia = _hoy.AddDays(k);
                    if (dia.DayOfWeek == DayOfWeek.Sunday) continue;
                    foreach (var p in _productos.Values)
                    {
                        // Cada producto cada 2 o 3 días, con lotes de ~2.5 días de venta.
                        if ((dia.DayNumber + p.Id) % (p.Base >= 150 ? 2 : 3) != 0) continue;
                        var lote = Math.Ceiling(_demanda.Esperado(p.Base, dia) * 2.5 / 100) * 100;
                        await prod.CrearPlanAsync(new PlanRequest
                        {
                            Fecha = dia, LineaId = _lineas[p.Linea], ProductoId = p.Id, Paquetes = (decimal)lote, Comentario = "Plan semanal"
                        });
                    }
                }
            }, usuario: Planificacion);
        }

        // ---------------------------------------------------------------- 9. pronóstico y alertas
        private async Task EntrenarYAlertarAsync()
        {
            await EnScopeAsync(async sp =>
            {
                var r = await sp.GetRequiredService<IPrediccionService>().EntrenarAsync(OrigenEjecucion.Importacion);
                logger.LogInformation("Demostración: {Mensaje}", r.Message);
                await sp.GetRequiredService<IAlertaService>().SincronizarAsync(forzar: true);
            });
        }

        /// <summary>Paso opcional: si falla, se registra y la carga continúa.</summary>
        private async Task Seguro(string que, Func<Task> accion)
        {
            try { await accion(); }
            catch (GenericException ex)
            {
                var detalle = ex.Errores is { Count: > 0 } ? string.Join(" ", ex.Errores.Values.SelectMany(v => v)) : ex.Message;
                logger.LogWarning("Demostración: se omitió {Que}: {Detalle}", que, detalle);
            }
        }
    }
}

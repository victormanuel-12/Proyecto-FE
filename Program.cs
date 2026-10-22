using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SPIn.Common.Excepciones;
using SPIn.Common.Seguridad;
using SPIn.Data;
using SPIn.Models;
using SPIn.Repository;
using SPIn.Repository.RepositoryImpl;
using SPIn.Services;
using SPIn.Services.ServiceImpl;
using SPIn.Services.Importacion;
using SPIn.Services.Prediccion;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------------ Base de datos (SQLite)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'DefaultConnection'.");
CrearCarpetaDeBaseDeDatos(connectionString, builder.Environment.ContentRootPath);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TenantProvider>();
builder.Services.AddScoped<ITenantProvider>(sp => sp.GetRequiredService<TenantProvider>());
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));

// ------------------------------------------------------------------ Identidad y sesión
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<SpinIdentityErrorDescriber>()
    .AddClaimsPrincipalFactory<SpinClaimsPrincipalFactory>();

// Un usuario desactivado o con rol cambiado pierde la sesión en 1 minuto como máximo.
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "spin.sesion";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.LoginPath = "/cuenta/login";
    options.LogoutPath = "/cuenta/salir";
    options.AccessDeniedPath = "/cuenta/acceso-denegado";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    // Para /api/ se responde 401/403 en lugar de redirigir a la página de login.
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api")) context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        else context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api")) context.Response.StatusCode = StatusCodes.Status403Forbidden;
        else context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// Las claves que cifran la cookie se guardan junto a la base para que la sesión sobreviva a reinicios del contenedor.
var rutaClaves = builder.Configuration["App:RutaClavesProteccion"];
if (string.IsNullOrWhiteSpace(rutaClaves))
    rutaClaves = Path.Combine(builder.Environment.ContentRootPath, "datos", "keys");
Directory.CreateDirectory(rutaClaves);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(rutaClaves))
    .SetApplicationName("SPIn");

// Protección CSRF: los formularios usan el campo oculto y fetch envía la cabecera X-CSRF-TOKEN.
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "spin.af";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ------------------------------------------------------------------ MVC + API
builder.Services
    .AddControllersWithViews(options =>
    {
        options.Filters.Add<GlobalExceptionFilter>();
        options.Filters.Add<ValidationFilter>();
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    })
    .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true)
    .AddJsonOptions(options => options.AllowInputFormatterExceptionMessages = false);

// ------------------------------------------------------------------ Repositorios y servicios
builder.Services.AddScoped<IEmpresaRepository, EmpresaRepositoryImpl>();
builder.Services.AddScoped<IPlantaRepository, PlantaRepositoryImpl>();
builder.Services.AddScoped<IAlmacenRepository, AlmacenRepositoryImpl>();
builder.Services.AddScoped<ILineaRepository, LineaRepositoryImpl>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepositoryImpl>();
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepositoryImpl>();
builder.Services.AddScoped<IUnidadRepository, UnidadRepositoryImpl>();
builder.Services.AddScoped<IInsumoRepository, InsumoRepositoryImpl>();
builder.Services.AddScoped<IProveedorRepository, ProveedorRepositoryImpl>();
builder.Services.AddScoped<ICondicionCompraRepository, CondicionCompraRepositoryImpl>();
builder.Services.AddScoped<IProductoRepository, ProductoRepositoryImpl>();
builder.Services.AddScoped<IRecetaRepository, RecetaRepositoryImpl>();
builder.Services.AddScoped<IInventarioRepository, InventarioRepositoryImpl>();
builder.Services.AddScoped<IVentaRepository, VentaRepositoryImpl>();
builder.Services.AddScoped<IImportacionRepository, ImportacionRepositoryImpl>();
builder.Services.AddScoped<IProduccionOperacionRepository, ProduccionOperacionRepositoryImpl>();
builder.Services.AddScoped<IPedidoRepository, PedidoRepositoryImpl>();
builder.Services.AddScoped<IPrediccionRepository, PrediccionRepositoryImpl>();
builder.Services.AddScoped<IAlertaRepository, AlertaRepositoryImpl>();

builder.Services.AddScoped<IEmpresaService, EmpresaService>();
builder.Services.AddScoped<IPlantaService, PlantaService>();
builder.Services.AddScoped<IAlmacenService, AlmacenService>();
builder.Services.AddScoped<ILineaService, LineaService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IInicioService, InicioService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IUnidadService, UnidadService>();
builder.Services.AddScoped<IInsumoService, InsumoService>();
builder.Services.AddScoped<IProveedorService, ProveedorService>();
builder.Services.AddScoped<ICondicionCompraService, CondicionCompraService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IRecetaService, RecetaService>();
builder.Services.AddScoped<IInventarioService, InventarioService>();
builder.Services.AddScoped<IConteoService, ConteoService>();
builder.Services.AddScoped<IProductoTerminadoService, ProductoTerminadoService>();
builder.Services.AddScoped<IVentaService, VentaService>();
builder.Services.AddScoped<IProduccionService, ProduccionService>();
builder.Services.AddScoped<IPedidoService, PedidoService>();

// Entrega 8: motor predictivo (ML.NET) y su reentrenamiento programado en segundo plano.
builder.Services.AddScoped<IPrediccionService, PrediccionService>();
builder.Services.AddScoped<IReposicionService, ReposicionService>();
builder.Services.AddHostedService<ReentrenamientoProgramado>();

// Entrega 9: alertas y panel.
builder.Services.AddScoped<IAlertaService, AlertaService>();
builder.Services.AddScoped<IPanelService, PanelService>();
builder.Services.AddHostedService<AlertasProgramadas>();

// Entrega 5: importación desde Excel/CSV. El archivo revisado se guarda en memoria hasta confirmarlo.
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IImportacionService, ImportacionService>();
builder.Services.AddScoped<IImportador, ImportadorProveedores>();
builder.Services.AddScoped<IImportador, ImportadorInsumos>();
builder.Services.AddScoped<IImportador, ImportadorCondiciones>();
builder.Services.AddScoped<IImportador, ImportadorProductos>();
builder.Services.AddScoped<IImportador, ImportadorRecetas>();
builder.Services.AddScoped<IImportador, ImportadorStockInsumos>();
builder.Services.AddScoped<IImportador, ImportadorStockProducto>();
builder.Services.AddScoped<IImportador, ImportadorVentas>();

var app = builder.Build();

// ------------------------------------------------------------------ Migraciones y datos mínimos
await DbInitializer.InicializarAsync(app.Services);

// Entrega 10: datos de demostración (solo si Demo:Cargar = true y la empresa inicial está vacía).
if (app.Configuration.GetValue<bool>("Demo:Cargar"))
    await SPIn.Data.Demo.DatosDemostracion.CargarAsync(app.Services);

// ------------------------------------------------------------------ Canal HTTP
var redirigirHttps = app.Configuration.GetValue("App:RedirigirHttps", true);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    if (redirigirHttps) app.UseHsts();
}

// Páginas de error amigables solo para las vistas; la API responde JSON.
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    rama => rama.UseStatusCodePagesWithReExecute("/estado/{0}"));

if (redirigirHttps) app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    var cabeceras = context.Response.Headers;
    cabeceras["X-Content-Type-Options"] = "nosniff";
    cabeceras["X-Frame-Options"] = "DENY";
    cabeceras["Referrer-Policy"] = "strict-origin-when-cross-origin";
    cabeceras["Content-Security-Policy"] =
        "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self'; " +
        "font-src 'self'; connect-src 'self'; frame-ancestors 'none'; form-action 'self'; base-uri 'self'";
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// ------------------------------------------------------------------ Auxiliares
static void CrearCarpetaDeBaseDeDatos(string connectionString, string contentRoot)
{
    var archivo = new SqliteConnectionStringBuilder(connectionString).DataSource;
    if (string.IsNullOrWhiteSpace(archivo) || archivo == ":memory:") return;
    var ruta = Path.IsPathRooted(archivo) ? archivo : Path.Combine(contentRoot, archivo);
    var carpeta = Path.GetDirectoryName(ruta);
    if (!string.IsNullOrEmpty(carpeta)) Directory.CreateDirectory(carpeta);
}

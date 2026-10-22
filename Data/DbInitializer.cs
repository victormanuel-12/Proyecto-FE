using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SPIn.Common;
using SPIn.Common.Seguridad;
using SPIn.Models;

namespace SPIn.Data;

/// <summary>
/// Al iniciar: aplica las migraciones pendientes y crea los datos mínimos para entrar al sistema
/// (roles, catálogo de formatos, la empresa inicial y el superadministrador).
/// No crea datos de demostración: eso corresponde a la Entrega 10.
/// </summary>
public static class DbInitializer
{
    public static async Task InicializarAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("SPIn.Inicializacion");
        var config = sp.GetRequiredService<IConfiguration>();
        var db = sp.GetRequiredService<ApplicationDbContext>();

        if (!db.Database.GetMigrations().Any())
        {
            throw new InvalidOperationException(
                "El proyecto no tiene migraciones. Ejecute en PowerShell, desde la carpeta del proyecto: " +
                ".\\scripts\\migracion.ps1 -Nombre Entrega01  (ver README.md, paso 2).");
        }

        await db.Database.MigrateAsync();
        logger.LogInformation("Base de datos actualizada.");

        await CrearRolesAsync(sp.GetRequiredService<RoleManager<IdentityRole>>());
        await CrearFormatosAsync(db);
        var empresa = await CrearEmpresaInicialAsync(db, config, logger);
        await CrearSuperAdminAsync(sp.GetRequiredService<UserManager<ApplicationUser>>(), empresa, config, logger);
        await CrearCatalogosAsync(db);
    }

    /// <summary>Categorías y unidades sugeridas para cada empresa que aún no las tenga (Entrega 2).</summary>
    private static async Task CrearCatalogosAsync(ApplicationDbContext db)
    {
        var empresas = await db.Empresas.Select(e => e.Id).ToListAsync();
        foreach (var empresaId in empresas)
            await CatalogoInicial.AgregarSiFaltaAsync(db, empresaId);
        await db.SaveChangesAsync();
    }

    private static async Task CrearRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var rol in Roles.Todos)
        {
            if (!await roleManager.RoleExistsAsync(rol))
                await roleManager.CreateAsync(new IdentityRole(rol));
        }
    }

    private static async Task CrearFormatosAsync(ApplicationDbContext db)
    {
        if (await db.Formatos.AnyAsync()) return;

        db.Formatos.AddRange(
            new Formato { Nombre = "PET 250 ml", TipoEnvase = TipoEnvase.Pet, VolumenMl = 250 },
            new Formato { Nombre = "PET 400 ml", TipoEnvase = TipoEnvase.Pet, VolumenMl = 400 },
            new Formato { Nombre = "PET 500 ml", TipoEnvase = TipoEnvase.Pet, VolumenMl = 500 },
            new Formato { Nombre = "PET 625 ml", TipoEnvase = TipoEnvase.Pet, VolumenMl = 625 },
            new Formato { Nombre = "PET 1 L", TipoEnvase = TipoEnvase.Pet, VolumenMl = 1000 },
            new Formato { Nombre = "PET 1.5 L", TipoEnvase = TipoEnvase.Pet, VolumenMl = 1500 },
            new Formato { Nombre = "PET 2.25 L", TipoEnvase = TipoEnvase.Pet, VolumenMl = 2250 },
            new Formato { Nombre = "PET 3 L", TipoEnvase = TipoEnvase.Pet, VolumenMl = 3000 },
            new Formato { Nombre = "Vidrio 192 ml", TipoEnvase = TipoEnvase.VidrioRetornable, VolumenMl = 192 },
            new Formato { Nombre = "Vidrio 237 ml", TipoEnvase = TipoEnvase.VidrioRetornable, VolumenMl = 237 },
            new Formato { Nombre = "Vidrio 296 ml", TipoEnvase = TipoEnvase.VidrioRetornable, VolumenMl = 296 },
            new Formato { Nombre = "Vidrio 1 L", TipoEnvase = TipoEnvase.VidrioRetornable, VolumenMl = 1000 },
            new Formato { Nombre = "Lata 355 ml", TipoEnvase = TipoEnvase.Lata, VolumenMl = 355 });
        await db.SaveChangesAsync();
    }

    private static async Task<Empresa> CrearEmpresaInicialAsync(ApplicationDbContext db, IConfiguration config, ILogger logger)
    {
        var existente = await db.Empresas.OrderBy(e => e.Id).FirstOrDefaultAsync();
        if (existente is not null) return existente;

        var ruc = config["Seed:Empresa:Ruc"] ?? "20123456786";
        if (!Validaciones.RucValido(ruc))
            throw new InvalidOperationException($"El RUC configurado en Seed:Empresa:Ruc ({ruc}) no es válido.");

        var empresa = new Empresa
        {
            Ruc = ruc,
            RazonSocial = config["Seed:Empresa:RazonSocial"] ?? "Empresa inicial",
            NombreComercial = Validaciones.TextoOpcional(config["Seed:Empresa:NombreComercial"]),
            Activo = true
        };
        db.Empresas.Add(empresa);
        await db.SaveChangesAsync();
        logger.LogInformation("Empresa inicial creada: {RazonSocial}", empresa.RazonSocial);
        return empresa;
    }

    private static async Task CrearSuperAdminAsync(UserManager<ApplicationUser> userManager, Empresa empresa, IConfiguration config, ILogger logger)
    {
        if (await userManager.Users.AnyAsync()) return;

        var email = config["Seed:Admin:Email"]?.Trim().ToLowerInvariant();
        var clave = config["Seed:Admin:Clave"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(clave))
        {
            logger.LogWarning("No hay usuarios y no se configuró Seed:Admin:Email / Seed:Admin:Clave. No se podrá iniciar sesión.");
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            NombreCompleto = config["Seed:Admin:Nombre"] ?? "Administrador SPIn",
            EmpresaId = empresa.Id,
            Activo = true,
            CreadoEn = DateTime.UtcNow
        };

        var resultado = await userManager.CreateAsync(admin, clave);
        if (!resultado.Succeeded)
        {
            var errores = string.Join(" ", resultado.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"No se pudo crear el superadministrador: {errores}");
        }

        await userManager.AddToRoleAsync(admin, Roles.SuperAdmin);
        logger.LogInformation("Superadministrador creado: {Email}", email);
    }
}

namespace SPIn.Common.Seguridad;

/// <summary>Roles del sistema. Los valores se usan en [Authorize(Roles = ...)].</summary>
public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Administrador = "Administrador";
    public const string JefeAlmacen = "JefeAlmacen";
    public const string Planificador = "Planificador";
    public const string Compras = "Compras";
    public const string Gerencia = "Gerencia";

    /// <summary>Gestión de la estructura de la empresa (plantas, almacenes, líneas) y de usuarios.</summary>
    public const string Administracion = SuperAdmin + "," + Administrador;

    /// <summary>Mantenimiento del catálogo de insumos, categorías y unidades.</summary>
    public const string Catalogo = SuperAdmin + "," + Administrador + "," + JefeAlmacen + "," + Compras;

    /// <summary>Mantenimiento de proveedores y condiciones de compra.</summary>
    public const string Abastecimiento = SuperAdmin + "," + Administrador + "," + Compras;

    /// <summary>Mantenimiento de productos y recetas (y, desde la Entrega 6, la producción).</summary>
    public const string Produccion = SuperAdmin + "," + Administrador + "," + Planificador;

    public static bool PuedeProduccion(System.Security.Claims.ClaimsPrincipal u) =>
        u.IsInRole(SuperAdmin) || u.IsInRole(Administrador) || u.IsInRole(Planificador);

    /// <summary>Registro de movimientos de stock y conteos físicos.</summary>
    public const string Almacen = SuperAdmin + "," + Administrador + "," + JefeAlmacen;

    public static bool PuedeAlmacen(System.Security.Claims.ClaimsPrincipal u) =>
        u.IsInRole(SuperAdmin) || u.IsInRole(Administrador) || u.IsInRole(JefeAlmacen);

    /// <summary>Registro de ventas y despachos.</summary>
    public const string Ventas = SuperAdmin + "," + Administrador + "," + Planificador + "," + JefeAlmacen;

    public static bool PuedeVentas(System.Security.Claims.ClaimsPrincipal u) =>
        u.IsInRole(SuperAdmin) || u.IsInRole(Administrador) || u.IsInRole(Planificador) || u.IsInRole(JefeAlmacen);

    /// <summary>Registro de partes de producción (Entrega 6). La programación la hace el grupo Produccion.</summary>
    public const string PartesProduccion = SuperAdmin + "," + Administrador + "," + Planificador + "," + JefeAlmacen;

    public static bool PuedePartes(System.Security.Claims.ClaimsPrincipal u) =>
        u.IsInRole(SuperAdmin) || u.IsInRole(Administrador) || u.IsInRole(Planificador) || u.IsInRole(JefeAlmacen);

    /// <summary>Recepción de pedidos en almacén (Entrega 7).</summary>
    public const string Recepcion = SuperAdmin + "," + Administrador + "," + JefeAlmacen + "," + Compras;

    public static bool PuedeRecibir(System.Security.Claims.ClaimsPrincipal u) =>
        u.IsInRole(SuperAdmin) || u.IsInRole(Administrador) || u.IsInRole(JefeAlmacen) || u.IsInRole(Compras);

    /// <summary>Recalcular el pronóstico a mano (Entrega 8): planificación, compras y administración.</summary>
    public const string Pronostico = SuperAdmin + "," + Administrador + "," + Planificador + "," + Compras;

    public static bool PuedePronostico(System.Security.Claims.ClaimsPrincipal u) =>
        u.IsInRole(SuperAdmin) || u.IsInRole(Administrador) || u.IsInRole(Planificador) || u.IsInRole(Compras);

    /// <summary>Importación masiva de datos (carga inicial).</summary>
    public static bool PuedeImportar(System.Security.Claims.ClaimsPrincipal u) =>
        u.IsInRole(SuperAdmin) || u.IsInRole(Administrador);

    public static bool PuedeCatalogo(System.Security.Claims.ClaimsPrincipal u) =>
        u.IsInRole(SuperAdmin) || u.IsInRole(Administrador) || u.IsInRole(JefeAlmacen) || u.IsInRole(Compras);

    public static bool PuedeAbastecimiento(System.Security.Claims.ClaimsPrincipal u) =>
        u.IsInRole(SuperAdmin) || u.IsInRole(Administrador) || u.IsInRole(Compras);

    public static readonly string[] Todos =
        [SuperAdmin, Administrador, JefeAlmacen, Planificador, Compras, Gerencia];

    public static string Etiqueta(string? rol) => rol switch
    {
        SuperAdmin => "Superadministrador SPIn",
        Administrador => "Administrador",
        JefeAlmacen => "Jefe de almacén",
        Planificador => "Planificador de producción",
        Compras => "Compras / Abastecimiento",
        Gerencia => "Gerencia (consulta)",
        _ => rol ?? string.Empty
    };

    /// <summary>Roles que puede asignar quien administra usuarios.</summary>
    public static IEnumerable<string> Asignables(bool esSuperAdmin) =>
        esSuperAdmin ? Todos : Todos.Where(r => r != SuperAdmin);

    public static bool Existe(string? rol) => rol is not null && Todos.Contains(rol);
}

public static class ClaimsSpin
{
    public const string EmpresaId = "spin:empresa_id";
    public const string EmpresaNombre = "spin:empresa_nombre";
    public const string NombreCompleto = "spin:nombre";
}

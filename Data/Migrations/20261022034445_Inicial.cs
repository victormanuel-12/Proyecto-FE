using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPIn.Data.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alertas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Clave = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Severidad = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Detalle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Enlace = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    TextoEnlace = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    PrimeraVez = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UltimaVez = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DescartadaEn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DescartadaPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    MotivoDescarte = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ResueltaEn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alertas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CategoriasInsumo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ModificadoEn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModificadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasInsumo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Configuraciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    SemanasHorizonte = table.Column<int>(type: "INTEGER", nullable: false),
                    SemanasValidacion = table.Column<int>(type: "INTEGER", nullable: false),
                    SemanasMinimas = table.Column<int>(type: "INTEGER", nullable: false),
                    Frecuencia = table.Column<int>(type: "INTEGER", nullable: false),
                    HoraReentrenamiento = table.Column<int>(type: "INTEGER", nullable: false),
                    ReentrenarPendiente = table.Column<bool>(type: "INTEGER", nullable: false),
                    DiasRevision = table.Column<int>(type: "INTEGER", nullable: false),
                    DiasEntregaPorDefecto = table.Column<int>(type: "INTEGER", nullable: false),
                    UmbralConteo = table.Column<double>(type: "REAL", nullable: false),
                    ModificadoEn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModificadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuraciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EjecucionesPronostico",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Origen = table.Column<int>(type: "INTEGER", nullable: false),
                    Usuario = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    DuracionMs = table.Column<int>(type: "INTEGER", nullable: false),
                    Exitosa = table.Column<bool>(type: "INTEGER", nullable: false),
                    Mensaje = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Productos = table.Column<int>(type: "INTEGER", nullable: false),
                    MapePromedio = table.Column<double>(type: "REAL", nullable: true),
                    UltimaSemanaHistorial = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    SemanasHorizonte = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EjecucionesPronostico", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ruc = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    RazonSocial = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    NombreComercial = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ModificadoEn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModificadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Formatos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    TipoEnvase = table.Column<int>(type: "INTEGER", nullable: false),
                    VolumenMl = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Formatos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Pais = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Ruc = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true),
                    RazonSocial = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    NombreComercial = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Contacto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ModificadoEn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModificadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnidadesMedida",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ModificadoEn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModificadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadesMedida", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    NombreCompleto = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Plantas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ModificadoEn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModificadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plantas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plantas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Sabor = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    FormatoId = table.Column<int>(type: "INTEGER", nullable: false),
                    UnidadesPorPaquete = table.Column<int>(type: "INTEGER", nullable: false),
                    PaquetesPorPallet = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ModificadoEn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModificadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Productos_Formatos_FormatoId",
                        column: x => x.FormatoId,
                        principalTable: "Formatos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PedidosCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Numero = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ProveedorId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaPedido = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    FechaEstimada = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Moneda = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentoProveedor = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    Observacion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    MotivoCancelacion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreadoPor = table.Column<string>(type: "TEXT", nullable: true),
                    ModificadoEn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModificadoPor = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidosCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidosCompra_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Insumos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    CategoriaId = table.Column<int>(type: "INTEGER", nullable: false),
                    UnidadCompraId = table.Column<int>(type: "INTEGER", nullable: false),
                    UnidadConsumoId = table.Column<int>(type: "INTEGER", nullable: false),
                    FactorConversion = table.Column<double>(type: "REAL", nullable: false),
                    StockMinimo = table.Column<double>(type: "REAL", nullable: false),
                    StockMaximo = table.Column<double>(type: "REAL", nullable: false),
                    CapacidadMaxima = table.Column<double>(type: "REAL", nullable: true),
                    EsCritico = table.Column<bool>(type: "INTEGER", nullable: false),
                    EsImportado = table.Column<bool>(type: "INTEGER", nullable: false),
                    NivelServicio = table.Column<double>(type: "REAL", nullable: false),
                    TipoConsumo = table.Column<int>(type: "INTEGER", nullable: false),
                    ConsumoEstimadoDiario = table.Column<double>(type: "REAL", nullable: true),
                    ControlaLote = table.Column<bool>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ModificadoEn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModificadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Insumos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Insumos_CategoriasInsumo_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "CategoriasInsumo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Insumos_UnidadesMedida_UnidadCompraId",
                        column: x => x.UnidadCompraId,
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Insumos_UnidadesMedida_UnidadConsumoId",
                        column: x => x.UnidadConsumoId,
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Almacenes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlantaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ModificadoEn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModificadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Almacenes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Almacenes_Plantas_PlantaId",
                        column: x => x.PlantaId,
                        principalTable: "Plantas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LineasProduccion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlantaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TipoEnvase = table.Column<int>(type: "INTEGER", nullable: false),
                    VelocidadNominal = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ModificadoEn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModificadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineasProduccion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineasProduccion_Plantas_PlantaId",
                        column: x => x.PlantaId,
                        principalTable: "Plantas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PronosticosProducto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EjecucionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Metodo = table.Column<int>(type: "INTEGER", nullable: false),
                    Mape = table.Column<double>(type: "REAL", nullable: true),
                    MapeSsa = table.Column<double>(type: "REAL", nullable: true),
                    MapeHolt = table.Column<double>(type: "REAL", nullable: true),
                    MapePromedio = table.Column<double>(type: "REAL", nullable: true),
                    MapeEstacional = table.Column<double>(type: "REAL", nullable: true),
                    SemanasHistorial = table.Column<int>(type: "INTEGER", nullable: false),
                    PromedioReciente = table.Column<double>(type: "REAL", nullable: false),
                    Nota = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PronosticosProducto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PronosticosProducto_EjecucionesPronostico_EjecucionId",
                        column: x => x.EjecucionId,
                        principalTable: "EjecucionesPronostico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PronosticosProducto_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recetas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    VigenteDesde = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    VigenteHasta = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Comentario = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ModificadoEn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModificadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recetas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recetas_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PedidoDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PedidoId = table.Column<int>(type: "INTEGER", nullable: false),
                    InsumoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cantidad = table.Column<double>(type: "REAL", nullable: false),
                    PrecioUnitario = table.Column<double>(type: "REAL", nullable: false),
                    CantidadRecibida = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoDetalles_Insumos_InsumoId",
                        column: x => x.InsumoId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidoDetalles_PedidosCompra_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "PedidosCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProveedorInsumos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProveedorId = table.Column<int>(type: "INTEGER", nullable: false),
                    InsumoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Moneda = table.Column<int>(type: "INTEGER", nullable: false),
                    PrecioUnitario = table.Column<double>(type: "REAL", nullable: false),
                    TiempoEntregaDias = table.Column<int>(type: "INTEGER", nullable: false),
                    VariacionEntregaDias = table.Column<int>(type: "INTEGER", nullable: false),
                    PedidoMinimo = table.Column<double>(type: "REAL", nullable: false),
                    MultiploCompra = table.Column<double>(type: "REAL", nullable: false),
                    EsPrincipal = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ModificadoEn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModificadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProveedorInsumos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProveedorInsumos_Insumos_InsumoId",
                        column: x => x.InsumoId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProveedorInsumos_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConteosFisicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    AlmacenId = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoriaId = table.Column<int>(type: "INTEGER", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    Observacion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CerradoEn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CerradoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConteosFisicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConteosFisicos_Almacenes_AlmacenId",
                        column: x => x.AlmacenId,
                        principalTable: "Almacenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConteosFisicos_CategoriasInsumo_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "CategoriasInsumo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosProducto",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductoId = table.Column<int>(type: "INTEGER", nullable: false),
                    AlmacenId = table.Column<int>(type: "INTEGER", nullable: false),
                    Paquetes = table.Column<double>(type: "REAL", nullable: false),
                    SaldoAlmacen = table.Column<double>(type: "REAL", nullable: false),
                    Documento = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Referencia = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    VentaId = table.Column<long>(type: "INTEGER", nullable: true),
                    ParteProduccionId = table.Column<int>(type: "INTEGER", nullable: true),
                    RevierteId = table.Column<long>(type: "INTEGER", nullable: true),
                    Revertido = table.Column<bool>(type: "INTEGER", nullable: false),
                    Usuario = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosProducto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosProducto_Almacenes_AlmacenId",
                        column: x => x.AlmacenId,
                        principalTable: "Almacenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosProducto_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosStock",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    InsumoId = table.Column<int>(type: "INTEGER", nullable: false),
                    AlmacenId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cantidad = table.Column<double>(type: "REAL", nullable: false),
                    SaldoAlmacen = table.Column<double>(type: "REAL", nullable: false),
                    Documento = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Referencia = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    ConteoId = table.Column<int>(type: "INTEGER", nullable: true),
                    ParteProduccionId = table.Column<int>(type: "INTEGER", nullable: true),
                    RecepcionId = table.Column<int>(type: "INTEGER", nullable: true),
                    RevierteId = table.Column<long>(type: "INTEGER", nullable: true),
                    Revertido = table.Column<bool>(type: "INTEGER", nullable: false),
                    Usuario = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosStock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_Almacenes_AlmacenId",
                        column: x => x.AlmacenId,
                        principalTable: "Almacenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_Insumos_InsumoId",
                        column: x => x.InsumoId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Recepciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    PedidoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AlmacenId = table.Column<int>(type: "INTEGER", nullable: false),
                    Guia = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Observacion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Anulada = table.Column<bool>(type: "INTEGER", nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Usuario = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recepciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recepciones_Almacenes_AlmacenId",
                        column: x => x.AlmacenId,
                        principalTable: "Almacenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recepciones_PedidosCompra_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "PedidosCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    InsumoId = table.Column<int>(type: "INTEGER", nullable: false),
                    AlmacenId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cantidad = table.Column<double>(type: "REAL", nullable: false),
                    UltimoMovimiento = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stocks_Almacenes_AlmacenId",
                        column: x => x.AlmacenId,
                        principalTable: "Almacenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Stocks_Insumos_InsumoId",
                        column: x => x.InsumoId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StocksProducto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductoId = table.Column<int>(type: "INTEGER", nullable: false),
                    AlmacenId = table.Column<int>(type: "INTEGER", nullable: false),
                    Paquetes = table.Column<double>(type: "REAL", nullable: false),
                    UltimoMovimiento = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StocksProducto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StocksProducto_Almacenes_AlmacenId",
                        column: x => x.AlmacenId,
                        principalTable: "Almacenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StocksProducto_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ventas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ProductoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Canal = table.Column<int>(type: "INTEGER", nullable: false),
                    Paquetes = table.Column<double>(type: "REAL", nullable: false),
                    Documento = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    Origen = table.Column<int>(type: "INTEGER", nullable: false),
                    AfectaStock = table.Column<bool>(type: "INTEGER", nullable: false),
                    AlmacenId = table.Column<int>(type: "INTEGER", nullable: true),
                    Anulada = table.Column<bool>(type: "INTEGER", nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ventas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ventas_Almacenes_AlmacenId",
                        column: x => x.AlmacenId,
                        principalTable: "Almacenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ventas_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LineaFormatos",
                columns: table => new
                {
                    LineaProduccionId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormatoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineaFormatos", x => new { x.LineaProduccionId, x.FormatoId });
                    table.ForeignKey(
                        name: "FK_LineaFormatos_Formatos_FormatoId",
                        column: x => x.FormatoId,
                        principalTable: "Formatos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LineaFormatos_LineasProduccion_LineaProduccionId",
                        column: x => x.LineaProduccionId,
                        principalTable: "LineasProduccion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartesProduccion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Turno = table.Column<int>(type: "INTEGER", nullable: false),
                    LineaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Paquetes = table.Column<double>(type: "REAL", nullable: false),
                    HorasOperacion = table.Column<double>(type: "REAL", nullable: true),
                    AlmacenInsumosId = table.Column<int>(type: "INTEGER", nullable: false),
                    AlmacenProductoId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecetaVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    PlanId = table.Column<int>(type: "INTEGER", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    Observacion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    MotivoAnulacion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreadoPor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartesProduccion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartesProduccion_Almacenes_AlmacenInsumosId",
                        column: x => x.AlmacenInsumosId,
                        principalTable: "Almacenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartesProduccion_Almacenes_AlmacenProductoId",
                        column: x => x.AlmacenProductoId,
                        principalTable: "Almacenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartesProduccion_LineasProduccion_LineaId",
                        column: x => x.LineaId,
                        principalTable: "LineasProduccion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartesProduccion_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanesProduccion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    LineaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Paquetes = table.Column<double>(type: "REAL", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    Comentario = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ParteId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreadoPor = table.Column<string>(type: "TEXT", nullable: true),
                    ModificadoEn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModificadoPor = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanesProduccion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanesProduccion_LineasProduccion_LineaId",
                        column: x => x.LineaId,
                        principalTable: "LineasProduccion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanesProduccion_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PronosticoSemanas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PronosticoProductoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Semana = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EsValidacion = table.Column<bool>(type: "INTEGER", nullable: false),
                    Paquetes = table.Column<double>(type: "REAL", nullable: false),
                    Minimo = table.Column<double>(type: "REAL", nullable: false),
                    Maximo = table.Column<double>(type: "REAL", nullable: false),
                    Real = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PronosticoSemanas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PronosticoSemanas_PronosticosProducto_PronosticoProductoId",
                        column: x => x.PronosticoProductoId,
                        principalTable: "PronosticosProducto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecetaDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RecetaId = table.Column<int>(type: "INTEGER", nullable: false),
                    InsumoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cantidad = table.Column<double>(type: "REAL", nullable: false),
                    Nivel = table.Column<int>(type: "INTEGER", nullable: false),
                    MermaPorcentaje = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecetaDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecetaDetalles_Insumos_InsumoId",
                        column: x => x.InsumoId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecetaDetalles_Recetas_RecetaId",
                        column: x => x.RecetaId,
                        principalTable: "Recetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConteoDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConteoId = table.Column<int>(type: "INTEGER", nullable: false),
                    InsumoId = table.Column<int>(type: "INTEGER", nullable: false),
                    StockSistema = table.Column<double>(type: "REAL", nullable: false),
                    CantidadContada = table.Column<double>(type: "REAL", nullable: true),
                    StockAlAprobar = table.Column<double>(type: "REAL", nullable: true),
                    AjusteAplicado = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConteoDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConteoDetalles_ConteosFisicos_ConteoId",
                        column: x => x.ConteoId,
                        principalTable: "ConteosFisicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConteoDetalles_Insumos_InsumoId",
                        column: x => x.InsumoId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecepcionDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RecepcionId = table.Column<int>(type: "INTEGER", nullable: false),
                    PedidoDetalleId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cantidad = table.Column<double>(type: "REAL", nullable: false),
                    MovimientoId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecepcionDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecepcionDetalles_PedidoDetalles_PedidoDetalleId",
                        column: x => x.PedidoDetalleId,
                        principalTable: "PedidoDetalles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecepcionDetalles_Recepciones_RecepcionId",
                        column: x => x.RecepcionId,
                        principalTable: "Recepciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParteConsumos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParteId = table.Column<int>(type: "INTEGER", nullable: false),
                    InsumoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cantidad = table.Column<double>(type: "REAL", nullable: false),
                    MovimientoId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParteConsumos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParteConsumos_Insumos_InsumoId",
                        column: x => x.InsumoId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParteConsumos_PartesProduccion_ParteId",
                        column: x => x.ParteId,
                        principalTable: "PartesProduccion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_EmpresaId_Clave",
                table: "Alertas",
                columns: new[] { "EmpresaId", "Clave" });

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_EmpresaId_Estado",
                table: "Alertas",
                columns: new[] { "EmpresaId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Almacenes_EmpresaId_Codigo",
                table: "Almacenes",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Almacenes_PlantaId",
                table: "Almacenes",
                column: "PlantaId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_EmpresaId",
                table: "AspNetUsers",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasInsumo_EmpresaId_Nombre",
                table: "CategoriasInsumo",
                columns: new[] { "EmpresaId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Configuraciones_EmpresaId",
                table: "Configuraciones",
                column: "EmpresaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConteoDetalles_ConteoId_InsumoId",
                table: "ConteoDetalles",
                columns: new[] { "ConteoId", "InsumoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConteoDetalles_InsumoId",
                table: "ConteoDetalles",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConteosFisicos_AlmacenId",
                table: "ConteosFisicos",
                column: "AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_ConteosFisicos_CategoriaId",
                table: "ConteosFisicos",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_ConteosFisicos_EmpresaId_Numero",
                table: "ConteosFisicos",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EjecucionesPronostico_EmpresaId_Fecha",
                table: "EjecucionesPronostico",
                columns: new[] { "EmpresaId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_Ruc",
                table: "Empresas",
                column: "Ruc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Formatos_Nombre",
                table: "Formatos",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Insumos_CategoriaId",
                table: "Insumos",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Insumos_EmpresaId_Codigo",
                table: "Insumos",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Insumos_UnidadCompraId",
                table: "Insumos",
                column: "UnidadCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_Insumos_UnidadConsumoId",
                table: "Insumos",
                column: "UnidadConsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_LineaFormatos_FormatoId",
                table: "LineaFormatos",
                column: "FormatoId");

            migrationBuilder.CreateIndex(
                name: "IX_LineasProduccion_EmpresaId_Codigo",
                table: "LineasProduccion",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LineasProduccion_PlantaId",
                table: "LineasProduccion",
                column: "PlantaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosProducto_AlmacenId",
                table: "MovimientosProducto",
                column: "AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosProducto_ProductoId_Fecha",
                table: "MovimientosProducto",
                columns: new[] { "ProductoId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_AlmacenId_Fecha",
                table: "MovimientosStock",
                columns: new[] { "AlmacenId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_Fecha",
                table: "MovimientosStock",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_InsumoId_Fecha",
                table: "MovimientosStock",
                columns: new[] { "InsumoId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_ParteConsumos_InsumoId",
                table: "ParteConsumos",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_ParteConsumos_ParteId",
                table: "ParteConsumos",
                column: "ParteId");

            migrationBuilder.CreateIndex(
                name: "IX_PartesProduccion_AlmacenInsumosId",
                table: "PartesProduccion",
                column: "AlmacenInsumosId");

            migrationBuilder.CreateIndex(
                name: "IX_PartesProduccion_AlmacenProductoId",
                table: "PartesProduccion",
                column: "AlmacenProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_PartesProduccion_EmpresaId_Numero",
                table: "PartesProduccion",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartesProduccion_LineaId",
                table: "PartesProduccion",
                column: "LineaId");

            migrationBuilder.CreateIndex(
                name: "IX_PartesProduccion_ProductoId_Fecha",
                table: "PartesProduccion",
                columns: new[] { "ProductoId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_PedidoDetalles_InsumoId",
                table: "PedidoDetalles",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoDetalles_PedidoId_InsumoId",
                table: "PedidoDetalles",
                columns: new[] { "PedidoId", "InsumoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PedidosCompra_EmpresaId_Numero",
                table: "PedidosCompra",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PedidosCompra_Estado",
                table: "PedidosCompra",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosCompra_ProveedorId",
                table: "PedidosCompra",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanesProduccion_Fecha_LineaId",
                table: "PlanesProduccion",
                columns: new[] { "Fecha", "LineaId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanesProduccion_LineaId",
                table: "PlanesProduccion",
                column: "LineaId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanesProduccion_ProductoId",
                table: "PlanesProduccion",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Plantas_EmpresaId_Codigo",
                table: "Plantas",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Productos_EmpresaId_Codigo",
                table: "Productos",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Productos_FormatoId",
                table: "Productos",
                column: "FormatoId");

            migrationBuilder.CreateIndex(
                name: "IX_PronosticoSemanas_PronosticoProductoId",
                table: "PronosticoSemanas",
                column: "PronosticoProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_PronosticosProducto_EjecucionId_ProductoId",
                table: "PronosticosProducto",
                columns: new[] { "EjecucionId", "ProductoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PronosticosProducto_ProductoId",
                table: "PronosticosProducto",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_EmpresaId_Ruc",
                table: "Proveedores",
                columns: new[] { "EmpresaId", "Ruc" },
                unique: true,
                filter: "\"Ruc\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProveedorInsumos_InsumoId",
                table: "ProveedorInsumos",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProveedorInsumos_ProveedorId_InsumoId",
                table: "ProveedorInsumos",
                columns: new[] { "ProveedorId", "InsumoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecepcionDetalles_PedidoDetalleId",
                table: "RecepcionDetalles",
                column: "PedidoDetalleId");

            migrationBuilder.CreateIndex(
                name: "IX_RecepcionDetalles_RecepcionId",
                table: "RecepcionDetalles",
                column: "RecepcionId");

            migrationBuilder.CreateIndex(
                name: "IX_Recepciones_AlmacenId",
                table: "Recepciones",
                column: "AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_Recepciones_PedidoId",
                table: "Recepciones",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_RecetaDetalles_InsumoId",
                table: "RecetaDetalles",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_RecetaDetalles_RecetaId_InsumoId",
                table: "RecetaDetalles",
                columns: new[] { "RecetaId", "InsumoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recetas_ProductoId_Version",
                table: "Recetas",
                columns: new[] { "ProductoId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recetas_ProductoId_VigenteDesde",
                table: "Recetas",
                columns: new[] { "ProductoId", "VigenteDesde" });

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_AlmacenId_InsumoId",
                table: "Stocks",
                columns: new[] { "AlmacenId", "InsumoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_InsumoId",
                table: "Stocks",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_StocksProducto_AlmacenId_ProductoId",
                table: "StocksProducto",
                columns: new[] { "AlmacenId", "ProductoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StocksProducto_ProductoId",
                table: "StocksProducto",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesMedida_EmpresaId_Codigo",
                table: "UnidadesMedida",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_AlmacenId",
                table: "Ventas",
                column: "AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_Fecha",
                table: "Ventas",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_ProductoId_Fecha",
                table: "Ventas",
                columns: new[] { "ProductoId", "Fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alertas");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Configuraciones");

            migrationBuilder.DropTable(
                name: "ConteoDetalles");

            migrationBuilder.DropTable(
                name: "LineaFormatos");

            migrationBuilder.DropTable(
                name: "MovimientosProducto");

            migrationBuilder.DropTable(
                name: "MovimientosStock");

            migrationBuilder.DropTable(
                name: "ParteConsumos");

            migrationBuilder.DropTable(
                name: "PlanesProduccion");

            migrationBuilder.DropTable(
                name: "PronosticoSemanas");

            migrationBuilder.DropTable(
                name: "ProveedorInsumos");

            migrationBuilder.DropTable(
                name: "RecepcionDetalles");

            migrationBuilder.DropTable(
                name: "RecetaDetalles");

            migrationBuilder.DropTable(
                name: "Stocks");

            migrationBuilder.DropTable(
                name: "StocksProducto");

            migrationBuilder.DropTable(
                name: "Ventas");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "ConteosFisicos");

            migrationBuilder.DropTable(
                name: "PartesProduccion");

            migrationBuilder.DropTable(
                name: "PronosticosProducto");

            migrationBuilder.DropTable(
                name: "PedidoDetalles");

            migrationBuilder.DropTable(
                name: "Recepciones");

            migrationBuilder.DropTable(
                name: "Recetas");

            migrationBuilder.DropTable(
                name: "LineasProduccion");

            migrationBuilder.DropTable(
                name: "EjecucionesPronostico");

            migrationBuilder.DropTable(
                name: "Insumos");

            migrationBuilder.DropTable(
                name: "Almacenes");

            migrationBuilder.DropTable(
                name: "PedidosCompra");

            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropTable(
                name: "CategoriasInsumo");

            migrationBuilder.DropTable(
                name: "UnidadesMedida");

            migrationBuilder.DropTable(
                name: "Plantas");

            migrationBuilder.DropTable(
                name: "Proveedores");

            migrationBuilder.DropTable(
                name: "Formatos");

            migrationBuilder.DropTable(
                name: "Empresas");
        }
    }
}

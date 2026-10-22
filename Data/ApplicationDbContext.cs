using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SPIn.Common.Seguridad;
using SPIn.Models;

namespace SPIn.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ITenantProvider _tenant;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    /// <summary>Empresa del usuario actual. Lo usan los filtros globales de consulta.</summary>
    public int? EmpresaActualId => _tenant.EmpresaId;

    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Planta> Plantas => Set<Planta>();
    public DbSet<Almacen> Almacenes => Set<Almacen>();
    public DbSet<Formato> Formatos => Set<Formato>();
    public DbSet<LineaProduccion> Lineas => Set<LineaProduccion>();
    public DbSet<LineaFormato> LineaFormatos => Set<LineaFormato>();
    public DbSet<CategoriaInsumo> Categorias => Set<CategoriaInsumo>();
    public DbSet<UnidadMedida> Unidades => Set<UnidadMedida>();
    public DbSet<Insumo> Insumos => Set<Insumo>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<ProveedorInsumo> ProveedorInsumos => Set<ProveedorInsumo>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Receta> Recetas => Set<Receta>();
    public DbSet<RecetaDetalle> RecetaDetalles => Set<RecetaDetalle>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<MovimientoStock> Movimientos => Set<MovimientoStock>();
    public DbSet<ConteoFisico> Conteos => Set<ConteoFisico>();
    public DbSet<ConteoDetalle> ConteoDetalles => Set<ConteoDetalle>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<StockProducto> StocksProducto => Set<StockProducto>();
    public DbSet<MovimientoProducto> MovimientosProducto => Set<MovimientoProducto>();
    public DbSet<PlanProduccion> PlanesProduccion => Set<PlanProduccion>();
    public DbSet<ParteProduccion> PartesProduccion => Set<ParteProduccion>();
    public DbSet<ParteConsumo> ParteConsumos => Set<ParteConsumo>();
    public DbSet<PedidoCompra> Pedidos => Set<PedidoCompra>();
    public DbSet<PedidoDetalle> PedidoDetalles => Set<PedidoDetalle>();
    public DbSet<Recepcion> Recepciones => Set<Recepcion>();
    public DbSet<RecepcionDetalle> RecepcionDetalles => Set<RecepcionDetalle>();
    public DbSet<ConfiguracionEmpresa> Configuraciones => Set<ConfiguracionEmpresa>();
    public DbSet<EjecucionPronostico> EjecucionesPronostico => Set<EjecucionPronostico>();
    public DbSet<PronosticoProducto> PronosticosProducto => Set<PronosticoProducto>();
    public DbSet<PronosticoSemana> PronosticoSemanas => Set<PronosticoSemana>();
    public DbSet<Alerta> Alertas => Set<Alerta>();

    /// <summary>
    /// SQLite guarda decimal como texto y no puede compararlo ni ordenarlo en SQL.
    /// Se almacena como REAL (double): suficiente para cantidades y precios de inventario.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        // También aplica a las propiedades decimal? (EF desenvuelve el tipo anulable).
        configurationBuilder.Properties<decimal>().HaveConversion<double>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(u => u.NombreCompleto).HasMaxLength(120).IsRequired();
            b.HasOne(u => u.Empresa).WithMany().HasForeignKey(u => u.EmpresaId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(u => u.EmpresaId);
        });

        builder.Entity<Empresa>(b =>
        {
            b.ToTable("Empresas");
            b.Property(e => e.Ruc).HasMaxLength(11).IsRequired();
            b.Property(e => e.RazonSocial).HasMaxLength(150).IsRequired();
            b.Property(e => e.NombreComercial).HasMaxLength(100);
            b.Property(e => e.CreadoPor).HasMaxLength(256);
            b.Property(e => e.ModificadoPor).HasMaxLength(256);
            b.HasIndex(e => e.Ruc).IsUnique();
        });

        builder.Entity<Planta>(b =>
        {
            b.ToTable("Plantas");
            b.Property(p => p.Codigo).HasMaxLength(10).IsRequired();
            b.Property(p => p.Nombre).HasMaxLength(100).IsRequired();
            b.Property(p => p.Direccion).HasMaxLength(200);
            b.Property(p => p.CreadoPor).HasMaxLength(256);
            b.Property(p => p.ModificadoPor).HasMaxLength(256);
            b.HasOne(p => p.Empresa).WithMany().HasForeignKey(p => p.EmpresaId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(p => new { p.EmpresaId, p.Codigo }).IsUnique();
            b.HasQueryFilter(p => p.EmpresaId == EmpresaActualId);
        });

        builder.Entity<Almacen>(b =>
        {
            b.ToTable("Almacenes");
            b.Property(a => a.Codigo).HasMaxLength(10).IsRequired();
            b.Property(a => a.Nombre).HasMaxLength(100).IsRequired();
            b.Property(a => a.CreadoPor).HasMaxLength(256);
            b.Property(a => a.ModificadoPor).HasMaxLength(256);
            b.HasOne(a => a.Planta).WithMany(p => p.Almacenes).HasForeignKey(a => a.PlantaId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(a => new { a.EmpresaId, a.Codigo }).IsUnique();
            b.HasIndex(a => a.PlantaId);
            b.HasQueryFilter(a => a.EmpresaId == EmpresaActualId);
        });

        builder.Entity<Formato>(b =>
        {
            b.ToTable("Formatos");
            b.Property(f => f.Nombre).HasMaxLength(40).IsRequired();
            b.HasIndex(f => f.Nombre).IsUnique();
        });

        builder.Entity<LineaProduccion>(b =>
        {
            b.ToTable("LineasProduccion");
            b.Property(l => l.Codigo).HasMaxLength(10).IsRequired();
            b.Property(l => l.Nombre).HasMaxLength(100).IsRequired();
            b.Property(l => l.CreadoPor).HasMaxLength(256);
            b.Property(l => l.ModificadoPor).HasMaxLength(256);
            b.HasOne(l => l.Planta).WithMany(p => p.Lineas).HasForeignKey(l => l.PlantaId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(l => new { l.EmpresaId, l.Codigo }).IsUnique();
            b.HasIndex(l => l.PlantaId);
            b.HasQueryFilter(l => l.EmpresaId == EmpresaActualId);
        });

        builder.Entity<LineaFormato>(b =>
        {
            b.ToTable("LineaFormatos");
            b.HasKey(lf => new { lf.LineaProduccionId, lf.FormatoId });
            b.HasOne(lf => lf.LineaProduccion).WithMany(l => l.Formatos)
                .HasForeignKey(lf => lf.LineaProduccionId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(lf => lf.Formato).WithMany()
                .HasForeignKey(lf => lf.FormatoId).OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(lf => lf.LineaProduccion!.EmpresaId == EmpresaActualId);
        });

        ConfigurarCatalogo(builder);
        ConfigurarProduccion(builder);
        ConfigurarInventario(builder);
        ConfigurarVentas(builder);
        ConfigurarOperacion(builder);
        ConfigurarPrediccion(builder);
        builder.Entity<Alerta>(b =>
        {
            b.ToTable("Alertas");
            b.Property(x => x.Clave).HasMaxLength(80).IsRequired();
            b.Property(x => x.Titulo).HasMaxLength(200).IsRequired();
            b.Property(x => x.Detalle).HasMaxLength(500).IsRequired();
            b.Property(x => x.Enlace).HasMaxLength(200);
            b.Property(x => x.TextoEnlace).HasMaxLength(60);
            b.Property(x => x.DescartadaPor).HasMaxLength(256);
            b.Property(x => x.MotivoDescarte).HasMaxLength(200);
            b.HasIndex(x => new { x.EmpresaId, x.Clave });
            b.HasIndex(x => new { x.EmpresaId, x.Estado });
            b.HasQueryFilter(x => x.EmpresaId == EmpresaActualId);
        });
    }

    private void ConfigurarCatalogo(ModelBuilder builder)
    {
        builder.Entity<CategoriaInsumo>(b =>
        {
            b.ToTable("CategoriasInsumo");
            b.Property(c => c.Nombre).HasMaxLength(60).IsRequired();
            b.Property(c => c.Descripcion).HasMaxLength(200);
            b.Property(c => c.CreadoPor).HasMaxLength(256);
            b.Property(c => c.ModificadoPor).HasMaxLength(256);
            b.HasIndex(c => new { c.EmpresaId, c.Nombre }).IsUnique();
            b.HasQueryFilter(c => c.EmpresaId == EmpresaActualId);
        });

        builder.Entity<UnidadMedida>(b =>
        {
            b.ToTable("UnidadesMedida");
            b.Property(u => u.Codigo).HasMaxLength(10).IsRequired();
            b.Property(u => u.Nombre).HasMaxLength(40).IsRequired();
            b.Property(u => u.CreadoPor).HasMaxLength(256);
            b.Property(u => u.ModificadoPor).HasMaxLength(256);
            b.HasIndex(u => new { u.EmpresaId, u.Codigo }).IsUnique();
            b.HasQueryFilter(u => u.EmpresaId == EmpresaActualId);
        });

        builder.Entity<Insumo>(b =>
        {
            b.ToTable("Insumos");
            b.Property(i => i.Codigo).HasMaxLength(20).IsRequired();
            b.Property(i => i.Nombre).HasMaxLength(120).IsRequired();
            b.Property(i => i.CreadoPor).HasMaxLength(256);
            b.Property(i => i.ModificadoPor).HasMaxLength(256);
            b.HasOne(i => i.Categoria).WithMany().HasForeignKey(i => i.CategoriaId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(i => i.UnidadCompra).WithMany().HasForeignKey(i => i.UnidadCompraId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(i => i.UnidadConsumo).WithMany().HasForeignKey(i => i.UnidadConsumoId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(i => new { i.EmpresaId, i.Codigo }).IsUnique();
            b.HasIndex(i => i.CategoriaId);
            b.HasQueryFilter(i => i.EmpresaId == EmpresaActualId);
        });

        builder.Entity<Proveedor>(b =>
        {
            b.ToTable("Proveedores");
            b.Ignore(p => p.EsExtranjero);
            b.Property(p => p.Pais).HasMaxLength(60).IsRequired();
            b.Property(p => p.Ruc).HasMaxLength(11);
            b.Property(p => p.RazonSocial).HasMaxLength(150).IsRequired();
            b.Property(p => p.NombreComercial).HasMaxLength(100);
            b.Property(p => p.Contacto).HasMaxLength(100);
            b.Property(p => p.Telefono).HasMaxLength(30);
            b.Property(p => p.Email).HasMaxLength(256);
            b.Property(p => p.CreadoPor).HasMaxLength(256);
            b.Property(p => p.ModificadoPor).HasMaxLength(256);
            b.HasIndex(p => new { p.EmpresaId, p.Ruc }).IsUnique().HasFilter("\"Ruc\" IS NOT NULL");
            b.HasQueryFilter(p => p.EmpresaId == EmpresaActualId);
        });

        builder.Entity<ProveedorInsumo>(b =>
        {
            b.ToTable("ProveedorInsumos");
            b.Property(c => c.CreadoPor).HasMaxLength(256);
            b.Property(c => c.ModificadoPor).HasMaxLength(256);
            b.HasOne(c => c.Proveedor).WithMany(p => p.Insumos).HasForeignKey(c => c.ProveedorId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(c => c.Insumo).WithMany(i => i.Proveedores).HasForeignKey(c => c.InsumoId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(c => new { c.ProveedorId, c.InsumoId }).IsUnique();
            b.HasIndex(c => c.InsumoId);
            b.HasQueryFilter(c => c.EmpresaId == EmpresaActualId);
        });
    }

    private void ConfigurarProduccion(ModelBuilder builder)
    {
        builder.Entity<Producto>(b =>
        {
            b.ToTable("Productos");
            b.Property(p => p.Codigo).HasMaxLength(20).IsRequired();
            b.Property(p => p.Nombre).HasMaxLength(120).IsRequired();
            b.Property(p => p.Sabor).HasMaxLength(40);
            b.Property(p => p.CreadoPor).HasMaxLength(256);
            b.Property(p => p.ModificadoPor).HasMaxLength(256);
            b.HasOne(p => p.Formato).WithMany().HasForeignKey(p => p.FormatoId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(p => new { p.EmpresaId, p.Codigo }).IsUnique();
            b.HasQueryFilter(p => p.EmpresaId == EmpresaActualId);
        });

        builder.Entity<Receta>(b =>
        {
            b.ToTable("Recetas");
            b.Property(r => r.Comentario).HasMaxLength(200);
            b.Property(r => r.CreadoPor).HasMaxLength(256);
            b.Property(r => r.ModificadoPor).HasMaxLength(256);
            b.HasOne(r => r.Producto).WithMany(p => p.Recetas).HasForeignKey(r => r.ProductoId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(r => new { r.ProductoId, r.Version }).IsUnique();
            b.HasIndex(r => new { r.ProductoId, r.VigenteDesde });
            b.HasQueryFilter(r => r.EmpresaId == EmpresaActualId);
        });

        builder.Entity<RecetaDetalle>(b =>
        {
            b.ToTable("RecetaDetalles");
            b.HasOne(d => d.Receta).WithMany(r => r.Detalles).HasForeignKey(d => d.RecetaId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(d => d.Insumo).WithMany().HasForeignKey(d => d.InsumoId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(d => new { d.RecetaId, d.InsumoId }).IsUnique();
            b.HasIndex(d => d.InsumoId);
            b.HasQueryFilter(d => d.Receta!.EmpresaId == EmpresaActualId);
        });
    }

    private void ConfigurarInventario(ModelBuilder builder)
    {
        builder.Entity<Stock>(b =>
        {
            b.ToTable("Stocks");
            b.HasOne(x => x.Insumo).WithMany().HasForeignKey(x => x.InsumoId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Almacen).WithMany().HasForeignKey(x => x.AlmacenId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.AlmacenId, x.InsumoId }).IsUnique();
            b.HasIndex(x => x.InsumoId);
            b.HasQueryFilter(x => x.EmpresaId == EmpresaActualId);
        });

        builder.Entity<MovimientoStock>(b =>
        {
            b.ToTable("MovimientosStock");
            b.Property(m => m.Documento).HasMaxLength(60);
            b.Property(m => m.Motivo).HasMaxLength(200);
            b.Property(m => m.Referencia).HasMaxLength(120);
            b.Property(m => m.Usuario).HasMaxLength(256).IsRequired();
            b.HasOne(m => m.Insumo).WithMany().HasForeignKey(m => m.InsumoId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(m => m.Almacen).WithMany().HasForeignKey(m => m.AlmacenId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(m => new { m.InsumoId, m.Fecha });
            b.HasIndex(m => new { m.AlmacenId, m.Fecha });
            b.HasIndex(m => m.Fecha);
            b.HasQueryFilter(m => m.EmpresaId == EmpresaActualId);
        });

        builder.Entity<ConteoFisico>(b =>
        {
            b.ToTable("ConteosFisicos");
            b.Property(c => c.Observacion).HasMaxLength(200);
            b.Property(c => c.CreadoPor).HasMaxLength(256).IsRequired();
            b.Property(c => c.CerradoPor).HasMaxLength(256);
            b.HasOne(c => c.Almacen).WithMany().HasForeignKey(c => c.AlmacenId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(c => c.Categoria).WithMany().HasForeignKey(c => c.CategoriaId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(c => new { c.EmpresaId, c.Numero }).IsUnique();
            b.HasQueryFilter(c => c.EmpresaId == EmpresaActualId);
        });

        builder.Entity<ConteoDetalle>(b =>
        {
            b.ToTable("ConteoDetalles");
            b.HasOne(d => d.Conteo).WithMany(c => c.Detalles).HasForeignKey(d => d.ConteoId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(d => d.Insumo).WithMany().HasForeignKey(d => d.InsumoId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(d => new { d.ConteoId, d.InsumoId }).IsUnique();
            b.HasQueryFilter(d => d.Conteo!.EmpresaId == EmpresaActualId);
        });
    }

    private void ConfigurarPrediccion(ModelBuilder builder)
    {
        builder.Entity<ConfiguracionEmpresa>(b =>
        {
            b.ToTable("Configuraciones");
            b.Property(x => x.ModificadoPor).HasMaxLength(256);
            b.HasIndex(x => x.EmpresaId).IsUnique();
            b.HasQueryFilter(x => x.EmpresaId == EmpresaActualId);
        });

        builder.Entity<EjecucionPronostico>(b =>
        {
            b.ToTable("EjecucionesPronostico");
            b.Property(x => x.Usuario).HasMaxLength(256).IsRequired();
            b.Property(x => x.Mensaje).HasMaxLength(1000);
            b.HasIndex(x => new { x.EmpresaId, x.Fecha });
            b.HasQueryFilter(x => x.EmpresaId == EmpresaActualId);
        });

        builder.Entity<PronosticoProducto>(b =>
        {
            b.ToTable("PronosticosProducto");
            b.Property(x => x.Nota).HasMaxLength(300);
            b.HasOne(x => x.Ejecucion).WithMany(e => e.Resultados).HasForeignKey(x => x.EjecucionId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.EjecucionId, x.ProductoId }).IsUnique();
            b.HasQueryFilter(x => x.Ejecucion!.EmpresaId == EmpresaActualId);
        });

        builder.Entity<PronosticoSemana>(b =>
        {
            b.ToTable("PronosticoSemanas");
            b.HasOne(x => x.PronosticoProducto).WithMany(p => p.Semanas).HasForeignKey(x => x.PronosticoProductoId).OnDelete(DeleteBehavior.Cascade);
            b.HasQueryFilter(x => x.PronosticoProducto!.Ejecucion!.EmpresaId == EmpresaActualId);
        });
    }

    private void ConfigurarOperacion(ModelBuilder builder)
    {
        builder.Entity<PlanProduccion>(b =>
        {
            b.ToTable("PlanesProduccion");
            b.Property(x => x.Comentario).HasMaxLength(200);
            b.HasOne(x => x.Linea).WithMany().HasForeignKey(x => x.LineaId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.Fecha, x.LineaId });
            b.HasQueryFilter(x => x.EmpresaId == EmpresaActualId);
        });

        builder.Entity<ParteProduccion>(b =>
        {
            b.ToTable("PartesProduccion");
            b.Property(x => x.Observacion).HasMaxLength(200);
            b.Property(x => x.MotivoAnulacion).HasMaxLength(200);
            b.Property(x => x.CreadoPor).HasMaxLength(256).IsRequired();
            b.HasOne(x => x.Linea).WithMany().HasForeignKey(x => x.LineaId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.AlmacenInsumos).WithMany().HasForeignKey(x => x.AlmacenInsumosId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.AlmacenProducto).WithMany().HasForeignKey(x => x.AlmacenProductoId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique();
            b.HasIndex(x => new { x.ProductoId, x.Fecha });
            b.HasQueryFilter(x => x.EmpresaId == EmpresaActualId);
        });

        builder.Entity<ParteConsumo>(b =>
        {
            b.ToTable("ParteConsumos");
            b.HasOne(x => x.Parte).WithMany(p => p.Consumos).HasForeignKey(x => x.ParteId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Insumo).WithMany().HasForeignKey(x => x.InsumoId).OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(x => x.Parte!.EmpresaId == EmpresaActualId);
        });

        builder.Entity<PedidoCompra>(b =>
        {
            b.ToTable("PedidosCompra");
            b.Property(x => x.Numero).HasMaxLength(20).IsRequired();
            b.Property(x => x.DocumentoProveedor).HasMaxLength(60);
            b.Property(x => x.Observacion).HasMaxLength(200);
            b.Property(x => x.MotivoCancelacion).HasMaxLength(200);
            b.HasOne(x => x.Proveedor).WithMany().HasForeignKey(x => x.ProveedorId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique();
            b.HasIndex(x => x.Estado);
            b.HasQueryFilter(x => x.EmpresaId == EmpresaActualId);
        });

        builder.Entity<PedidoDetalle>(b =>
        {
            b.ToTable("PedidoDetalles");
            b.HasOne(x => x.Pedido).WithMany(p => p.Detalles).HasForeignKey(x => x.PedidoId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Insumo).WithMany().HasForeignKey(x => x.InsumoId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.PedidoId, x.InsumoId }).IsUnique();
            b.HasIndex(x => x.InsumoId);
            b.HasQueryFilter(x => x.Pedido!.EmpresaId == EmpresaActualId);
        });

        builder.Entity<Recepcion>(b =>
        {
            b.ToTable("Recepciones");
            b.Property(x => x.Guia).HasMaxLength(60).IsRequired();
            b.Property(x => x.Observacion).HasMaxLength(200);
            b.Property(x => x.MotivoAnulacion).HasMaxLength(200);
            b.Property(x => x.Usuario).HasMaxLength(256).IsRequired();
            b.HasOne(x => x.Pedido).WithMany(p => p.Recepciones).HasForeignKey(x => x.PedidoId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Almacen).WithMany().HasForeignKey(x => x.AlmacenId).OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(x => x.EmpresaId == EmpresaActualId);
        });

        builder.Entity<RecepcionDetalle>(b =>
        {
            b.ToTable("RecepcionDetalles");
            b.HasOne(x => x.Recepcion).WithMany(r => r.Detalles).HasForeignKey(x => x.RecepcionId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.PedidoDetalle).WithMany().HasForeignKey(x => x.PedidoDetalleId).OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(x => x.Recepcion!.EmpresaId == EmpresaActualId);
        });
    }

    private void ConfigurarVentas(ModelBuilder builder)
    {
        builder.Entity<Venta>(b =>
        {
            b.ToTable("Ventas");
            b.Property(v => v.Documento).HasMaxLength(60);
            b.Property(v => v.MotivoAnulacion).HasMaxLength(200);
            b.Property(v => v.CreadoPor).HasMaxLength(256).IsRequired();
            b.HasOne(v => v.Producto).WithMany().HasForeignKey(v => v.ProductoId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(v => v.Almacen).WithMany().HasForeignKey(v => v.AlmacenId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(v => new { v.ProductoId, v.Fecha });
            b.HasIndex(v => v.Fecha);
            b.HasQueryFilter(v => v.EmpresaId == EmpresaActualId);
        });

        builder.Entity<StockProducto>(b =>
        {
            b.ToTable("StocksProducto");
            b.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Almacen).WithMany().HasForeignKey(x => x.AlmacenId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.AlmacenId, x.ProductoId }).IsUnique();
            b.HasQueryFilter(x => x.EmpresaId == EmpresaActualId);
        });

        builder.Entity<MovimientoProducto>(b =>
        {
            b.ToTable("MovimientosProducto");
            b.Property(m => m.Documento).HasMaxLength(60);
            b.Property(m => m.Motivo).HasMaxLength(200);
            b.Property(m => m.Referencia).HasMaxLength(120);
            b.Property(m => m.Usuario).HasMaxLength(256).IsRequired();
            b.HasOne(m => m.Producto).WithMany().HasForeignKey(m => m.ProductoId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(m => m.Almacen).WithMany().HasForeignKey(m => m.AlmacenId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(m => new { m.ProductoId, m.Fecha });
            b.HasQueryFilter(m => m.EmpresaId == EmpresaActualId);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AplicarAuditoria();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AplicarAuditoria();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void AplicarAuditoria()
    {
        var ahora = DateTime.UtcNow;
        var usuario = _tenant.UsuarioNombre ?? "sistema";

        foreach (var entrada in ChangeTracker.Entries())
        {
            if (entrada.State == EntityState.Added && entrada.Entity is IEntidadEmpresa conEmpresa && conEmpresa.EmpresaId == 0)
            {
                conEmpresa.EmpresaId = _tenant.EmpresaId
                    ?? throw new InvalidOperationException("No se pudo determinar la empresa del usuario.");
            }

            if (entrada.Entity is not EntidadAuditable auditable) continue;

            if (entrada.State == EntityState.Added)
            {
                auditable.CreadoEn = ahora;
                auditable.CreadoPor = usuario;
            }
            else if (entrada.State == EntityState.Modified)
            {
                auditable.ModificadoEn = ahora;
                auditable.ModificadoPor = usuario;
                entrada.Property(nameof(EntidadAuditable.CreadoEn)).IsModified = false;
                entrada.Property(nameof(EntidadAuditable.CreadoPor)).IsModified = false;
            }
        }
    }
}

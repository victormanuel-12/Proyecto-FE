using SPIn.Common;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;

namespace SPIn.Services.Importacion;

// =====================================================================================================
// 4. Productos (SKU)
// =====================================================================================================
public class ImportadorProductos(IImportacionRepository repo, IProductoService service) : ImportadorBase
{
    private static readonly Columna CSku = new("SKU", true, "Código único del producto.", "COLA-500-12");
    private static readonly Columna CNombre = new("Nombre", true, "Nombre comercial.", "Cola 500 ml × 12");
    private static readonly Columna CSabor = new("Sabor", false, "Sabor.", "Cola");
    private static readonly Columna CFormato = new("Formato", true, "Formato existente (vea «Valores válidos»).", "PET 500 ml");
    private static readonly Columna CUnidades = new("Botellas por paquete", true, "Unidades en cada paquete.", "12");
    private static readonly Columna CPaquetes = new("Paquetes por pallet", true, "Paquetes en cada pallet.", "84");

    public override string Tipo => "productos";
    public override string Titulo => "Productos";
    public override string Descripcion => "SKU que embotella la planta, con formato y empaque.";
    public override int Orden => 4;
    public override IReadOnlyList<Columna> Columnas => [CSku, CNombre, CSabor, CFormato, CUnidades, CPaquetes];
    public override IReadOnlyList<string[]> Ejemplos =>
    [
        ["COLA-500-12", "Cola 500 ml × 12", "Cola", "PET 500 ml", "12", "84"],
        ["NAR-1500-6", "Naranja 1.5 L × 6", "Naranja", "PET 1.5 L", "6", "70"]
    ];

    public override async Task<List<(string Titulo, List<string> Valores)>> ValoresValidosAsync() =>
        [("Formatos", (await repo.FormatosAsync()).Select(f => f.Nombre).ToList())];

    public override async Task<Preparacion> PrepararAsync(List<Fila> filas, OpcionesImportacion opciones)
    {
        var p = new Preparacion();
        var modo = ModoElegido(opciones, Modos);
        var formatos = Indice((await repo.FormatosAsync()).Select(x => (x.Nombre, x.Id)));
        var existentes = Indice((await repo.ProductosAsync()).Select(x => (x.Codigo, x.Id)));
        var vistos = new HashSet<string>();
        var columnas = new Dictionary<string, string> { ["Codigo"] = CSku.Nombre, ["Nombre"] = CNombre.Nombre, ["Sabor"] = CSabor.Nombre,
            ["UnidadesPorPaquete"] = CUnidades.Nombre, ["PaquetesPorPallet"] = CPaquetes.Nombre };

        foreach (var f in filas)
        {
            var errores = p.Errores.Count;
            var req = new ProductoRequest
            {
                Codigo = Requerido(f, CSku, p)?.ToUpperInvariant() ?? string.Empty,
                Nombre = Requerido(f, CNombre, p) ?? string.Empty,
                Sabor = Texto(f, CSabor),
                FormatoId = Opcion(f, CFormato, p, formatos, "el formato") ?? 0,
                UnidadesPorPaquete = Entero(f, CUnidades, p, true) ?? 0,
                PaquetesPorPallet = Entero(f, CPaquetes, p, true) ?? 0
            };
            if (p.Errores.Count > errores) continue;
            if (!ValidarDto(req, f, p, columnas)) continue;
            if (Duplicado(vistos, Clave(req.Codigo), f, CSku, p, req.Codigo)) continue;

            if (existentes.TryGetValue(Clave(req.Codigo), out var id))
            {
                if (modo == "omitir") { p.Omitidos++; continue; }
                p.Actualizados++;
                p.Pasos.Add(new Paso(f.Numero, $"Actualizar {req.Codigo} · {req.Nombre}", () => service.ActualizarAsync(id, req)));
            }
            else
            {
                p.Nuevos++;
                p.Pasos.Add(new Paso(f.Numero, $"Crear {req.Codigo} · {req.Nombre}", () => service.CrearAsync(req)));
            }
        }
        return p;
    }
}

// =====================================================================================================
// 5. Recetas (una fila por insumo; se agrupan por SKU)
// =====================================================================================================
public class ImportadorRecetas(IImportacionRepository repo, IRecetaService service) : ImportadorBase
{
    private static readonly Columna CSku = new("SKU", true, "Producto al que pertenece la línea.", "COLA-500-12");
    private static readonly Columna CInsumo = new("Insumo", true, "Código del insumo.", "PRE-500");
    private static readonly Columna CCantidad = new("Cantidad", true, "En unidad de consumo del insumo.", "1");
    private static readonly Columna CNivel = new("Por", false, "botella, paquete o pallet. Vacío: botella.", "botella");
    private static readonly Columna CMerma = new("Merma %", false, "Porcentaje de merma (0 a 50). Vacío: 0.", "1.5", "merma");

    private static readonly Dictionary<string, NivelReceta> Niveles = new()
    {
        ["botella"] = NivelReceta.PorBotella, ["porbotella"] = NivelReceta.PorBotella, ["unidad"] = NivelReceta.PorBotella,
        ["paquete"] = NivelReceta.PorPaquete, ["porpaquete"] = NivelReceta.PorPaquete,
        ["pallet"] = NivelReceta.PorPallet, ["porpallet"] = NivelReceta.PorPallet
    };

    public override string Tipo => "recetas";
    public override string Titulo => "Recetas";
    public override string Descripcion => "Insumos por botella, paquete o pallet de cada producto. Crea una nueva versión por producto.";
    public override int Orden => 5;
    public override string? Requisito => "Importe antes los productos y los insumos.";
    public override IReadOnlyList<OpcionTextoResponse> Modos =>
    [
        new() { Valor = "actualizar", Texto = "Reemplazar la receta de los productos del archivo" },
        new() { Valor = "omitir", Texto = "Solo productos que aún no tienen receta" }
    ];
    public override string? TextoFecha => "Vigente desde";
    public override IReadOnlyList<Columna> Columnas => [CSku, CInsumo, CCantidad, CNivel, CMerma];
    public override IReadOnlyList<string[]> Ejemplos =>
    [
        ["COLA-500-12", "PRE-500", "1", "botella", "0.5"],
        ["COLA-500-12", "TAPA-28", "1", "botella", "0.8"],
        ["COLA-500-12", "FILM-TERMO", "0.045", "paquete", "2"]
    ];

    public override async Task<List<(string Titulo, List<string> Valores)>> ValoresValidosAsync() =>
    [
        ("Productos", (await repo.ProductosAsync()).Where(x => x.Activo).Select(x => $"{x.Codigo} · {x.Nombre}").OrderBy(x => x).ToList()),
        ("Insumos", (await repo.InsumosAsync()).Where(x => x.Activo).Select(x => $"{x.Codigo} · {x.Nombre}").OrderBy(x => x).ToList()),
        ("Por", new List<string> { "botella", "paquete", "pallet" })
    ];

    public override async Task<Preparacion> PrepararAsync(List<Fila> filas, OpcionesImportacion opciones)
    {
        var p = new Preparacion();
        var modo = ModoElegido(opciones, Modos);
        var hoy = Reloj.Hoy;
        var vigente = opciones.Fecha ?? hoy;
        var productosLista = await repo.ProductosAsync();
        var productos = Indice(productosLista.Select(x => (x.Codigo, x.Id)));
        var insumosLista = await repo.InsumosAsync();
        var insumos = Indice(insumosLista.Select(x => (x.Codigo, x.Id)));
        var inactivos = insumosLista.Where(x => !x.Activo).Select(x => x.Id).ToHashSet();
        var noProduccion = await repo.InsumosNoProduccionAsync();
        var productosInactivos = productosLista.Where(x => !x.Activo).Select(x => x.Id).ToHashSet();
        var conReceta = await repo.ProductosConRecetaAsync();

        var grupos = new Dictionary<int, (int PrimeraFila, string Sku, RecetaRequest Receta, HashSet<int> Insumos)>();
        foreach (var f in filas)
        {
            var errores = p.Errores.Count;
            var productoId = Opcion(f, CSku, p, productos, "el producto");
            var insumoId = Opcion(f, CInsumo, p, insumos, "el insumo");
            var cantidad = Numero(f, CCantidad, p, true);
            var nivel = Opcion(f, CNivel, p, Niveles, "el nivel", false) ?? NivelReceta.PorBotella;
            var merma = Numero(f, CMerma, p, false, 0) ?? 0;
            if (cantidad is <= 0) p.Error(f.Numero, CCantidad.Nombre, "Debe ser mayor que 0.");
            if (merma > 50) p.Error(f.Numero, CMerma.Nombre, "La merma debe estar entre 0 y 50 %.");
            if (productoId is int pi && productosInactivos.Contains(pi)) p.Error(f.Numero, CSku.Nombre, "El producto está inactivo: actívelo antes de cambiar su receta.");
            if (insumoId is int ii && inactivos.Contains(ii)) p.Error(f.Numero, CInsumo.Nombre, "El insumo está inactivo.");
            if (insumoId is int ij && noProduccion.Contains(ij)) p.Error(f.Numero, CInsumo.Nombre, "Este insumo no se consume por producción: no puede ir en una receta.");
            if (p.Errores.Count > errores) continue;

            if (!grupos.TryGetValue(productoId!.Value, out var g))
            {
                g = (f.Numero, Texto(f, CSku)!.ToUpperInvariant(), new RecetaRequest { VigenteDesde = vigente, Comentario = "Importada desde archivo" }, new HashSet<int>());
                grupos[productoId.Value] = g;
            }
            if (!g.Insumos.Add(insumoId!.Value))
            {
                p.Error(f.Numero, CInsumo.Nombre, $"El insumo «{Texto(f, CInsumo)}» está repetido en la receta de {g.Sku}.");
                continue;
            }
            g.Receta.Detalles.Add(new RecetaDetalleRequest { InsumoId = insumoId.Value, Cantidad = cantidad!.Value, Nivel = nivel, MermaPorcentaje = merma });
        }

        foreach (var (productoId, g) in grupos)
        {
            if (conReceta.Contains(productoId))
            {
                if (modo == "omitir") { p.Omitidos++; continue; }
                p.Actualizados++;
            }
            else p.Nuevos++;
            p.Pasos.Add(new Paso(g.PrimeraFila, $"Receta de {g.Sku}: {g.Receta.Detalles.Count} insumos, vigente desde {vigente:dd/MM/yyyy}",
                () => service.GuardarAsync(productoId, g.Receta)));
        }
        return p;
    }
}

// =====================================================================================================
// 6. Stock inicial de insumos
// =====================================================================================================
public class ImportadorStockInsumos(IImportacionRepository repo, IInventarioService service) : ImportadorBase
{
    private static readonly Columna CAlmacen = new("Almacen", true, "Código del almacén de insumos.", "LUR-INS");
    private static readonly Columna CInsumo = new("Insumo", true, "Código del insumo.", "PRE-500");
    private static readonly Columna CCantidad = new("Cantidad", true, "En unidad de consumo del insumo.", "450000");
    private static readonly Columna CDocumento = new("Documento", false, "Referencia (ej.: inventario de apertura).", "INV-APERTURA");

    public override string Tipo => "stock-insumos";
    public override string Titulo => "Stock inicial de insumos";
    public override string Descripcion => "Saldo de apertura de cada insumo por almacén. Queda en el kardex como «Stock inicial».";
    public override int Orden => 6;
    public override string? Requisito => "Solo para insumos sin movimientos en ese almacén. Después use movimientos o conteos.";
    public override IReadOnlyList<OpcionTextoResponse> Modos =>
    [
        new() { Valor = "error", Texto = "Marcar error si el insumo ya tiene movimientos" },
        new() { Valor = "omitir", Texto = "Omitir los que ya tienen movimientos" }
    ];
    public override IReadOnlyList<Columna> Columnas => [CAlmacen, CInsumo, CCantidad, CDocumento];
    public override IReadOnlyList<string[]> Ejemplos => [["LUR-INS", "PRE-500", "450000", "INV-APERTURA"], ["LUR-INS", "CO2", "5400", "INV-APERTURA"]];

    public override async Task<List<(string Titulo, List<string> Valores)>> ValoresValidosAsync() =>
    [
        ("Almacenes de insumos", (await repo.AlmacenesAsync()).Where(a => a.Activo && a.Tipo == TipoAlmacen.Insumos).Select(a => a.Codigo).ToList()),
        ("Insumos", (await repo.InsumosAsync()).Where(x => x.Activo).Select(x => $"{x.Codigo} · {x.Nombre}").OrderBy(x => x).ToList())
    ];

    public override async Task<Preparacion> PrepararAsync(List<Fila> filas, OpcionesImportacion opciones)
    {
        var p = new Preparacion();
        var modo = ModoElegido(opciones, Modos);
        var almacenes = (await repo.AlmacenesAsync()).ToDictionary(a => Clave(a.Codigo));
        var insumos = (await repo.InsumosAsync()).ToDictionary(i => Clave(i.Codigo));
        var conMovimientos = await repo.InsumosConMovimientosAsync();
        var vistos = new HashSet<string>();

        foreach (var f in filas)
        {
            var errores = p.Errores.Count;
            var codAlmacen = Requerido(f, CAlmacen, p);
            var codInsumo = Requerido(f, CInsumo, p);
            var cantidad = Numero(f, CCantidad, p, true);
            var documento = Texto(f, CDocumento);
            if (cantidad is <= 0) p.Error(f.Numero, CCantidad.Nombre, "Debe ser mayor que 0.");
            if (documento?.Length > 60) p.Error(f.Numero, CDocumento.Nombre, "Admite hasta 60 caracteres.");

            int almacenId = 0, insumoId = 0;
            if (codAlmacen is not null)
            {
                if (!almacenes.TryGetValue(Clave(codAlmacen), out var a)) p.Error(f.Numero, CAlmacen.Nombre, $"No existe el almacén «{codAlmacen}».");
                else if (!a.Activo) p.Error(f.Numero, CAlmacen.Nombre, $"El almacén «{codAlmacen}» está inactivo.");
                else if (a.Tipo == TipoAlmacen.ProductoTerminado) p.Error(f.Numero, CAlmacen.Nombre, $"«{codAlmacen}» es de producto terminado: use la importación de stock de producto terminado.");
                else almacenId = a.Id;
            }
            if (codInsumo is not null)
            {
                if (!insumos.TryGetValue(Clave(codInsumo), out var i)) p.Error(f.Numero, CInsumo.Nombre, $"No existe el insumo «{codInsumo}».");
                else if (!i.Activo) p.Error(f.Numero, CInsumo.Nombre, $"El insumo «{codInsumo}» está inactivo.");
                else insumoId = i.Id;
            }
            if (p.Errores.Count > errores) continue;
            if (Duplicado(vistos, $"{almacenId}-{insumoId}", f, CInsumo, p, $"{codInsumo} en {codAlmacen}")) continue;

            if (conMovimientos.Contains((insumoId, almacenId)))
            {
                if (modo == "omitir") { p.Omitidos++; continue; }
                p.Error(f.Numero, CInsumo.Nombre, $"{codInsumo} ya tiene movimientos en {codAlmacen}: registre un ajuste o un conteo físico.");
                continue;
            }

            var req = new MovimientoRequest
            {
                Tipo = TipoMovimiento.StockInicial, AlmacenId = almacenId, InsumoId = insumoId, Cantidad = cantidad!.Value,
                Documento = documento ?? "Importación", Motivo = "Stock inicial importado"
            };
            p.Nuevos++;
            p.Pasos.Add(new Paso(f.Numero, $"{codInsumo!.ToUpperInvariant()} en {codAlmacen!.ToUpperInvariant()}: {cantidad:#,0.######}",
                () => service.RegistrarAsync(req)));
        }
        return p;
    }
}

// =====================================================================================================
// 7. Stock inicial de producto terminado
// =====================================================================================================
public class ImportadorStockProducto(IImportacionRepository repo, IProductoTerminadoService service) : ImportadorBase
{
    private static readonly Columna CAlmacen = new("Almacen", true, "Código del almacén de producto terminado.", "LUR-PT");
    private static readonly Columna CSku = new("SKU", true, "Código del producto.", "COLA-500-12");
    private static readonly Columna CPaquetes = new("Paquetes", true, "Paquetes en stock.", "3200");
    private static readonly Columna CDocumento = new("Documento", false, "Referencia.", "INV-APERTURA");

    public override string Tipo => "stock-producto";
    public override string Titulo => "Stock inicial de producto terminado";
    public override string Descripcion => "Paquetes en el almacén de producto terminado al empezar a usar SPIn.";
    public override int Orden => 7;
    public override string? Requisito => "Solo para productos sin movimientos en ese almacén.";
    public override IReadOnlyList<OpcionTextoResponse> Modos =>
    [
        new() { Valor = "error", Texto = "Marcar error si el producto ya tiene movimientos" },
        new() { Valor = "omitir", Texto = "Omitir los que ya tienen movimientos" }
    ];
    public override IReadOnlyList<Columna> Columnas => [CAlmacen, CSku, CPaquetes, CDocumento];
    public override IReadOnlyList<string[]> Ejemplos => [["LUR-PT", "COLA-500-12", "3200", "INV-APERTURA"]];

    public override async Task<List<(string Titulo, List<string> Valores)>> ValoresValidosAsync() =>
    [
        ("Almacenes de producto terminado", (await repo.AlmacenesAsync()).Where(a => a.Activo && a.Tipo == TipoAlmacen.ProductoTerminado).Select(a => a.Codigo).ToList()),
        ("Productos", (await repo.ProductosAsync()).Where(x => x.Activo).Select(x => $"{x.Codigo} · {x.Nombre}").OrderBy(x => x).ToList())
    ];

    public override async Task<Preparacion> PrepararAsync(List<Fila> filas, OpcionesImportacion opciones)
    {
        var p = new Preparacion();
        var modo = ModoElegido(opciones, Modos);
        var almacenes = (await repo.AlmacenesAsync()).ToDictionary(a => Clave(a.Codigo));
        var productos = (await repo.ProductosAsync()).ToDictionary(x => Clave(x.Codigo));
        var conMovimientos = await repo.ProductosConMovimientosAsync();
        var vistos = new HashSet<string>();

        foreach (var f in filas)
        {
            var errores = p.Errores.Count;
            var codAlmacen = Requerido(f, CAlmacen, p);
            var sku = Requerido(f, CSku, p);
            var paquetes = Numero(f, CPaquetes, p, true);
            var documento = Texto(f, CDocumento);
            if (paquetes is <= 0) p.Error(f.Numero, CPaquetes.Nombre, "Debe ser mayor que 0.");
            if (documento?.Length > 60) p.Error(f.Numero, CDocumento.Nombre, "Admite hasta 60 caracteres.");

            int almacenId = 0, productoId = 0;
            if (codAlmacen is not null)
            {
                if (!almacenes.TryGetValue(Clave(codAlmacen), out var a)) p.Error(f.Numero, CAlmacen.Nombre, $"No existe el almacén «{codAlmacen}».");
                else if (!a.Activo) p.Error(f.Numero, CAlmacen.Nombre, $"El almacén «{codAlmacen}» está inactivo.");
                else if (a.Tipo != TipoAlmacen.ProductoTerminado) p.Error(f.Numero, CAlmacen.Nombre, $"«{codAlmacen}» no es un almacén de producto terminado.");
                else almacenId = a.Id;
            }
            if (sku is not null)
            {
                if (!productos.TryGetValue(Clave(sku), out var x)) p.Error(f.Numero, CSku.Nombre, $"No existe el producto «{sku}».");
                else if (!x.Activo) p.Error(f.Numero, CSku.Nombre, $"El producto «{sku}» está inactivo.");
                else productoId = x.Id;
            }
            if (p.Errores.Count > errores) continue;
            if (Duplicado(vistos, $"{almacenId}-{productoId}", f, CSku, p, $"{sku} en {codAlmacen}")) continue;

            if (conMovimientos.Contains((productoId, almacenId)))
            {
                if (modo == "omitir") { p.Omitidos++; continue; }
                p.Error(f.Numero, CSku.Nombre, $"{sku} ya tiene movimientos en {codAlmacen}: registre un ajuste.");
                continue;
            }

            var req = new MovimientoProductoRequest
            {
                Tipo = TipoMovimientoProducto.StockInicial, AlmacenId = almacenId, ProductoId = productoId,
                Paquetes = paquetes!.Value, Documento = documento ?? "Importación", Motivo = "Stock inicial importado"
            };
            p.Nuevos++;
            p.Pasos.Add(new Paso(f.Numero, $"{sku!.ToUpperInvariant()} en {codAlmacen!.ToUpperInvariant()}: {paquetes:#,0.##} paquetes",
                () => service.RegistrarAsync(req)));
        }
        return p;
    }
}

// =====================================================================================================
// 8. Historial de ventas
// =====================================================================================================
public class ImportadorVentas(IImportacionRepository repo, IVentaRepository ventas, SPIn.Common.Seguridad.ITenantProvider tenant) : ImportadorBase
{
    private static readonly Columna CFecha = new("Fecha", true, "Día de la venta (dd/mm/aaaa o aaaa-mm-dd).", "15/03/2026");
    private static readonly Columna CSku = new("SKU", true, "Código del producto.", "COLA-500-12");
    private static readonly Columna CCanal = new("Canal", false, "mayorista, tradicional, moderno, distribuidor u otro. Vacío: otro.", "tradicional");
    private static readonly Columna CPaquetes = new("Paquetes", false, "Paquetes vendidos. Use esta columna o «Botellas».", "420");
    private static readonly Columna CBotellas = new("Botellas", false, "Botellas vendidas (se convierten a paquetes).", "");
    private static readonly Columna CDocumento = new("Documento", false, "Guía o factura (opcional).", "");

    private static readonly Dictionary<string, CanalVenta> Canales = new()
    {
        ["mayorista"] = CanalVenta.Mayorista,
        ["tradicional"] = CanalVenta.CanalTradicional, ["canaltradicional"] = CanalVenta.CanalTradicional, ["bodega"] = CanalVenta.CanalTradicional, ["bodegas"] = CanalVenta.CanalTradicional,
        ["moderno"] = CanalVenta.CanalModerno, ["canalmoderno"] = CanalVenta.CanalModerno, ["supermercado"] = CanalVenta.CanalModerno, ["supermercados"] = CanalVenta.CanalModerno, ["autoservicio"] = CanalVenta.CanalModerno,
        ["distribuidor"] = CanalVenta.Distribuidor, ["distribuidores"] = CanalVenta.Distribuidor,
        ["otro"] = CanalVenta.Otro, ["otros"] = CanalVenta.Otro
    };

    public override string Tipo => "ventas";
    public override string Titulo => "Historial de ventas";
    public override string Descripcion => "Ventas pasadas por día, producto y canal. Es la base del pronóstico (Entrega 8). No descuenta stock.";
    public override int Orden => 8;
    public override string? Requisito => "Importe antes los productos. Se recomienda al menos 1 año de historia.";
    public override IReadOnlyList<OpcionTextoResponse> Modos =>
    [
        new() { Valor = "reemplazar", Texto = "Reemplazar el historial del mismo día, producto y canal" },
        new() { Valor = "sumar", Texto = "Sumar a lo ya registrado" },
        new() { Valor = "omitir", Texto = "Omitir días que ya tienen historial" }
    ];
    public override IReadOnlyList<Columna> Columnas => [CFecha, CSku, CCanal, CPaquetes, CBotellas, CDocumento];
    public override IReadOnlyList<string[]> Ejemplos =>
    [
        ["2026-03-15", "COLA-500-12", "tradicional", "420", "", ""],
        ["2026-03-15", "COLA-500-12", "moderno", "180", "", ""],
        ["2026-03-16", "COLA-500-12", "tradicional", "", "5040", ""]
    ];

    public override async Task<List<(string Titulo, List<string> Valores)>> ValoresValidosAsync() =>
    [
        ("Productos", (await repo.ProductosAsync()).Select(x => $"{x.Codigo} · {x.Nombre}").OrderBy(x => x).ToList()),
        ("Canal", new List<string> { "mayorista", "tradicional", "moderno", "distribuidor", "otro" })
    ];

    public override async Task<Preparacion> PrepararAsync(List<Fila> filas, OpcionesImportacion opciones)
    {
        var p = new Preparacion();
        var modo = ModoElegido(opciones, Modos);
        var hoy = Reloj.Hoy;
        var productos = (await repo.ProductosAsync()).ToDictionary(x => Clave(x.Codigo));

        // Se agrupa por día + producto + canal: varias filas (por ejemplo, varias guías) suman un solo registro.
        var grupos = new Dictionary<(DateOnly, int, CanalVenta), (int Fila, string Sku, decimal Paquetes, List<string> Documentos)>();
        foreach (var f in filas)
        {
            var errores = p.Errores.Count;
            var textoFecha = Requerido(f, CFecha, p);
            DateOnly fecha = default;
            if (textoFecha is not null)
            {
                if (!TryFecha(textoFecha, out fecha)) p.Error(f.Numero, CFecha.Nombre, $"«{textoFecha}» no es una fecha (use dd/mm/aaaa).");
                else if (fecha > hoy) p.Error(f.Numero, CFecha.Nombre, "La fecha no puede ser futura.");
                else if (fecha < hoy.AddYears(-10)) p.Error(f.Numero, CFecha.Nombre, "La fecha es demasiado antigua.");
            }
            var sku = Requerido(f, CSku, p);
            (int Id, string Codigo, string Nombre, int UnidadesPorPaquete, bool Activo) producto = default;
            if (sku is not null && !productos.TryGetValue(Clave(sku), out producto))
                p.Error(f.Numero, CSku.Nombre, $"No existe el producto «{sku}».");
            var canal = Opcion(f, CCanal, p, Canales, "el canal", false) ?? CanalVenta.Otro;
            var paquetes = Numero(f, CPaquetes, p, false);
            var botellas = Numero(f, CBotellas, p, false);
            if (paquetes is null && botellas is null && Texto(f, CPaquetes) is null && Texto(f, CBotellas) is null)
                p.Error(f.Numero, CPaquetes.Nombre, "Indique los paquetes o las botellas.");
            else if (paquetes is not null && botellas is not null)
                p.Error(f.Numero, CBotellas.Nombre, "Use solo una columna: paquetes o botellas.");
            var documento = Texto(f, CDocumento);
            if (p.Errores.Count > errores) continue;

            var cantidad = paquetes ?? Math.Round(botellas!.Value / Math.Max(1, producto.UnidadesPorPaquete), 4);
            if (cantidad <= 0) { p.Error(f.Numero, paquetes is null ? CBotellas.Nombre : CPaquetes.Nombre, "Debe ser mayor que 0."); continue; }

            var clave = (fecha, producto.Id, canal);
            if (grupos.TryGetValue(clave, out var g))
            {
                g.Paquetes += cantidad;
                if (documento is not null) g.Documentos.Add(documento);
                grupos[clave] = g;
            }
            else grupos[clave] = (f.Numero, producto.Codigo, cantidad, documento is null ? new List<string>() : new List<string> { documento });
        }

        var nombreUsuario = tenant.UsuarioNombre ?? "sistema";
        HashSet<(DateOnly Fecha, int ProductoId, CanalVenta Canal)> existentes = grupos.Count == 0 ? [] : await repo.ClavesHistorialVentasAsync(grupos.Keys.Min(k => k.Item1), grupos.Keys.Max(k => k.Item1));
        foreach (var ((fecha, productoId, canal), g) in grupos.OrderBy(x => x.Key.Item1))
        {
            var doc = g.Documentos.Count == 0 ? null : string.Join(", ", g.Documentos.Distinct());
            if (doc?.Length > 60) doc = doc[..57] + "…";
            var existe = existentes.Contains((fecha, productoId, canal));
            if (existe && modo == "omitir") { p.Omitidos++; continue; }
            if (existe) p.Actualizados++; else p.Nuevos++;
            var reemplazar = existe && modo == "reemplazar";
            p.Pasos.Add(new Paso(g.Fila, $"{fecha:dd/MM/yyyy} · {g.Sku} · {EtiquetasVentas.De(canal)}: {g.Paquetes:#,0.##} paquetes" +
                (existe ? (reemplazar ? " (reemplaza)" : " (se suma)") : ""), async () =>
            {
                if (reemplazar)
                    foreach (var v in await ventas.HistorialMismaClaveAsync(fecha, productoId, canal)) ventas.Eliminar(v);
                ventas.Agregar(new Venta
                {
                    Fecha = fecha, ProductoId = productoId, Canal = canal, Paquetes = Math.Round(g.Paquetes, 4), Documento = doc,
                    Origen = OrigenVenta.Importacion, AfectaStock = false, CreadoEn = Reloj.Ahora, CreadoPor = nombreUsuario
                });
            }));
        }
        p.AlFinal = () => ventas.GuardarAsync();
        return p;
    }
}

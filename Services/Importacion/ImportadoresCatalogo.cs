using SPIn.Common;
using SPIn.Dtos.Request;
using SPIn.Dtos.Response;
using SPIn.Models;
using SPIn.Repository;

namespace SPIn.Services.Importacion;

// =====================================================================================================
// 1. Proveedores
// =====================================================================================================
public class ImportadorProveedores(IImportacionRepository repo, IProveedorService service) : ImportadorBase
{
    private static readonly Columna CPais = new("Pais", false, "País del proveedor. Si se deja vacío: Perú.", "Perú");
    private static readonly Columna CRuc = new("RUC", false, "Obligatorio para proveedores de Perú (11 dígitos). Identifica al proveedor.", "20100047218");
    private static readonly Columna CRazon = new("Razon social", true, "Nombre legal.", "San Miguel Industrias PET S.A.");
    private static readonly Columna CComercial = new("Nombre comercial", false, "Nombre corto.", "SMI PET");
    private static readonly Columna CContacto = new("Contacto", false, "Persona de contacto.", "Rosa Quispe");
    private static readonly Columna CTelefono = new("Telefono", false, "Teléfono.", "01 6145000");
    private static readonly Columna CEmail = new("Email", false, "Correo electrónico.", "ventas@proveedor.pe");

    public override string Tipo => "proveedores";
    public override string Titulo => "Proveedores";
    public override string Descripcion => "Proveedores nacionales (con RUC) y del extranjero.";
    public override int Orden => 1;
    public override IReadOnlyList<Columna> Columnas => [CPais, CRuc, CRazon, CComercial, CContacto, CTelefono, CEmail];
    public override IReadOnlyList<string[]> Ejemplos =>
    [
        ["Perú", "20100047218", "San Miguel Industrias PET S.A.", "SMI PET", "Rosa Quispe", "01 6145000", "ventas@smi.com.pe"],
        ["México", "", "Tapas Plásticas del Norte S.A. de C.V.", "TAPANOR", "", "", ""]
    ];

    public override Task<List<(string Titulo, List<string> Valores)>> ValoresValidosAsync() => Task.FromResult(new List<(string Titulo, List<string> Valores)>());

    public override async Task<Preparacion> PrepararAsync(List<Fila> filas, OpcionesImportacion opciones)
    {
        var p = new Preparacion();
        var modo = ModoElegido(opciones, Modos);
        var existentes = await repo.ProveedoresAsync();
        var porRuc = existentes.Where(e => !string.IsNullOrEmpty(e.Ruc)).GroupBy(e => e.Ruc!).ToDictionary(g => g.Key, g => g.First().Id);
        var porNombre = Indice(existentes.Select(e => (e.RazonSocial, e.Id)));
        var vistos = new HashSet<string>();
        var columnas = new Dictionary<string, string> { ["Pais"] = CPais.Nombre, ["Ruc"] = CRuc.Nombre, ["RazonSocial"] = CRazon.Nombre,
            ["NombreComercial"] = CComercial.Nombre, ["Contacto"] = CContacto.Nombre, ["Telefono"] = CTelefono.Nombre, ["Email"] = CEmail.Nombre };

        foreach (var f in filas)
        {
            var errores = p.Errores.Count;
            var req = new ProveedorRequest
            {
                Pais = Texto(f, CPais) ?? "Perú",
                Ruc = Texto(f, CRuc),
                RazonSocial = Requerido(f, CRazon, p) ?? string.Empty,
                NombreComercial = Texto(f, CComercial),
                Contacto = Texto(f, CContacto),
                Telefono = Texto(f, CTelefono),
                Email = Texto(f, CEmail)
            };
            var peruano = Clave(req.Pais) is "peru" or "pe";
            if (peruano) req.Pais = "Perú";
            if (peruano && req.Ruc is null) p.Error(f.Numero, CRuc.Nombre, "Es obligatorio para proveedores de Perú.");
            if (req.Ruc is not null && !Validaciones.RucValido(req.Ruc)) p.Error(f.Numero, CRuc.Nombre, $"El RUC «{req.Ruc}» no es válido.");
            ValidarDto(req, f, p, columnas);
            if (p.Errores.Count > errores) continue;

            var clave = req.Ruc ?? Clave(req.RazonSocial);
            if (Duplicado(vistos, clave, f, req.Ruc is null ? CRazon : CRuc, p, req.Ruc ?? req.RazonSocial)) continue;

            int? id = req.Ruc is not null ? (porRuc.TryGetValue(req.Ruc, out var i1) ? i1 : null)
                                          : (porNombre.TryGetValue(Clave(req.RazonSocial), out var i2) ? i2 : null);
            if (id is int existente)
            {
                if (modo == "omitir") { p.Omitidos++; continue; }
                p.Actualizados++;
                p.Pasos.Add(new Paso(f.Numero, $"Actualizar {req.RazonSocial}", () => service.ActualizarAsync(existente, req)));
            }
            else
            {
                p.Nuevos++;
                p.Pasos.Add(new Paso(f.Numero, $"Crear {req.RazonSocial}", () => service.CrearAsync(req)));
            }
        }
        return p;
    }
}

// =====================================================================================================
// 2. Insumos
// =====================================================================================================
public class ImportadorInsumos(IImportacionRepository repo, IInsumoService service) : ImportadorBase
{
    private static readonly Columna CCodigo = new("Codigo", true, "Código único del insumo (2 a 20 caracteres).", "PRE-500");
    private static readonly Columna CNombre = new("Nombre", true, "Descripción.", "Preforma PET 500 ml 21 g");
    private static readonly Columna CCategoria = new("Categoria", true, "Categoría existente (vea «Valores válidos»).", "Preformas");
    private static readonly Columna CCompra = new("Unidad compra", true, "Código o nombre de la unidad en que se compra.", "MIL");
    private static readonly Columna CConsumo = new("Unidad consumo", true, "Código o nombre de la unidad en que se consume.", "UND");
    private static readonly Columna CFactor = new("Factor", true, "Cuántas unidades de consumo trae una de compra.", "1000");
    private static readonly Columna CMinimo = new("Stock minimo", false, "En unidad de consumo. Vacío: 0.", "200000");
    private static readonly Columna CMaximo = new("Stock maximo", true, "En unidad de consumo.", "1200000");
    private static readonly Columna CCapacidad = new("Capacidad maxima", false, "Límite físico de almacenamiento (opcional).", "");
    private static readonly Columna CCritico = new("Critico", false, "sí / no. Detiene la línea si falta.", "sí");
    private static readonly Columna CImportado = new("Importado", false, "sí / no.", "no");
    private static readonly Columna CServicio = new("Nivel servicio", false, "Entre 50 y 99.9. Vacío: 95.", "95");
    private static readonly Columna CTipo = new("Tipo consumo", false, "produccion, operacion o manual. Vacío: produccion.", "produccion");
    private static readonly Columna CDiario = new("Consumo diario", false, "Solo para tipo operación o manual.", "");
    private static readonly Columna CLote = new("Controla lote", false, "sí / no.", "no");

    private static readonly Dictionary<string, TipoConsumo> Tipos = new()
    {
        ["produccion"] = TipoConsumo.PorProduccion, ["porproduccion"] = TipoConsumo.PorProduccion,
        ["operacion"] = TipoConsumo.PorOperacion, ["porhorasdeoperacion"] = TipoConsumo.PorOperacion, ["poroperacion"] = TipoConsumo.PorOperacion,
        ["manual"] = TipoConsumo.Manual
    };

    public override string Tipo => "insumos";
    public override string Titulo => "Insumos";
    public override string Descripcion => "Catálogo de insumos con unidades, conversión y política de stock.";
    public override int Orden => 2;
    public override string? Requisito => "Las categorías y unidades deben existir (Catálogo → Categorías y unidades).";
    public override IReadOnlyList<Columna> Columnas => [CCodigo, CNombre, CCategoria, CCompra, CConsumo, CFactor, CMinimo, CMaximo, CCapacidad, CCritico, CImportado, CServicio, CTipo, CDiario, CLote];
    public override IReadOnlyList<string[]> Ejemplos =>
    [
        ["PRE-500", "Preforma PET 500 ml 21 g", "Preformas", "MIL", "UND", "1000", "200000", "1200000", "", "sí", "no", "95", "produccion", "", "sí"],
        ["CO2", "Dióxido de carbono grado alimentario", "CO₂", "KG", "KG", "1", "2000", "12000", "15000", "sí", "no", "97", "produccion", "", "no"]
    ];

    public override async Task<List<(string Titulo, List<string> Valores)>> ValoresValidosAsync() =>
    [
        ("Categorías", (await repo.CategoriasAsync()).Select(c => c.Nombre).OrderBy(x => x).ToList()),
        ("Unidades (código · nombre)", (await repo.UnidadesAsync()).Select(u => $"{u.Codigo} · {u.Nombre}").OrderBy(x => x).ToList()),
        ("Tipo de consumo", new List<string> { "produccion", "operacion", "manual" })
    ];

    public override async Task<Preparacion> PrepararAsync(List<Fila> filas, OpcionesImportacion opciones)
    {
        var p = new Preparacion();
        var modo = ModoElegido(opciones, Modos);
        var categorias = Indice((await repo.CategoriasAsync()).Select(c => (c.Nombre, c.Id)));
        var unidadesLista = await repo.UnidadesAsync();
        var unidades = Indice(unidadesLista.Select(u => (u.Codigo, u.Id)).Concat(unidadesLista.Select(u => (u.Nombre, u.Id))));
        var existentes = Indice((await repo.InsumosAsync()).Select(i => (i.Codigo, i.Id)));
        var vistos = new HashSet<string>();
        var columnas = new Dictionary<string, string> { ["Codigo"] = CCodigo.Nombre, ["Nombre"] = CNombre.Nombre, ["FactorConversion"] = CFactor.Nombre,
            ["StockMinimo"] = CMinimo.Nombre, ["StockMaximo"] = CMaximo.Nombre, ["CapacidadMaxima"] = CCapacidad.Nombre,
            ["NivelServicio"] = CServicio.Nombre, ["ConsumoEstimadoDiario"] = CDiario.Nombre };

        foreach (var f in filas)
        {
            var errores = p.Errores.Count;
            var codigo = Requerido(f, CCodigo, p)?.ToUpperInvariant();
            var req = new InsumoRequest
            {
                Codigo = codigo ?? string.Empty,
                Nombre = Requerido(f, CNombre, p) ?? string.Empty,
                CategoriaId = Opcion(f, CCategoria, p, categorias, "la categoría") ?? 0,
                UnidadCompraId = Opcion(f, CCompra, p, unidades, "la unidad") ?? 0,
                UnidadConsumoId = Opcion(f, CConsumo, p, unidades, "la unidad") ?? 0,
                FactorConversion = Numero(f, CFactor, p, true) ?? 0,
                StockMinimo = Numero(f, CMinimo, p, false, 0) ?? 0,
                StockMaximo = Numero(f, CMaximo, p, true) ?? 0,
                CapacidadMaxima = Numero(f, CCapacidad, p, false),
                EsCritico = SiNo(f, CCritico, p) ?? false,
                EsImportado = SiNo(f, CImportado, p) ?? false,
                NivelServicio = Numero(f, CServicio, p, false, 95) ?? 95,
                TipoConsumo = Opcion(f, CTipo, p, Tipos, "el tipo de consumo", false) ?? TipoConsumo.PorProduccion,
                ConsumoEstimadoDiario = Numero(f, CDiario, p, false),
                ControlaLote = SiNo(f, CLote, p) ?? false
            };
            if (p.Errores.Count > errores) continue;
            if (!ValidarDto(req, f, p, columnas)) continue;
            if (req.StockMaximo < req.StockMinimo) { p.Error(f.Numero, CMaximo.Nombre, "Debe ser mayor o igual al stock mínimo."); continue; }
            if (req.CapacidadMaxima is decimal cap && cap < req.StockMaximo) { p.Error(f.Numero, CCapacidad.Nombre, "No puede ser menor que el stock máximo."); continue; }
            if (req.UnidadCompraId == req.UnidadConsumoId && req.FactorConversion != 1) { p.Error(f.Numero, CFactor.Nombre, "Si compra y consume en la misma unidad, el factor debe ser 1."); continue; }
            if (Duplicado(vistos, Clave(req.Codigo), f, CCodigo, p, req.Codigo)) continue;

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
// 3. Condiciones de compra (insumo ↔ proveedor)
// =====================================================================================================
public class ImportadorCondiciones(IImportacionRepository repo, ICondicionCompraService service) : ImportadorBase
{
    private static readonly Columna CInsumo = new("Insumo", true, "Código del insumo.", "PRE-500");
    private static readonly Columna CProveedor = new("Proveedor", true, "RUC o razón social del proveedor.", "20100047218");
    private static readonly Columna CMoneda = new("Moneda", false, "PEN o USD. Vacío: PEN.", "PEN");
    private static readonly Columna CPrecio = new("Precio", true, "Precio por unidad de compra.", "92.50");
    private static readonly Columna CEntrega = new("Entrega dias", true, "Días desde el pedido hasta la llegada.", "7");
    private static readonly Columna CVariacion = new("Variacion dias", false, "± días de variación. Vacío: 0.", "2");
    private static readonly Columna CMinimo = new("Pedido minimo", false, "En unidad de compra. Vacío: 0.", "100");
    private static readonly Columna CMultiplo = new("Multiplo", false, "Se pide en múltiplos de. Vacío: 1.", "10");
    private static readonly Columna CPrincipal = new("Principal", false, "sí / no. Proveedor principal del insumo.", "sí");

    private static readonly Dictionary<string, Moneda> Monedas = new()
    {
        ["pen"] = Moneda.PEN, ["soles"] = Moneda.PEN, ["s"] = Moneda.PEN, ["usd"] = Moneda.USD, ["dolares"] = Moneda.USD, ["us"] = Moneda.USD
    };

    public override string Tipo => "condiciones";
    public override string Titulo => "Condiciones de compra";
    public override string Descripcion => "Qué proveedor vende cada insumo: precio, tiempo de entrega, pedido mínimo.";
    public override int Orden => 3;
    public override string? Requisito => "Importe antes los insumos y los proveedores.";
    public override IReadOnlyList<Columna> Columnas => [CInsumo, CProveedor, CMoneda, CPrecio, CEntrega, CVariacion, CMinimo, CMultiplo, CPrincipal];
    public override IReadOnlyList<string[]> Ejemplos => [["PRE-500", "20100047218", "PEN", "92.50", "7", "2", "100", "10", "sí"]];

    public override async Task<List<(string Titulo, List<string> Valores)>> ValoresValidosAsync() =>
    [
        ("Insumos", (await repo.InsumosAsync()).Where(i => i.Activo).Select(i => $"{i.Codigo} · {i.Nombre}").OrderBy(x => x).ToList()),
        ("Proveedores (RUC · razón social)", (await repo.ProveedoresAsync()).Where(x => x.Activo).Select(x => $"{x.Ruc ?? "—"} · {x.RazonSocial}").OrderBy(x => x).ToList()),
        ("Moneda", new List<string> { "PEN", "USD" })
    ];

    public override async Task<Preparacion> PrepararAsync(List<Fila> filas, OpcionesImportacion opciones)
    {
        var p = new Preparacion();
        var modo = ModoElegido(opciones, Modos);
        var insumos = Indice((await repo.InsumosAsync()).Select(i => (i.Codigo, i.Id)));
        var provLista = await repo.ProveedoresAsync();
        var proveedores = Indice(provLista.Where(x => x.Ruc != null).Select(x => (x.Ruc!, x.Id)).Concat(provLista.Select(x => (x.RazonSocial, x.Id))));
        var existentes = (await repo.CondicionesAsync()).ToDictionary(c => (c.InsumoId, c.ProveedorId), c => c.Id);
        var vistos = new HashSet<string>();
        var columnas = new Dictionary<string, string> { ["PrecioUnitario"] = CPrecio.Nombre, ["TiempoEntregaDias"] = CEntrega.Nombre,
            ["VariacionEntregaDias"] = CVariacion.Nombre, ["PedidoMinimo"] = CMinimo.Nombre, ["MultiploCompra"] = CMultiplo.Nombre };

        foreach (var f in filas)
        {
            var errores = p.Errores.Count;
            var req = new CondicionCompraRequest
            {
                InsumoId = Opcion(f, CInsumo, p, insumos, "el insumo") ?? 0,
                ProveedorId = Opcion(f, CProveedor, p, proveedores, "el proveedor") ?? 0,
                Moneda = Opcion(f, CMoneda, p, Monedas, "la moneda", false) ?? Moneda.PEN,
                PrecioUnitario = Numero(f, CPrecio, p, true) ?? 0,
                TiempoEntregaDias = Entero(f, CEntrega, p, true) ?? 0,
                VariacionEntregaDias = Entero(f, CVariacion, p, false, 0) ?? 0,
                PedidoMinimo = Numero(f, CMinimo, p, false, 0) ?? 0,
                MultiploCompra = Numero(f, CMultiplo, p, false, 1) ?? 1,
                EsPrincipal = SiNo(f, CPrincipal, p) ?? false
            };
            if (p.Errores.Count > errores) continue;
            if (!ValidarDto(req, f, p, columnas)) continue;
            if (req.VariacionEntregaDias > req.TiempoEntregaDias) { p.Error(f.Numero, CVariacion.Nombre, "No puede ser mayor que el tiempo de entrega."); continue; }
            var texto = $"{Texto(f, CInsumo)} / {Texto(f, CProveedor)}";
            if (Duplicado(vistos, $"{req.InsumoId}-{req.ProveedorId}", f, CProveedor, p, texto)) continue;

            if (existentes.TryGetValue((req.InsumoId, req.ProveedorId), out var id))
            {
                if (modo == "omitir") { p.Omitidos++; continue; }
                p.Actualizados++;
                p.Pasos.Add(new Paso(f.Numero, $"Actualizar {texto}", () => service.ActualizarAsync(id, req)));
            }
            else
            {
                p.Nuevos++;
                p.Pasos.Add(new Paso(f.Numero, $"Crear {texto}", () => service.CrearAsync(req)));
            }
        }
        return p;
    }
}

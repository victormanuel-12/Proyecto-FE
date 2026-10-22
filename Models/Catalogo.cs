namespace SPIn.Models;

public class CategoriaInsumo : EntidadAuditable, IEntidadEmpresa
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
}

public class UnidadMedida : EntidadAuditable, IEntidadEmpresa
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    /// <summary>Abreviatura que se muestra junto a las cantidades. Ej.: UND, KG, ROL.</summary>
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

public class Insumo : EntidadAuditable, IEntidadEmpresa
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;

    public int CategoriaId { get; set; }
    public CategoriaInsumo? Categoria { get; set; }

    /// <summary>Unidad en la que lo vende el proveedor (caja, rollo, saco).</summary>
    public int UnidadCompraId { get; set; }
    public UnidadMedida? UnidadCompra { get; set; }

    /// <summary>Unidad en la que lo consume la línea (unidad, kg).</summary>
    public int UnidadConsumoId { get; set; }
    public UnidadMedida? UnidadConsumo { get; set; }

    /// <summary>Cuántas unidades de consumo trae una unidad de compra. Ej.: 1 caja = 2 000 preformas.</summary>
    public decimal FactorConversion { get; set; } = 1;

    // Política de stock (en unidad de consumo)
    public decimal StockMinimo { get; set; }
    public decimal StockMaximo { get; set; }
    public decimal? CapacidadMaxima { get; set; }

    public bool EsCritico { get; set; }
    public bool EsImportado { get; set; }

    /// <summary>Probabilidad deseada de no quedarse sin stock durante la reposición (%).</summary>
    public decimal NivelServicio { get; set; } = 95;

    public TipoConsumo TipoConsumo { get; set; } = TipoConsumo.PorProduccion;

    /// <summary>Plan B del pronóstico cuando aún no hay historial (en unidad de consumo por día).</summary>
    public decimal? ConsumoEstimadoDiario { get; set; }

    public bool ControlaLote { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<ProveedorInsumo> Proveedores { get; set; } = new List<ProveedorInsumo>();
}

public class Proveedor : EntidadAuditable, IEntidadEmpresa
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public string Pais { get; set; } = "Perú";
    /// <summary>Obligatorio para proveedores del Perú; vacío para extranjeros.</summary>
    public string? Ruc { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string? Contacto { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public bool Activo { get; set; } = true;

    public bool EsExtranjero => !string.Equals(Pais, "Perú", StringComparison.OrdinalIgnoreCase);

    public ICollection<ProveedorInsumo> Insumos { get; set; } = new List<ProveedorInsumo>();
}

/// <summary>Condiciones de compra de un insumo a un proveedor.</summary>
public class ProveedorInsumo : EntidadAuditable, IEntidadEmpresa
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }

    public int ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }

    public int InsumoId { get; set; }
    public Insumo? Insumo { get; set; }

    public Moneda Moneda { get; set; } = Moneda.PEN;
    /// <summary>Precio por unidad de compra.</summary>
    public decimal PrecioUnitario { get; set; }

    public int TiempoEntregaDias { get; set; }
    /// <summary>Desviación típica (±) del tiempo de entrega, en días.</summary>
    public int VariacionEntregaDias { get; set; }

    /// <summary>En unidad de compra.</summary>
    public decimal PedidoMinimo { get; set; }
    /// <summary>En unidad de compra. Ej.: se compra de 10 en 10 cajas.</summary>
    public decimal MultiploCompra { get; set; } = 1;

    public bool EsPrincipal { get; set; }
}

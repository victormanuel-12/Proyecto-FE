namespace SPIn.Models;

/// <summary>Stock actual de un insumo en un almacén (en unidad de consumo). Se actualiza solo con movimientos.</summary>
public class Stock : IEntidadEmpresa
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int InsumoId { get; set; }
    public Insumo? Insumo { get; set; }
    public int AlmacenId { get; set; }
    public Almacen? Almacen { get; set; }
    public decimal Cantidad { get; set; }
    public DateTime? UltimoMovimiento { get; set; }
}

/// <summary>
/// Movimiento de inventario. Nunca se edita ni se borra: un error se corrige revirtiéndolo,
/// lo que genera un movimiento inverso enlazado. Así el kardex siempre cuadra.
/// </summary>
public class MovimientoStock : IEntidadEmpresa
{
    public long Id { get; set; }
    public int EmpresaId { get; set; }

    /// <summary>Fecha y hora de registro, en hora de Perú.</summary>
    public DateTime Fecha { get; set; }
    public TipoMovimiento Tipo { get; set; }

    public int InsumoId { get; set; }
    public Insumo? Insumo { get; set; }
    public int AlmacenId { get; set; }
    public Almacen? Almacen { get; set; }

    /// <summary>Siempre positiva. El signo lo da el tipo.</summary>
    public decimal Cantidad { get; set; }

    /// <summary>Stock del insumo en ese almacén después del movimiento.</summary>
    public decimal SaldoAlmacen { get; set; }

    /// <summary>Guía de remisión, factura u otro documento de respaldo.</summary>
    public string? Documento { get; set; }
    public string? Motivo { get; set; }
    /// <summary>Texto que explica el origen automático (ej.: "Conteo N° 3", "Reversión del mov. 120").</summary>
    public string? Referencia { get; set; }

    public int? ConteoId { get; set; }
    /// <summary>Parte de producción que lo generó (Entrega 6). Se corrige anulando el parte.</summary>
    public int? ParteProduccionId { get; set; }
    /// <summary>Recepción de pedido que lo generó (Entrega 7). Se corrige anulando la recepción.</summary>
    public int? RecepcionId { get; set; }
    public long? RevierteId { get; set; }
    public bool Revertido { get; set; }

    public string Usuario { get; set; } = string.Empty;
}

public class ConteoFisico : IEntidadEmpresa
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int Numero { get; set; }
    public int AlmacenId { get; set; }
    public Almacen? Almacen { get; set; }
    public int? CategoriaId { get; set; }
    public CategoriaInsumo? Categoria { get; set; }
    public EstadoConteo Estado { get; set; } = EstadoConteo.Abierto;
    public string? Observacion { get; set; }

    public DateTime CreadoEn { get; set; }
    public string CreadoPor { get; set; } = string.Empty;
    public DateTime? CerradoEn { get; set; }
    public string? CerradoPor { get; set; }

    public ICollection<ConteoDetalle> Detalles { get; set; } = new List<ConteoDetalle>();
}

public class ConteoDetalle
{
    public int Id { get; set; }
    public int ConteoId { get; set; }
    public ConteoFisico? Conteo { get; set; }
    public int InsumoId { get; set; }
    public Insumo? Insumo { get; set; }
    /// <summary>Stock del sistema cuando se creó el conteo.</summary>
    public decimal StockSistema { get; set; }
    /// <summary>Lo contado físicamente; null mientras no se cuenta.</summary>
    public decimal? CantidadContada { get; set; }
    /// <summary>Stock del sistema al aprobar (puede diferir si hubo movimientos durante el conteo).</summary>
    public decimal? StockAlAprobar { get; set; }
    /// <summary>Ajuste aplicado al aprobar (contado − stock al aprobar).</summary>
    public decimal? AjusteAplicado { get; set; }
}

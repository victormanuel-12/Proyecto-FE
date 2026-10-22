using System.ComponentModel.DataAnnotations;
using SPIn.Models;

namespace SPIn.Dtos.Request;

public class FiltroInsumoRequest : FiltroListadoRequest
{
    public int? CategoriaId { get; set; }

    /// <summary>true: solo críticos para la línea.</summary>
    public bool? Criticos { get; set; }

    /// <summary>true: solo importados; false: solo nacionales.</summary>
    public bool? Importados { get; set; }
}

public class FiltroProveedorRequest : FiltroListadoRequest
{
    /// <summary>true: solo extranjeros; false: solo del Perú.</summary>
    public bool? Extranjeros { get; set; }
}

public class CategoriaRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 60 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "La descripción admite hasta 200 caracteres.")]
    public string? Descripcion { get; set; }
}

public class UnidadRequest
{
    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(10, MinimumLength = 1, ErrorMessage = "El código admite hasta 10 caracteres.")]
    [RegularExpression("^[A-Za-z0-9.]+$", ErrorMessage = "El código solo admite letras, números y punto.")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(40, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 40 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
}

public class InsumoRequest
{
    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "El código debe tener entre 2 y 20 caracteres.")]
    [RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "El código solo admite letras, números y guiones.")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 120 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la categoría.")]
    public int CategoriaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la unidad de compra.")]
    public int UnidadCompraId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la unidad de consumo.")]
    public int UnidadConsumoId { get; set; }

    [Range(typeof(decimal), "0.0001", "100000000", ErrorMessage = "El factor debe ser mayor que 0.")]
    public decimal FactorConversion { get; set; }

    [Range(typeof(decimal), "0", "1000000000000", ErrorMessage = "El stock mínimo no puede ser negativo.")]
    public decimal StockMinimo { get; set; }

    [Range(typeof(decimal), "0.0001", "1000000000000", ErrorMessage = "El stock máximo debe ser mayor que 0.")]
    public decimal StockMaximo { get; set; }

    [Range(typeof(decimal), "0.0001", "1000000000000", ErrorMessage = "La capacidad debe ser mayor que 0.")]
    public decimal? CapacidadMaxima { get; set; }

    public bool EsCritico { get; set; }
    public bool EsImportado { get; set; }

    [Range(typeof(decimal), "50", "99.9", ErrorMessage = "El nivel de servicio debe estar entre 50 % y 99.9 %.")]
    public decimal NivelServicio { get; set; } = 95;

    [EnumDataType(typeof(TipoConsumo), ErrorMessage = "Seleccione cómo se consume.")]
    public TipoConsumo TipoConsumo { get; set; }

    [Range(typeof(decimal), "0", "1000000000000", ErrorMessage = "El consumo estimado no puede ser negativo.")]
    public decimal? ConsumoEstimadoDiario { get; set; }

    public bool ControlaLote { get; set; }
}

public class ProveedorRequest
{
    [Required(ErrorMessage = "El país es obligatorio.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "El país debe tener entre 2 y 60 caracteres.")]
    public string Pais { get; set; } = "Perú";

    [RegularExpression(@"^\d{11}$", ErrorMessage = "El RUC debe tener 11 dígitos.")]
    public string? Ruc { get; set; }

    [Required(ErrorMessage = "La razón social es obligatoria.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "La razón social debe tener entre 3 y 150 caracteres.")]
    public string RazonSocial { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "El nombre comercial admite hasta 100 caracteres.")]
    public string? NombreComercial { get; set; }

    [StringLength(100, ErrorMessage = "El contacto admite hasta 100 caracteres.")]
    public string? Contacto { get; set; }

    [StringLength(30, ErrorMessage = "El teléfono admite hasta 30 caracteres.")]
    [RegularExpression(@"^[0-9+()\s-]*$", ErrorMessage = "El teléfono solo admite números, espacios, +, - y paréntesis.")]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(256, ErrorMessage = "El correo admite hasta 256 caracteres.")]
    public string? Email { get; set; }
}

public class CondicionCompraRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el insumo.")]
    public int InsumoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el proveedor.")]
    public int ProveedorId { get; set; }

    [EnumDataType(typeof(Moneda), ErrorMessage = "Seleccione la moneda.")]
    public Moneda Moneda { get; set; }

    [Range(typeof(decimal), "0", "100000000", ErrorMessage = "El precio no puede ser negativo.")]
    public decimal PrecioUnitario { get; set; }

    [Range(1, 365, ErrorMessage = "El tiempo de entrega debe estar entre 1 y 365 días.")]
    public int TiempoEntregaDias { get; set; }

    [Range(0, 365, ErrorMessage = "La variación debe estar entre 0 y 365 días.")]
    public int VariacionEntregaDias { get; set; }

    [Range(typeof(decimal), "0", "1000000000000", ErrorMessage = "El pedido mínimo no puede ser negativo.")]
    public decimal PedidoMinimo { get; set; }

    [Range(typeof(decimal), "0.0001", "1000000000000", ErrorMessage = "El múltiplo de compra debe ser mayor que 0.")]
    public decimal MultiploCompra { get; set; } = 1;

    public bool EsPrincipal { get; set; }
}

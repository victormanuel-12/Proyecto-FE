namespace SPIn.Common;

public static class Validaciones
{
    /// <summary>Valida un RUC peruano: 11 dígitos, prefijo 10/15/17/20 y dígito verificador (módulo 11).</summary>
    public static bool RucValido(string? ruc)
    {
        if (string.IsNullOrWhiteSpace(ruc) || ruc.Length != 11 || !ruc.All(char.IsAsciiDigit)) return false;
        var prefijo = ruc[..2];
        if (prefijo is not ("10" or "15" or "17" or "20")) return false;

        int[] pesos = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];
        var suma = 0;
        for (var i = 0; i < 10; i++) suma += (ruc[i] - '0') * pesos[i];
        var resto = 11 - (suma % 11);
        var verificador = resto switch { 10 => 0, 11 => 1, _ => resto };
        return verificador == ruc[10] - '0';
    }

    public static string NormalizarCodigo(string codigo) => codigo.Trim().ToUpperInvariant();

    public static string? TextoOpcional(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}

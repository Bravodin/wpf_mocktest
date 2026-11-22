namespace ErpMock.Core.Services;

/// <summary>Validação de CPF e CNPJ pelos dígitos verificadores.</summary>
public static class DocumentValidator
{
    public static string OnlyDigits(string? value) =>
        new((value ?? string.Empty).Where(char.IsDigit).ToArray());

    public static bool IsValidCpf(string? value)
    {
        var d = OnlyDigits(value);
        if (d.Length != 11 || d.Distinct().Count() == 1) return false;
        return CheckDigit(d[..9], [10, 9, 8, 7, 6, 5, 4, 3, 2]) == d[9] - '0'
            && CheckDigit(d[..10], [11, 10, 9, 8, 7, 6, 5, 4, 3, 2]) == d[10] - '0';
    }

    public static bool IsValidCnpj(string? value)
    {
        var d = OnlyDigits(value);
        if (d.Length != 14 || d.Distinct().Count() == 1) return false;
        return CheckDigit(d[..12], [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]) == d[12] - '0'
            && CheckDigit(d[..13], [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]) == d[13] - '0';
    }

    public static string Format(string? value)
    {
        var d = OnlyDigits(value);
        return d.Length switch
        {
            11 => Convert.ToUInt64(d).ToString(@"000\.000\.000\-00"),
            14 => Convert.ToUInt64(d).ToString(@"00\.000\.000\/0000\-00"),
            _ => value ?? string.Empty
        };
    }

    private static int CheckDigit(string digits, int[] weights)
    {
        var sum = digits.Select((c, i) => (c - '0') * weights[i]).Sum();
        var rest = sum % 11;
        return rest < 2 ? 0 : 11 - rest;
    }
}

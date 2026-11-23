namespace ErpMock.Tests.Api;

/// <summary>CPFs válidos e únicos por teste (os testes de API compartilham o mesmo banco).</summary>
internal static class TestDocuments
{
    private static int _next = 100_000_000 + Random.Shared.Next(0, 800_000_000);

    public static string NewCpf()
    {
        var digits = Interlocked.Increment(ref _next).ToString("000000000").Select(c => c - '0').ToList();
        digits.Add(CheckDigit(digits, 10));
        digits.Add(CheckDigit(digits, 11));
        return string.Concat(digits);
    }

    private static int CheckDigit(List<int> digits, int firstWeight)
    {
        var sum = digits.Select((d, i) => d * (firstWeight - i)).Sum();
        var rest = sum % 11;
        return rest < 2 ? 0 : 11 - rest;
    }
}

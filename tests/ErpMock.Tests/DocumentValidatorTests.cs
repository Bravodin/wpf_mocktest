using ErpMock.Core.Services;

namespace ErpMock.Tests;

public class DocumentValidatorTests
{
    [Theory]
    [InlineData("529.982.247-25", true)]
    [InlineData("52998224725", true)]
    [InlineData("529.982.247-26", false)]
    [InlineData("111.111.111-11", false)]
    [InlineData("123", false)]
    [InlineData(null, false)]
    public void Cpf(string? value, bool expected) => Assert.Equal(expected, DocumentValidator.IsValidCpf(value));

    [Theory]
    [InlineData("11.222.333/0001-81", true)]
    [InlineData("11222333000181", true)]
    [InlineData("11.222.333/0001-82", false)]
    [InlineData("00.000.000/0000-00", false)]
    public void Cnpj(string value, bool expected) => Assert.Equal(expected, DocumentValidator.IsValidCnpj(value));

    [Theory]
    [InlineData("52998224725", "529.982.247-25")]
    [InlineData("11222333000181", "11.222.333/0001-81")]
    public void Format(string value, string expected) => Assert.Equal(expected, DocumentValidator.Format(value));
}

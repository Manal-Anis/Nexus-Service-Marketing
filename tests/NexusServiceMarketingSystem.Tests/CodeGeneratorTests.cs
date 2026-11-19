using NexusServiceMarketingSystem.Services;
using Xunit;

namespace NexusServiceMarketingSystem.Tests;

/// <summary>
/// Tests of the identifier formatting (requirement section 4).
/// The order id is one type letter plus a 10 digit serial, the account id is one type
/// letter plus the 3 digit city code plus a 12 digit serial.
/// </summary>
public class CodeGeneratorTests
{
    [Theory]
    [InlineData('D', 1L, "D0000000001")]
    [InlineData('T', 42L, "T0000000042")]
    [InlineData('B', 9876543210L, "B9876543210")]
    public void FormatOrderId_Builds_TypeLetter_And_TenDigitSerial(char letter, long serial, string expected)
    {
        var result = CodeGenerator.FormatOrderId(letter, serial);

        Assert.Equal(expected, result);
        Assert.Equal(CodeGenerator.OrderIdLength, result.Length);
    }

    [Fact]
    public void FormatOrderId_Pads_Small_Serial_Numbers_With_Leading_Zeros()
    {
        var result = CodeGenerator.FormatOrderId('D', 7);

        Assert.Equal("D0000000007", result);
        Assert.StartsWith("D00000000", result, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatOrderId_Accepts_Lowercase_Letter_And_Uppercases_It()
    {
        var result = CodeGenerator.FormatOrderId('d', 5);

        Assert.Equal("D0000000005", result);
    }

    [Fact]
    public void FormatOrderId_Rejects_Non_Alpha_Letter()
    {
        Assert.Throws<ArgumentException>(() => CodeGenerator.FormatOrderId('1', 1));
        Assert.Throws<ArgumentException>(() => CodeGenerator.FormatOrderId('-', 1));
    }

    [Fact]
    public void FormatOrderId_Rejects_Negative_Serial()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CodeGenerator.FormatOrderId('D', -1));
    }

    [Theory]
    [InlineData('B', "001", 1L, "B001000000000001")]
    [InlineData('D', "010", 25L, "D010000000000025")]
    [InlineData('T', "999", 123456789012L, "T999123456789012")]
    public void FormatAccountId_Builds_Letter_CityCode_And_TwelveDigitSerial(
        char letter, string cityCode, long serial, string expected)
    {
        var result = CodeGenerator.FormatAccountId(letter, cityCode, serial);

        Assert.Equal(expected, result);
        Assert.Equal(CodeGenerator.AccountIdLength, result.Length);
    }

    [Fact]
    public void FormatAccountId_Rejects_CityCode_That_Is_Not_Three_Digits()
    {
        Assert.Throws<ArgumentException>(() => CodeGenerator.FormatAccountId('B', "1", 1));
        Assert.Throws<ArgumentException>(() => CodeGenerator.FormatAccountId('B', "0011", 1));
        Assert.Throws<ArgumentException>(() => CodeGenerator.FormatAccountId('B', "ABC", 1));
        Assert.Throws<ArgumentException>(() => CodeGenerator.FormatAccountId('B', "", 1));
    }

    [Fact]
    public void FormatAccountId_Rejects_Negative_Serial()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CodeGenerator.FormatAccountId('B', "001", -5));
    }

    [Theory]
    [InlineData("D0000000001", true)]
    [InlineData("T0000000042", true)]
    [InlineData("B0000000001", true)]
    [InlineData("X0000000001", false)]   // unknown type letter
    [InlineData("D000000001", false)]    // too short
    [InlineData("D00000000011", false)]  // too long
    [InlineData("D00000000A1", false)]   // letter inside the serial
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidOrderId_Checks_Length_And_Allowed_Letters(string? value, bool expected)
    {
        Assert.Equal(expected, CodeGenerator.IsValidOrderId(value));
    }

    [Theory]
    [InlineData("B001000000000001", true)]
    [InlineData("D010000000000025", true)]
    [InlineData("Z001000000000001", false)]
    [InlineData("B00100000000001", false)]
    [InlineData("B00100000000000A", false)]
    [InlineData(null, false)]
    public void IsValidAccountId_Checks_Length_And_Allowed_Letters(string? value, bool expected)
    {
        Assert.Equal(expected, CodeGenerator.IsValidAccountId(value));
    }

    [Fact]
    public void TryParseAccountId_Returns_TypeLetter_CityCode_And_Serial()
    {
        var parsed = CodeGenerator.TryParseAccountId("B001000000000042", out var letter, out var cityCode, out var serial);

        Assert.True(parsed);
        Assert.Equal('B', letter);
        Assert.Equal("001", cityCode);
        Assert.Equal(42L, serial);
    }

    [Fact]
    public void TryParseAccountId_Fails_For_An_Invalid_Id()
    {
        var parsed = CodeGenerator.TryParseAccountId("X001000000000042", out var letter, out var cityCode, out var serial);

        Assert.False(parsed);
        Assert.Equal('\0', letter);
        Assert.Equal(string.Empty, cityCode);
        Assert.Equal(0L, serial);
    }
}

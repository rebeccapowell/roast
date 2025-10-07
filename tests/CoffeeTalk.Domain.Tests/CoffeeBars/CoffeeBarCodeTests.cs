using CoffeeTalk.Domain;
using CoffeeTalk.Domain.CoffeeBars;
using Shouldly;

namespace CoffeeTalk.Domain.Tests.CoffeeBars;

public class CoffeeBarCodeTests
{
    [Theory]
    [InlineData("BCDF12")]
    [InlineData("bcdf34")]
    [InlineData("XK9RTY")]
    public void From_ReturnsNormalisedCode_WhenInputIsValid(string raw)
    {
        var result = CoffeeBarCode.From(raw);

        result.Value.ShouldBe(raw.Trim().ToUpperInvariant());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ABCDE")] // too short
    [InlineData("ABCDEFG")] // too long
    public void From_Throws_WhenLengthIsInvalid(string? raw)
    {
        var act = () => CoffeeBarCode.From(raw);

        Should.Throw<DomainException>(act);
    }

    [Theory]
    [InlineData("ABCDEF")] // contains A
    [InlineData("BCDIE1")] // contains I
    public void From_Throws_WhenContainsVowel(string raw)
    {
        var act = () => CoffeeBarCode.From(raw);

        Should.Throw<DomainException>(act);
    }
}

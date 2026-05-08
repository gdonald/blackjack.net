namespace Blackjack.Tests;

public class CountMethodTests
{
    [Fact]
    public void EnumHasExactlyTwoValues()
    {
        Assert.Equal(2, Enum.GetValues<CountMethod>().Length);
    }

    [Fact]
    public void SoftValueExists()
    {
        Assert.Equal("Soft", CountMethod.Soft.ToString());
    }

    [Fact]
    public void HardValueExists()
    {
        Assert.Equal("Hard", CountMethod.Hard.ToString());
    }

    [Fact]
    public void ParseReturnsCorrectEnumConstants()
    {
        Assert.Equal(CountMethod.Soft, Enum.Parse<CountMethod>("Soft"));
        Assert.Equal(CountMethod.Hard, Enum.Parse<CountMethod>("Hard"));
    }

    [Fact]
    public void ParseThrowsForInvalidValue()
    {
        Assert.Throws<ArgumentException>(() => Enum.Parse<CountMethod>("Invalid"));
    }

    [Fact]
    public void EnumConstantsMaintainOrder()
    {
        var values = Enum.GetValues<CountMethod>();
        Assert.Equal(CountMethod.Soft, values[0]);
        Assert.Equal(CountMethod.Hard, values[1]);
    }
}

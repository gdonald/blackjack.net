namespace Blackjack.Tests;

public class HandStatusTests
{
    [Fact]
    public void EnumHasExactlyFourValues()
    {
        Assert.Equal(4, Enum.GetValues<HandStatus>().Length);
    }

    [Theory]
    [InlineData(HandStatus.Unknown, "Unknown")]
    [InlineData(HandStatus.Won, "Won")]
    [InlineData(HandStatus.Lost, "Lost")]
    [InlineData(HandStatus.Push, "Push")]
    public void ValueExistsAndHasName(HandStatus status, string expected)
    {
        Assert.Equal(expected, status.ToString());
    }

    [Theory]
    [InlineData("Unknown", HandStatus.Unknown)]
    [InlineData("Won", HandStatus.Won)]
    [InlineData("Lost", HandStatus.Lost)]
    [InlineData("Push", HandStatus.Push)]
    public void ParseReturnsCorrectEnumConstants(string name, HandStatus expected)
    {
        Assert.Equal(expected, Enum.Parse<HandStatus>(name));
    }

    [Fact]
    public void ParseThrowsForInvalidValue()
    {
        Assert.Throws<ArgumentException>(() => Enum.Parse<HandStatus>("Invalid"));
    }

    [Fact]
    public void EnumConstantsMaintainOrder()
    {
        var values = Enum.GetValues<HandStatus>();
        Assert.Equal(HandStatus.Unknown, values[0]);
        Assert.Equal(HandStatus.Won, values[1]);
        Assert.Equal(HandStatus.Lost, values[2]);
        Assert.Equal(HandStatus.Push, values[3]);
    }
}

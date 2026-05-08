namespace Blackjack.Tests;

public class CardTests
{
    [Fact]
    public void ConstructorAndPropertiesWork()
    {
        var card = new Card(0, 0);
        Assert.Equal(0, card.Value);
        Assert.Equal(0, card.Suit);
    }

    [Fact]
    public void RecordSupportsValueEqualityAndCopy()
    {
        var original = new Card(12, 3);
        var copy = original with { };

        Assert.Equal(original.Value, copy.Value);
        Assert.Equal(original.Suit, copy.Suit);
        Assert.Equal(original, copy);
    }

    [Fact]
    public void IsAceIdentifiesAces()
    {
        Assert.True(new Card(0, 0).IsAce);
        Assert.False(new Card(12, 0).IsAce);
    }

    [Fact]
    public void IsTenIdentifiesTenValueCards()
    {
        Assert.False(new Card(8, 0).IsTen);
        Assert.True(new Card(9, 0).IsTen);
        Assert.True(new Card(10, 0).IsTen);
        Assert.True(new Card(11, 0).IsTen);
        Assert.True(new Card(12, 0).IsTen);
    }

    [Fact]
    public void FacesContainsCorrectCardRepresentations()
    {
        Assert.Equal("A♠", Card.Faces[0][0]);
        Assert.Equal("K♥", Card.Faces[12][1]);
        Assert.Equal("T♣", Card.Faces[9][2]);
        Assert.Equal("5♦", Card.Faces[4][3]);
    }

    [Fact]
    public void Faces2ContainsCorrectUnicodeCardRepresentations()
    {
        Assert.Equal("🂡", Card.Faces2[0][0]);
        Assert.Equal("🂾", Card.Faces2[12][1]);
        Assert.Equal("🃊", Card.Faces2[9][2]);
        Assert.Equal("🃕", Card.Faces2[4][3]);
    }
}

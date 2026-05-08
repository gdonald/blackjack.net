using Moq;

namespace Blackjack.Tests;

public class HandTests
{
    private readonly Mock<Game> _mockGame;
    private Hand _hand;

    public HandTests()
    {
        _mockGame = new Mock<Game>();
        _hand = new Hand(_mockGame.Object);
    }

    [Fact]
    public void NewHandHasCorrectDefaults()
    {
        Assert.NotNull(_hand.Cards);
        Assert.Empty(_hand.Cards);
        Assert.False(_hand.Stood);
        Assert.False(_hand.Played);
        Assert.Same(_mockGame.Object, _hand.Game);
    }

    [Fact]
    public void CloneCreatesDeepCopy()
    {
        _hand.Cards.Add(new Card(0, 0));
        _hand.Stood = true;
        _hand.Played = true;

        var cloned = _hand.Clone();

        Assert.Equal(_hand.Cards.Count, cloned.Cards.Count);
        Assert.Equal(_hand.Cards[0].Value, cloned.Cards[0].Value);
        Assert.Equal(_hand.Cards[0].Suit, cloned.Cards[0].Suit);
        Assert.Equal(_hand.Stood, cloned.Stood);
        Assert.Equal(_hand.Played, cloned.Played);
        Assert.Same(_hand.Game, cloned.Game);
        Assert.NotSame(_hand.Cards, cloned.Cards);
    }

    [Fact]
    public void CalculateValueHardCount()
    {
        _hand.Cards.Add(new Card(0, 0));
        _hand.Cards.Add(new Card(9, 0));
        _hand.Cards.Add(new Card(8, 0));

        Assert.Equal(20, _hand.CalculateValue(CountMethod.Hard, false));
    }

    [Fact]
    public void CalculateValueSoftCount()
    {
        _hand.Cards.Add(new Card(0, 0));
        _hand.Cards.Add(new Card(5, 0));

        Assert.Equal(17, _hand.CalculateValue(CountMethod.Soft, false));
    }

    [Fact]
    public void CalculateValueHandlesMultipleAces()
    {
        _hand.Cards.Add(new Card(0, 0));
        _hand.Cards.Add(new Card(0, 1));
        _hand.Cards.Add(new Card(3, 0));

        Assert.Equal(16, _hand.CalculateValue(CountMethod.Soft, false));
    }

    [Fact]
    public void CalculateValueSkipsHiddenCard()
    {
        _hand.Cards.Add(new Card(9, 0));
        _hand.Cards.Add(new Card(0, 0));
        _hand.Cards.Add(new Card(5, 0));

        Assert.Equal(16, _hand.CalculateValue(CountMethod.Soft, true));
    }

    [Fact]
    public void CalculateValueSwitchesToHardWhenSoftExceeds21()
    {
        _hand.Cards.Add(new Card(0, 0));
        _hand.Cards.Add(new Card(9, 0));
        _hand.Cards.Add(new Card(8, 0));

        Assert.Equal(20, _hand.CalculateValue(CountMethod.Soft, false));
    }

    [Fact]
    public void DealCardAddsCardFromShoe()
    {
        var mockShoe = new Mock<Shoe>(_mockGame.Object);
        var card = new Card(0, 0);
        _mockGame.Setup(g => g.Shoe).Returns(mockShoe.Object);
        mockShoe.Setup(s => s.GetNextCard()).Returns(card);

        _hand.DealCard();

        Assert.Single(_hand.Cards);
        Assert.Equal(card, _hand.Cards[0]);
    }

    [Fact]
    public void IsBlackjackIdentifiesBlackjackHands()
    {
        _hand.Cards.Add(new Card(0, 0));
        _hand.Cards.Add(new Card(9, 0));
        Assert.True(_hand.IsBlackjack());

        _hand = new Hand(_mockGame.Object);
        _hand.Cards.Add(new Card(9, 0));
        _hand.Cards.Add(new Card(9, 1));
        _hand.Cards.Add(new Card(0, 0));
        Assert.False(_hand.IsBlackjack());

        _hand = new Hand(_mockGame.Object);
        _hand.Cards.Add(new Card(9, 0));
        _hand.Cards.Add(new Card(8, 0));
        Assert.False(_hand.IsBlackjack());
    }

    [Fact]
    public void PlayedCanBeUpdated()
    {
        Assert.False(_hand.Played);
        _hand.Played = true;
        Assert.True(_hand.Played);
        _hand.Played = false;
        Assert.False(_hand.Played);
    }
}

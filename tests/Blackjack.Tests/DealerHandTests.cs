using Moq;

namespace Blackjack.Tests;

public class DealerHandTests
{
    private readonly Mock<Game> _mockGame;
    private DealerHand _dealerHand;

    public DealerHandTests()
    {
        _mockGame = new Mock<Game>();
        _dealerHand = new DealerHand(_mockGame.Object);
    }

    [Fact]
    public void IsBustedIdentifiesBustWithHardCount()
    {
        _dealerHand.HideDownCard = false;

        _dealerHand.Cards.Add(new Card(6, 0));
        _dealerHand.Cards.Add(new Card(8, 0));
        Assert.False(_dealerHand.IsBusted());

        _dealerHand.Cards.Add(new Card(0, 0));
        Assert.False(_dealerHand.IsBusted());

        _dealerHand.Cards.Add(new Card(9, 0));
        Assert.True(_dealerHand.IsBusted());
    }

    [Fact]
    public void GetValueRespectsHideDownCard()
    {
        _dealerHand.HideDownCard = true;

        _dealerHand.Cards.Add(new Card(9, 0));
        _dealerHand.Cards.Add(new Card(0, 0));
        _dealerHand.Cards.Add(new Card(4, 0));

        Assert.Equal(15, _dealerHand.GetValue(CountMethod.Soft));

        _dealerHand.HideDownCard = false;
        Assert.Equal(16, _dealerHand.GetValue(CountMethod.Soft));
    }

    [Fact]
    public void ToStringFormatsHandWithHiddenCard()
    {
        _mockGame.Setup(g => g.CardFace(13, 0)).Returns("??");
        _mockGame.Setup(g => g.CardFace(9, 0)).Returns("10♠");
        _mockGame.Setup(g => g.CardFace(0, 0)).Returns("A♠");

        _dealerHand.Cards.Add(new Card(9, 0));
        _dealerHand.Cards.Add(new Card(0, 0));

        Assert.Equal(" 10♠ ??  ⇒  10\n", _dealerHand.ToString());
    }

    [Fact]
    public void ToStringFormatsHandWithRevealedCards()
    {
        _mockGame.Setup(g => g.CardFace(9, 0)).Returns("10♠");
        _mockGame.Setup(g => g.CardFace(0, 0)).Returns("A♠");

        _dealerHand.Cards.Add(new Card(9, 0));
        _dealerHand.Cards.Add(new Card(0, 0));
        _dealerHand.HideDownCard = false;

        Assert.Equal(" 10♠ A♠  ⇒  21\n", _dealerHand.ToString());
    }

    [Fact]
    public void UpcardIsAceIdentifiesAceAsFirstCard()
    {
        _dealerHand.Cards.Add(new Card(0, 0));
        Assert.True(_dealerHand.UpcardIsAce());

        _dealerHand = new DealerHand(_mockGame.Object);
        _dealerHand.Cards.Add(new Card(9, 0));
        Assert.False(_dealerHand.UpcardIsAce());
    }

    [Fact]
    public void GetValueHandlesBothHardAndSoftCounts()
    {
        _dealerHand.Cards.Add(new Card(0, 0));
        _dealerHand.Cards.Add(new Card(4, 0));
        _dealerHand.HideDownCard = false;

        Assert.Equal(16, _dealerHand.GetValue(CountMethod.Soft));
        Assert.Equal(6, _dealerHand.GetValue(CountMethod.Hard));
    }
}

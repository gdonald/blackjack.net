using Moq;

namespace Blackjack.Tests;

public class ShoeTests
{
    private readonly Mock<Game> _mockGame;
    private readonly Shoe _shoe;

    public ShoeTests()
    {
        _mockGame = new Mock<Game>();
        _shoe = new Shoe(_mockGame.Object);
    }

    [Fact]
    public void NeedToShuffleReturnsTrueForEmptyShoe()
    {
        Assert.True(_shoe.NeedToShuffle());
    }

    [Fact]
    public void NeedToShuffleCalculatesThresholdCorrectly()
    {
        _mockGame.Setup(g => g.NumDecks).Returns(1);
        _shoe.BuildNewShoe(1);
        Assert.False(_shoe.NeedToShuffle());

        var cardsToRemove = (int)(52 * 0.81);
        for (var i = 0; i < cardsToRemove; i++)
        {
            _shoe.GetNextCard();
        }

        Assert.True(_shoe.NeedToShuffle());
    }

    [Fact]
    public void ShuffleReordersCards()
    {
        _mockGame.Setup(g => g.NumDecks).Returns(1);

        var originalOrder = new List<Card>();
        var newOrder = new List<Card>();

        _shoe.BuildNewShoe(1);
        Card? card;
        while ((card = _shoe.GetNextCard()) is not null)
        {
            originalOrder.Add(card);
        }

        _shoe.BuildNewShoe(1);
        while ((card = _shoe.GetNextCard()) is not null)
        {
            newOrder.Add(card);
        }

        Assert.NotEqual(originalOrder, newOrder);
    }

    [Fact]
    public void GetNextCardReturnsNullForEmptyShoe()
    {
        Assert.Null(_shoe.GetNextCard());
    }

    [Theory]
    [InlineData(2, 104)]
    [InlineData(6, 312)]
    public void TotalCardsReturnsCorrectNumber(int numDecks, int expected)
    {
        _mockGame.Setup(g => g.NumDecks).Returns(numDecks);
        Assert.Equal(expected, _shoe.TotalCards);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void BuildNewShoeCreatesCorrectDeckTypes(int deckType)
    {
        _mockGame.Setup(g => g.NumDecks).Returns(1);
        _shoe.BuildNewShoe(deckType);

        var cards = new List<Card>();
        Card? card;
        while ((card = _shoe.GetNextCard()) is not null)
        {
            cards.Add(card);
        }

        switch (deckType)
        {
            case 2:
                Assert.All(cards, c => Assert.True(c.IsAce));
                Assert.Equal(52, cards.Count);
                break;
            case 3:
                Assert.All(cards, c => Assert.Equal(10, c.Value));
                Assert.Equal(52, cards.Count);
                break;
            case 4:
                Assert.All(cards, c => Assert.True(c.IsAce || c.Value == 10));
                Assert.Equal(52, cards.Count);
                break;
            case 5:
                Assert.All(cards, c => Assert.Equal(6, c.Value));
                Assert.Equal(52, cards.Count);
                break;
            case 6:
                Assert.All(cards, c => Assert.Equal(7, c.Value));
                Assert.Equal(52, cards.Count);
                break;
            default:
                Assert.Equal(52, cards.Distinct().Count());
                break;
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void BuildNewShoeHonorsDifferentNumbersOfDecks(int numDecks)
    {
        _mockGame.Setup(g => g.NumDecks).Returns(numDecks);
        _shoe.BuildNewShoe(1);

        var cardCount = 0;
        while (_shoe.GetNextCard() is not null)
        {
            cardCount++;
        }

        Assert.Equal(numDecks * 52, cardCount);
    }
}

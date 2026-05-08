namespace Blackjack;

public class Shoe
{
    private static readonly int[] ShuffleSpecs = { 80, 81, 82, 84, 86, 89, 92, 95 };
    private const int CardsPerDeck = 52;
    private static readonly Random Rng = new();

    private readonly Game _game;
    internal readonly List<Card> Cards = new();

    public Shoe(Game game)
    {
        _game = game;
    }

    public virtual bool NeedToShuffle()
    {
        if (Cards.Count == 0) return true;

        var totalCards = TotalCards;
        var cardsDealt = totalCards - Cards.Count;
        var used = (cardsDealt / (double)totalCards) * 100.0;

        return used > ShuffleSpecs[_game.NumDecks - 1];
    }

    public virtual void Shuffle()
    {
        for (var pass = 0; pass < 7; pass++)
        {
            for (var i = Cards.Count - 1; i > 0; i--)
            {
                var j = Rng.Next(i + 1);
                (Cards[i], Cards[j]) = (Cards[j], Cards[i]);
            }
        }
    }

    public virtual Card? GetNextCard()
    {
        if (Cards.Count == 0) return null;
        var card = Cards[0];
        Cards.RemoveAt(0);
        return card;
    }

    public virtual void BuildNewShoe(int deckType)
    {
        switch (deckType)
        {
            case 2: NewAces(); break;
            case 3: NewJacks(); break;
            case 4: NewAcesJacks(); break;
            case 5: NewSevens(); break;
            case 6: NewEights(); break;
            default: NewRegular(); break;
        }

        Shuffle();
    }

    public int TotalCards => _game.NumDecks * CardsPerDeck;

    private void NewShoe(IReadOnlyList<int> values)
    {
        var totalCards = TotalCards;
        Cards.Clear();

        while (Cards.Count < totalCards)
        {
            for (var deck = 0; deck < _game.NumDecks; deck++)
            {
                for (var suit = 0; suit < 4; suit++)
                {
                    if (Cards.Count >= totalCards) break;

                    foreach (var value in values)
                    {
                        Cards.Add(new Card(value, suit));
                    }
                }
            }
        }
    }

    private void NewRegular() => NewShoe(Enumerable.Range(0, 13).ToArray());
    private void NewAces() => NewShoe(new[] { 0 });
    private void NewJacks() => NewShoe(new[] { 10 });
    private void NewAcesJacks() => NewShoe(new[] { 0, 10 });
    private void NewSevens() => NewShoe(new[] { 6 });
    private void NewEights() => NewShoe(new[] { 7 });
}

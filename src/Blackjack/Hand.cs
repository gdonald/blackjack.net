namespace Blackjack;

public class Hand
{
    public virtual Game Game { get; protected set; }
    public virtual List<Card> Cards { get; set; } = new();
    public virtual bool Stood { get; set; }
    public virtual bool Played { get; set; }

    public Hand(Game game)
    {
        Game = game;
    }

    public virtual Hand Clone()
    {
        var cloned = (Hand)MemberwiseClone();
        cloned.Cards = new List<Card>(Cards);
        return cloned;
    }

    protected internal int CalculateValue(CountMethod countMethod, bool skipHiddenCard)
    {
        var total = 0;

        for (var i = 0; i < Cards.Count; i++)
        {
            if (skipHiddenCard && i == 1) continue;

            var cardValue = Cards[i].Value + 1;
            var v = cardValue > 9 ? 10 : cardValue;

            if (countMethod == CountMethod.Soft && v == 1 && total < 11)
            {
                v = 11;
            }

            total += v;
        }

        if (countMethod == CountMethod.Soft && total > 21)
        {
            return CalculateValue(CountMethod.Hard, skipHiddenCard);
        }

        return total;
    }

    public virtual void DealCard()
    {
        Cards.Add(Game.Shoe.GetNextCard()!);
    }

    public virtual void DealCards(int numCards)
    {
        for (var i = 0; i < numCards; i++)
        {
            DealCard();
        }
    }

    public virtual bool IsBlackjack()
    {
        return Cards.Count == 2 && CalculateValue(CountMethod.Soft, false) == 21;
    }
}

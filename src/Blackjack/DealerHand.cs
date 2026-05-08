using System.Text;

namespace Blackjack;

public class DealerHand : Hand
{
    public virtual bool HideDownCard { get; set; } = true;

    public DealerHand(Game game) : base(game) { }

    public virtual bool IsBusted() => GetValue(CountMethod.Soft) > 21;

    public virtual int GetValue(CountMethod countMethod) =>
        CalculateValue(countMethod, HideDownCard);

    public virtual bool UpcardIsAce() => Cards[0].IsAce;

    public override string ToString()
    {
        var sb = new StringBuilder(" ");

        for (var i = 0; i < Cards.Count; i++)
        {
            if (i == 1 && HideDownCard)
            {
                sb.Append(Game.CardFace(13, 0)).Append(' ');
            }
            else
            {
                var c = Cards[i];
                sb.Append(Game.CardFace(c.Value, c.Suit)).Append(' ');
            }
        }

        sb.Append(" ⇒  ").Append(GetValue(CountMethod.Soft));
        sb.Append('\n');
        return sb.ToString();
    }
}

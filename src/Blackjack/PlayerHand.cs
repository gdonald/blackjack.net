using System.Globalization;
using System.Text;

namespace Blackjack;

public class PlayerHand : Hand
{
    public virtual int Bet { get; set; }
    public virtual HandStatus Status { get; set; } = HandStatus.Unknown;
    public virtual bool Paid { get; set; }

    public PlayerHand(Game game) : base(game)
    {
        Bet = game.CurrentBet;
    }

    public override PlayerHand Clone() => (PlayerHand)base.Clone();

    public virtual bool IsBusted() => GetValue(CountMethod.Soft) > 21;

    public virtual int GetValue(CountMethod countMethod) => CalculateValue(countMethod, false);

    public virtual bool IsDone()
    {
        Played = Played || Stood || IsBlackjack() || IsBusted() ||
                 GetValue(CountMethod.Soft) == 21 ||
                 GetValue(CountMethod.Hard) == 21;

        if (Played && !Paid && IsBusted())
        {
            Paid = true;
            Status = HandStatus.Lost;
            Game.Money -= Bet;
        }

        return Played;
    }

    public virtual bool CanSplit()
    {
        if (Stood) return false;
        if (Cards.Count != 2) return false;
        if (Game.PlayerHands.Count >= Game.MaxPlayerHands) return false;
        if (CannotCoverBet()) return false;

        return Cards[0].Value == Cards[1].Value;
    }

    public virtual bool CanDbl()
    {
        if (Stood) return false;
        if (Cards.Count != 2) return false;
        if (CannotCoverBet()) return false;

        return !IsBlackjack();
    }

    private bool CannotCoverBet() => Game.Money < Game.AllBets() + Bet;

    public virtual void Hit()
    {
        DealCard();

        if (IsDone())
        {
            Process();
            return;
        }

        Game.DrawHands();
        Game.PlayerHands[Game.CurrentHand].GetAction();
    }

    public virtual void Dbl()
    {
        DealCard();

        Played = true;
        Bet *= 2;

        if (IsDone())
        {
            Process();
        }
    }

    public virtual void Stand()
    {
        Stood = true;
        Played = true;

        if (Game.MoreHandsToPlay())
        {
            Game.PlayMoreHands();
            return;
        }

        Game.PlayDealerHand();
        Game.DrawHands();
        Game.BetOptions();
    }

    public virtual void Process()
    {
        if (Game.MoreHandsToPlay())
        {
            Game.PlayMoreHands();
            return;
        }

        Game.PlayDealerHand();
        Game.DrawHands();
        Game.BetOptions();
    }

    public virtual void GetAction()
    {
        var sb = new StringBuilder(" ");
        sb.Append("(H) Hit  (S) Stand  ");

        if (CanSplit()) sb.Append("(P) Split  ");
        if (CanDbl()) sb.Append("(D) Double");

        Console.WriteLine(sb);

        switch (Game.GetChar())
        {
            case 'h':
                Hit();
                return;
            case 's':
                Stand();
                return;
            case 'p':
                if (CanSplit())
                {
                    Game.SplitCurrentHand();
                    return;
                }
                break;
            case 'd':
                if (CanDbl())
                {
                    Dbl();
                    return;
                }
                break;
        }

        Game.DrawHands();
        GetAction();
    }

    public override string ToString()
    {
        var sb = new StringBuilder(" ");

        foreach (var c in Cards)
        {
            sb.Append(Game.CardFace(c.Value, c.Suit)).Append(' ');
        }

        sb.Append(" ⇒  ").Append(GetValue(CountMethod.Soft)).Append(' ');

        if (Status == HandStatus.Lost) sb.Append('-');
        else if (Status == HandStatus.Won) sb.Append('+');

        sb.Append('$').Append((Bet / 100.0).ToString("F2", CultureInfo.InvariantCulture));

        if (!Played && ReferenceEquals(this, Game.PlayerHands[Game.CurrentHand]))
        {
            sb.Append(" ⇐");
        }

        sb.Append(' ');

        if (Status == HandStatus.Lost) sb.Append(IsBusted() ? "Busted!" : "Lose!");
        else if (Status == HandStatus.Won) sb.Append(IsBlackjack() ? "Blackjack!" : "Win!");
        else if (Status == HandStatus.Push) sb.Append("Push!");

        sb.Append("\n\n");
        return sb.ToString();
    }
}

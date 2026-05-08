using System.Globalization;
using System.Text;

namespace Blackjack;

public class Game
{
    public const int MaxPlayerHands = 7;
    private const string SaveFile = "blackjack.txt";
    private const int MinBet = 500;

    public virtual Shoe Shoe { get; private set; }
    public virtual List<PlayerHand> PlayerHands { get; private set; } = new();
    public virtual DealerHand? DealerHand { get; set; }

    public virtual int NumDecks { get; set; } = 1;
    public virtual int DeckType { get; set; } = 1;
    public virtual int FaceType { get; set; } = 1;
    public virtual int Money { get; set; } = 10000;
    public virtual int CurrentBet { get; set; } = 500;
    public virtual int CurrentHand { get; set; }
    public virtual bool Quitting { get; set; }

    public Game()
    {
        Shoe = new Shoe(this);
        LoadGame();
    }

    public static void Run() => new Game().Loop();

    public virtual string CardFace(int value, int suit) =>
        (FaceType == 2 ? Card.Faces2 : Card.Faces)[value][suit];

    public virtual bool MoreHandsToPlay() => CurrentHand < PlayerHands.Count - 1;

    public virtual void PlayMoreHands()
    {
        CurrentHand++;
        var playerHand = PlayerHands[CurrentHand];
        playerHand.DealCard();

        if (playerHand.IsDone())
        {
            playerHand.Process();
            return;
        }

        DrawHands();
        playerHand.GetAction();
    }

    public virtual void SplitCurrentHand()
    {
        var handCount = PlayerHands.Count;
        var newHand = new PlayerHand(this);
        PlayerHands.Add(newHand);

        while (handCount > CurrentHand)
        {
            var cloned = PlayerHands[handCount - 1].Clone();
            PlayerHands[handCount] = cloned;
            handCount--;
        }

        var currentPlayerHand = PlayerHands[CurrentHand];
        var splitHand = PlayerHands[CurrentHand + 1];

        var splitCard1 = currentPlayerHand.Cards[1];
        var splitCard0 = currentPlayerHand.Cards[0];

        splitHand.Cards = new List<Card> { splitCard1 };
        currentPlayerHand.Cards = new List<Card> { splitCard0 };
        currentPlayerHand.DealCard();

        if (currentPlayerHand.IsDone())
        {
            currentPlayerHand.Process();
            return;
        }

        DrawHands();
        currentPlayerHand.GetAction();
    }

    public virtual int AllBets() => PlayerHands.Sum(h => h.Bet);

    private void NormalizeBet()
    {
        if (CurrentBet > Money) CurrentBet = Money;
    }

    public virtual void GetNewBet()
    {
        DrawHands();
        Console.Write(" (1) $5  (2) $10  (3) $25  (4) $100");

        switch (GetChar())
        {
            case '1': CurrentBet = 500; break;
            case '2': CurrentBet = 1000; break;
            case '3': CurrentBet = 2500; break;
            case '4': CurrentBet = 10000; break;
            default:
                GetNewBet();
                return;
        }

        NormalizeBet();
        DealNewHand();
    }

    public virtual void GetNewNumDecks()
    {
        DrawHands();
        Console.Write($" Number of Decks: {NumDecks}  Enter New Number of Decks (1-8): ");

        var newNumDecks = GetChar() - '0';

        if (newNumDecks < 1) newNumDecks = 1;
        else if (newNumDecks > 8) newNumDecks = 8;

        NumDecks = newNumDecks;
        GameOptions();
    }

    public virtual void GetNewDeckType()
    {
        DrawHands();
        Console.WriteLine(" (1) Regular  (2) Aces  (3) Jacks  (4) Aces & Jacks  (5) Sevens  (6) Eights");

        var newDeckType = GetChar() - '0';

        if (newDeckType > 0 && newDeckType < 7)
        {
            DeckType = newDeckType;

            if (newDeckType > 1) NumDecks = 8;

            Shoe.BuildNewShoe(DeckType);
            SaveGame();
            return;
        }

        GetNewDeckType();
    }

    public virtual void GetNewFaceType()
    {
        DrawHands();
        Console.WriteLine(" (1) A♠  (2) 🂡");

        var newFaceType = GetChar() - '0';

        if (newFaceType == 1 || newFaceType == 2)
        {
            FaceType = newFaceType;
            SaveGame();
            return;
        }

        DrawHands();
        GetNewFaceType();
    }

    public virtual void GameOptions()
    {
        DrawHands();
        Console.WriteLine(" (N) Number of Decks  (T) Deck Type  (F) Face Type  (B) Back");

        switch (GetChar())
        {
            case 'n': GetNewNumDecks(); return;
            case 't': GetNewDeckType(); return;
            case 'f': GetNewFaceType(); return;
            case 'b':
                DrawHands();
                BetOptions();
                return;
        }

        DrawHands();
        GameOptions();
    }

    public virtual void BetOptions()
    {
        Console.WriteLine(" (D) Deal Hand  (B) Change Bet  (O) Options  (Q) Quit");

        switch (GetChar())
        {
            case 'd': return;
            case 'b': GetNewBet(); return;
            case 'o': GameOptions(); return;
            case 'q':
                Quitting = true;
                Clear();
                return;
        }

        DrawHands();
        BetOptions();
    }

    public virtual void InsureHand()
    {
        var playerHand = PlayerHands[CurrentHand];
        playerHand.Bet /= 2;
        playerHand.Played = true;
        playerHand.Paid = true;
        playerHand.Status = HandStatus.Lost;
        Money -= playerHand.Bet;

        DrawHands();
        BetOptions();
    }

    public virtual void PayHands()
    {
        var dealerHandValue = DealerHand!.GetValue(CountMethod.Soft);
        var dealerHandBusted = DealerHand.IsBusted();

        foreach (var playerHand in PlayerHands)
        {
            if (playerHand.Paid) continue;

            playerHand.Paid = true;
            var playerHandValue = playerHand.GetValue(CountMethod.Soft);

            if (dealerHandBusted || playerHandValue > dealerHandValue)
            {
                if (playerHand.IsBlackjack())
                {
                    playerHand.Bet = (int)(playerHand.Bet * 1.5);
                }

                Money += playerHand.Bet;
                playerHand.Status = HandStatus.Won;
            }
            else if (playerHandValue < dealerHandValue)
            {
                Money -= playerHand.Bet;
                playerHand.Status = HandStatus.Lost;
            }
            else
            {
                playerHand.Status = HandStatus.Push;
            }
        }

        NormalizeBet();
        SaveGame();
    }

    public virtual bool NeedToPlayDealerHand() =>
        PlayerHands.Any(h => !(h.IsBusted() || h.IsBlackjack()));

    public virtual void PlayDealerHand()
    {
        if (DealerHand!.IsBlackjack())
        {
            DealerHand.HideDownCard = false;
        }

        if (!NeedToPlayDealerHand())
        {
            DealerHand.Played = true;
            PayHands();
            return;
        }

        DealerHand.HideDownCard = false;

        var softCount = DealerHand.GetValue(CountMethod.Soft);
        var hardCount = DealerHand.GetValue(CountMethod.Hard);

        while (softCount < 18 && hardCount < 17)
        {
            DealerHand.DealCard();
            softCount = DealerHand.GetValue(CountMethod.Soft);
            hardCount = DealerHand.GetValue(CountMethod.Hard);
        }

        DealerHand.Played = true;
        PayHands();
    }

    public virtual void NoInsurance()
    {
        if (DealerHand!.IsBlackjack())
        {
            DealerHand.HideDownCard = false;
            DealerHand.Played = true;

            PayHands();
            DrawHands();
            BetOptions();
            return;
        }

        var playerHand = PlayerHands[0];
        if (playerHand.IsDone())
        {
            PlayDealerHand();
            DrawHands();
            BetOptions();
            return;
        }

        DrawHands();
        playerHand.GetAction();
    }

    public virtual void AskInsurance()
    {
        Console.WriteLine(" Insurance?  (Y) Yes (N) No");

        switch (GetChar())
        {
            case 'y': InsureHand(); return;
            case 'n': NoInsurance(); return;
        }

        DrawHands();
        AskInsurance();
    }

    public virtual void DealNewHand()
    {
        if (Shoe.NeedToShuffle())
        {
            Shoe.BuildNewShoe(DeckType);
        }

        PlayerHands.Clear();
        PlayerHands.Add(new PlayerHand(this));
        CurrentHand = 0;

        DealerHand = new DealerHand(this);

        for (var i = 0; i < 2; i++)
        {
            PlayerHands[0].DealCard();
            DealerHand.DealCard();
        }

        if (DealerHand.UpcardIsAce())
        {
            DrawHands();
            AskInsurance();
            return;
        }

        if (PlayerHands[0].IsDone())
        {
            DealerHand.HideDownCard = false;

            PayHands();
            DrawHands();
            BetOptions();
            return;
        }

        DrawHands();
        PlayerHands[0].GetAction();

        SaveGame();
    }

    public virtual void DrawHands()
    {
        Clear();

        var output = new StringBuilder();
        output.Append("\n Dealer:\n").Append(DealerHand);
        output.Append(string.Format(CultureInfo.InvariantCulture, "\n Player ${0:F2}:\n", Money / 100.0));

        foreach (var playerHand in PlayerHands)
        {
            output.Append(playerHand);
        }

        Console.Write(output);
    }

    public virtual void SaveGame()
    {
        try
        {
            File.WriteAllText(SaveFile,
                string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}|{4}",
                    NumDecks, Money, CurrentBet, DeckType, FaceType));
        }
        catch (Exception)
        {
            // ignored — file system error
        }
    }

    public virtual void LoadGame()
    {
        try
        {
            var line = File.ReadAllText(SaveFile).Split('\n')[0];
            var data = line.Split('|');

            if (data.Length == 5)
            {
                NumDecks = int.Parse(data[0], CultureInfo.InvariantCulture);
                Money = int.Parse(data[1], CultureInfo.InvariantCulture);
                CurrentBet = int.Parse(data[2], CultureInfo.InvariantCulture);
                DeckType = int.Parse(data[3], CultureInfo.InvariantCulture);
                FaceType = int.Parse(data[4], CultureInfo.InvariantCulture);
            }
        }
        catch (Exception)
        {
            // ignored — file missing or malformed
        }

        if (Money < MinBet)
        {
            Money = 10000;
            CurrentBet = MinBet;
        }
    }

    public virtual char GetChar()
    {
        if (Console.IsInputRedirected)
        {
            var c = Console.In.Read();
            if (c == -1)
            {
                throw new InvalidOperationException("Error reading input: end of stream");
            }
            return (char)c;
        }

        return char.ToLowerInvariant(Console.ReadKey(intercept: true).KeyChar);
    }

    public virtual void Clear()
    {
        Console.Write("\u001b[H\u001b[2J");
        Console.Out.Flush();
    }

    public virtual void Loop()
    {
        while (!Quitting)
        {
            DealNewHand();
        }
    }
}

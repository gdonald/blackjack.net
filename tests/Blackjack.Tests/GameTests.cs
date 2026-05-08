using Moq;

namespace Blackjack.Tests;

public class GameTests : IDisposable
{
    private const string SaveFile = "blackjack.txt";

    protected readonly TextWriter _originalOut = Console.Out;
    protected readonly StringWriter _output = new();
    protected readonly Mock<Game> _gameMock;
    protected readonly Mock<Shoe> _shoeMock;
    protected readonly Game _game;
    protected readonly Shoe _shoe;

    public GameTests()
    {
        DeleteSaveFile();
        Console.SetOut(_output);

        _gameMock = new Mock<Game> { CallBase = true };
        _game = _gameMock.Object;
        _shoeMock = new Mock<Shoe>(_game) { CallBase = true };
        _shoe = _shoeMock.Object;
        _gameMock.Setup(g => g.Shoe).Returns(_shoe);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        DeleteSaveFile();
    }

    private static void DeleteSaveFile()
    {
        if (File.Exists(SaveFile)) File.Delete(SaveFile);
    }

    [Fact]
    public void ClearWritesEscapeSequence()
    {
        _game.Clear();
        Assert.Equal("\u001b[H\u001b[2J", _output.ToString());
    }

    public class SplitCurrentHandTests : GameTests
    {
        private readonly Mock<PlayerHand> _playerHandMock;
        private readonly PlayerHand _playerHand;

        public SplitCurrentHandTests()
        {
            _shoe.BuildNewShoe(1);
            _gameMock.Setup(g => g.PlayDealerHand());
            _gameMock.SetupSequence(g => g.GetChar()).Returns('s').Returns('q');

            _playerHandMock = new Mock<PlayerHand>(_game) { CallBase = true };
            _playerHand = _playerHandMock.Object;
            // Stub out GetAction on the player hand so tests don't recurse on
            // unmatched chars after the SetupSequence is exhausted (in Mockito,
            // stubbed return sequences stick to the last value; in Moq they
            // return default(char), which would cause infinite GetAction recursion).
            _playerHandMock.Setup(p => p.GetAction());
            _gameMock.Setup(g => g.PlayerHands).Returns(new List<PlayerHand> { _playerHand });
            _gameMock.Setup(g => g.DrawHands());
        }

        [Fact]
        public void SplitsCurrentHand()
        {
            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(7, 0))
                .Returns(new Card(7, 1))
                .Returns(new Card(8, 0))
                .Returns(new Card(8, 1));
            _playerHand.DealCards(2);

            _game.SplitCurrentHand();
            _gameMock.Verify(g => g.DrawHands(), Times.AtLeastOnce);
        }

        [Fact]
        public void SplitsCurrentHandWhenHandIsDone()
        {
            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(0, 0))
                .Returns(new Card(0, 1))
                .Returns(new Card(10, 0))
                .Returns(new Card(10, 1));
            _playerHand.DealCards(2);

            _game.SplitCurrentHand();
            _gameMock.Verify(g => g.DrawHands(), Times.AtLeastOnce);
        }
    }

    public class GetNewNumDecksTests : GameTests
    {
        [Fact]
        public void Works()
        {
            _gameMock.Setup(g => g.DrawHands());
            _gameMock.Setup(g => g.GameOptions());
            _gameMock.Setup(g => g.GetChar()).Returns('1');

            _game.GetNewNumDecks();
            _gameMock.Verify(g => g.GameOptions(), Times.Once);
        }

        [Fact]
        public void ClampsLow()
        {
            _gameMock.Setup(g => g.DrawHands());
            _gameMock.Setup(g => g.GameOptions());
            _gameMock.SetupSequence(g => g.GetChar()).Returns('#').Returns('1');

            _game.GetNewNumDecks();
            Assert.Equal(1, _game.NumDecks);
            _gameMock.Verify(g => g.GameOptions(), Times.Once);
        }

        [Fact]
        public void ClampsHigh()
        {
            _gameMock.Setup(g => g.DrawHands());
            _gameMock.Setup(g => g.GameOptions());
            _gameMock.SetupSequence(g => g.GetChar()).Returns('9').Returns('1');

            _game.GetNewNumDecks();
            Assert.Equal(8, _game.NumDecks);
            _gameMock.Verify(g => g.GameOptions(), Times.Once);
        }
    }

    public class GetNewDeckTypeTests : GameTests
    {
        [Fact]
        public void Works()
        {
            _gameMock.Setup(g => g.DrawHands());
            _gameMock.Setup(g => g.GetChar()).Returns('1');

            _game.GetNewDeckType();
            _gameMock.Verify(g => g.SaveGame(), Times.Once);
        }

        [Fact]
        public void DeckType2BumpsDeckCountTo8()
        {
            _gameMock.Setup(g => g.DrawHands());
            _gameMock.Setup(g => g.GetChar()).Returns('2');

            _game.GetNewDeckType();
            _gameMock.Verify(g => g.SaveGame(), Times.Once);
            Assert.Equal(8, _game.NumDecks);
        }

        [Fact]
        public void RetriesOnInvalidLow()
        {
            _gameMock.Setup(g => g.DrawHands());
            _gameMock.SetupSequence(g => g.GetChar()).Returns('#').Returns('1');

            _game.GetNewDeckType();
            _gameMock.Verify(g => g.SaveGame(), Times.Once);
            _gameMock.Verify(g => g.GetNewDeckType(), Times.Exactly(2));
        }

        [Fact]
        public void RetriesOnInvalidHigh()
        {
            _gameMock.Setup(g => g.DrawHands());
            _gameMock.SetupSequence(g => g.GetChar()).Returns('8').Returns('1');

            _game.GetNewDeckType();
            _gameMock.Verify(g => g.SaveGame(), Times.Once);
            _gameMock.Verify(g => g.GetNewDeckType(), Times.Exactly(2));
        }
    }

    public class GetNewFaceTypeTests : GameTests
    {
        [Fact]
        public void Works()
        {
            _gameMock.Setup(g => g.DrawHands());
            _gameMock.Setup(g => g.GetChar()).Returns('1');

            _game.GetNewFaceType();
            _gameMock.Verify(g => g.SaveGame(), Times.Once);
        }

        [Fact]
        public void RetriesOnInvalidInput()
        {
            _gameMock.Setup(g => g.DrawHands());
            _gameMock.SetupSequence(g => g.GetChar()).Returns('x').Returns('2');

            _game.GetNewFaceType();
            _gameMock.Verify(g => g.GetNewFaceType(), Times.Exactly(2));
            _gameMock.Verify(g => g.SaveGame(), Times.Once);
        }
    }

    public class InsureHandTests : GameTests
    {
        [Fact]
        public void TakesHalfBetFromMoney()
        {
            _gameMock.Setup(g => g.DrawHands());
            _gameMock.Setup(g => g.BetOptions());

            var playerHand = new PlayerHand(_game);
            _game.PlayerHands.Add(playerHand);

            _game.InsureHand();
            Assert.Equal(9750, _game.Money);
        }
    }

    public class GameOptionsTests : GameTests
    {
        [Fact]
        public void NewNumDecks()
        {
            _gameMock.Setup(g => g.DrawHands());
            _gameMock.Setup(g => g.GetNewNumDecks());
            _gameMock.Setup(g => g.GetChar()).Returns('n');

            _game.GameOptions();
            _gameMock.Verify(g => g.GetNewNumDecks(), Times.Once);
        }

        [Fact]
        public void NewDeckType()
        {
            _gameMock.Setup(g => g.DrawHands());
            _gameMock.Setup(g => g.GetNewDeckType());
            _gameMock.Setup(g => g.GetChar()).Returns('t');

            _game.GameOptions();
            _gameMock.Verify(g => g.GetNewDeckType(), Times.Once);
        }

        [Fact]
        public void NewFaceType()
        {
            _gameMock.Setup(g => g.DrawHands());
            _gameMock.Setup(g => g.GetNewFaceType());
            _gameMock.Setup(g => g.GetChar()).Returns('f');

            _game.GameOptions();
            _gameMock.Verify(g => g.GetNewFaceType(), Times.Once);
        }

        [Fact]
        public void GoBack()
        {
            _gameMock.Setup(g => g.DrawHands());
            _gameMock.Setup(g => g.BetOptions());
            _gameMock.Setup(g => g.GetChar()).Returns('b');

            _game.GameOptions();
            _gameMock.Verify(g => g.BetOptions(), Times.Once);
        }

        [Fact]
        public void RetriesOnInvalid()
        {
            _gameMock.Setup(g => g.DrawHands());
            _gameMock.Setup(g => g.GetNewNumDecks());
            _gameMock.SetupSequence(g => g.GetChar()).Returns('x').Returns('n');

            _game.GameOptions();
            _gameMock.Verify(g => g.GameOptions(), Times.Exactly(2));
        }
    }

    public class PlayMoreHandsTests : GameTests
    {
        private readonly Mock<PlayerHand> _hand2Mock;
        private readonly PlayerHand _hand2;

        public PlayMoreHandsTests()
        {
            _shoe.BuildNewShoe(1);
            var hand1 = new PlayerHand(_game);
            _hand2Mock = new Mock<PlayerHand>(_game) { CallBase = true };
            _hand2 = _hand2Mock.Object;

            hand1.DealCards(2);
            _game.PlayerHands.Add(hand1);

            _hand2.DealCards(1);
            _game.PlayerHands.Add(_hand2);
        }

        [Fact]
        public void NotDoneShowsAction()
        {
            _hand2Mock.Setup(h => h.IsDone()).Returns(false);
            _hand2Mock.Setup(h => h.GetAction());

            _game.PlayMoreHands();
            _gameMock.Verify(g => g.DrawHands(), Times.Once);
            _hand2Mock.Verify(h => h.GetAction(), Times.Once);
        }

        [Fact]
        public void DoneCallsProcess()
        {
            _hand2Mock.Setup(h => h.IsDone()).Returns(true);
            _hand2Mock.Setup(h => h.Process());

            _game.PlayMoreHands();
            _gameMock.Verify(g => g.DrawHands(), Times.Never);
            _hand2Mock.Verify(h => h.Process(), Times.Once);
        }
    }

    public class BetOptionsTests : GameTests
    {
        [Fact]
        public void Quit()
        {
            _gameMock.Setup(g => g.GetChar()).Returns('q');

            _game.BetOptions();
            _gameMock.Verify(g => g.Clear(), Times.Once);
            _gameMock.Verify(g => g.DrawHands(), Times.Never);
        }

        [Fact]
        public void DealNewHand()
        {
            _gameMock.Setup(g => g.DealNewHand());
            _gameMock.Setup(g => g.GetChar()).Returns('d');

            _game.BetOptions();
            _gameMock.Verify(g => g.DrawHands(), Times.Never);
        }

        [Fact]
        public void GetNewBet()
        {
            _gameMock.Setup(g => g.GetNewBet());
            _gameMock.Setup(g => g.GetChar()).Returns('b');

            _game.BetOptions();
            _gameMock.Verify(g => g.GetNewBet(), Times.Once);
            _gameMock.Verify(g => g.DrawHands(), Times.Never);
        }

        [Fact]
        public void GameOptions()
        {
            _gameMock.Setup(g => g.GameOptions());
            _gameMock.Setup(g => g.GetChar()).Returns('o');

            _game.BetOptions();
            _gameMock.Verify(g => g.GameOptions(), Times.Once);
            _gameMock.Verify(g => g.DrawHands(), Times.Never);
        }

        [Fact]
        public void RetriesOnInvalid()
        {
            _gameMock.Setup(g => g.DrawHands());
            _gameMock.SetupSequence(g => g.GetChar()).Returns('x').Returns('q');

            _game.BetOptions();
            _gameMock.Verify(g => g.BetOptions(), Times.Exactly(2));
        }
    }

    public class GetNewBetTests : GameTests
    {
        [Fact]
        public void Bet2()
        {
            _gameMock.SetupSequence(g => g.GetChar()).Returns('2').Returns('q');
            _gameMock.Setup(g => g.DealNewHand());
            _game.GetNewBet();

            Assert.Equal(1000, _game.CurrentBet);
        }

        [Fact]
        public void Bet3()
        {
            _gameMock.SetupSequence(g => g.GetChar()).Returns('3').Returns('q');
            _gameMock.Setup(g => g.DealNewHand());
            _game.GetNewBet();

            Assert.Equal(2500, _game.CurrentBet);
        }

        [Fact]
        public void Bet4()
        {
            _gameMock.SetupSequence(g => g.GetChar()).Returns('4').Returns('q');
            _gameMock.Setup(g => g.DealNewHand());
            _game.GetNewBet();

            Assert.Equal(10000, _game.CurrentBet);
        }

        [Fact]
        public void RetriesOnInvalid()
        {
            _gameMock.SetupSequence(g => g.GetChar()).Returns('5').Returns('4').Returns('q');
            _gameMock.Setup(g => g.DealNewHand());
            _game.GetNewBet();

            Assert.Equal(10000, _game.CurrentBet);
        }
    }

    public class NormalizeBetTests : GameTests
    {
        [Fact]
        public void NormalizesIfBetExceedsMoney()
        {
            _game.Money = 100;
            _gameMock.SetupSequence(g => g.GetChar()).Returns('1').Returns('q');
            _gameMock.Setup(g => g.DealNewHand());
            _game.GetNewBet();

            Assert.Equal(100, _game.CurrentBet);
        }
    }

    public class DealNewHandTests : GameTests
    {
        [Fact]
        public void DealsHand()
        {
            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(8, 0))
                .Returns(new Card(7, 0))
                .Returns(new Card(8, 0))
                .Returns(new Card(8, 0));

            _gameMock.SetupSequence(g => g.GetChar()).Returns('s').Returns('q');

            _game.DealNewHand();
            _gameMock.Verify(g => g.SaveGame(), Times.Exactly(2));
        }

        [Fact]
        public void NoNeedToShuffleSkipsBuild()
        {
            _shoeMock.Setup(s => s.NeedToShuffle()).Returns(false);
            _gameMock.SetupSequence(g => g.GetChar()).Returns('s').Returns('q');

            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(8, 0))
                .Returns(new Card(7, 0))
                .Returns(new Card(8, 0))
                .Returns(new Card(8, 0));

            _game.DealNewHand();
            _shoeMock.Verify(s => s.BuildNewShoe(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void DealerUpcardAceTriggersInsurance()
        {
            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(8, 0))
                .Returns(new Card(0, 0))
                .Returns(new Card(8, 0))
                .Returns(new Card(7, 0));

            _gameMock.SetupSequence(g => g.GetChar()).Returns('n').Returns('s').Returns('q');

            _game.DealNewHand();
            _gameMock.Verify(g => g.AskInsurance(), Times.Once);
        }

        [Fact]
        public void PlayerHandDoneSettlesImmediately()
        {
            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(0, 0))
                .Returns(new Card(3, 0))
                .Returns(new Card(9, 0))
                .Returns(new Card(5, 0));

            _gameMock.Setup(g => g.GetChar()).Returns('q');

            _game.DealNewHand();
            _gameMock.Verify(g => g.PayHands(), Times.Once);
        }
    }

    public class DrawHandsTests : GameTests
    {
        [Fact]
        public void OutputsDealerAndPlayer()
        {
            var hand = new PlayerHand(_game);
            _game.PlayerHands.Add(hand);

            _game.DrawHands();

            var output = _output.ToString();
            Assert.Contains("Dealer:", output);
            Assert.Contains("Player $100.00:", output);
        }
    }

    public class CardFaceTests : GameTests
    {
        [Fact]
        public void RegularFaces()
        {
            _gameMock.Setup(g => g.FaceType).Returns(1);

            Assert.Equal("A♠", _game.CardFace(0, 0));
            Assert.Equal("K♦", _game.CardFace(12, 3));
            Assert.Equal("7♥", _game.CardFace(6, 1));
            Assert.Equal("??", _game.CardFace(13, 0));
        }

        [Fact]
        public void Faces2()
        {
            _gameMock.Setup(g => g.FaceType).Returns(2);

            Assert.Equal("🂡", _game.CardFace(0, 0));
            Assert.Equal("🃞", _game.CardFace(12, 3));
            Assert.Equal("🂷", _game.CardFace(6, 1));
            Assert.Equal("🂠", _game.CardFace(13, 0));
        }
    }

    public class PayHandsTests : GameTests
    {
        private readonly Mock<DealerHand> _dealerHandMock;
        private readonly DealerHand _dealerHand;
        private readonly Mock<PlayerHand> _playerHandMock;
        private readonly PlayerHand _playerHand;

        public PayHandsTests()
        {
            _gameMock.Setup(g => g.SaveGame());

            _dealerHandMock = new Mock<DealerHand>(_game) { CallBase = true };
            _dealerHand = _dealerHandMock.Object;
            _game.DealerHand = _dealerHand;

            _playerHandMock = new Mock<PlayerHand>(_game) { CallBase = true };
            _playerHand = _playerHandMock.Object;
            _game.PlayerHands.Add(_playerHand);
        }

        [Fact]
        public void Saves()
        {
            _game.PayHands();
            _gameMock.Verify(g => g.SaveGame(), Times.Once);
        }

        [Fact]
        public void SkipsAlreadyPaid()
        {
            _playerHandMock.Setup(p => p.Paid).Returns(true);

            _game.PayHands();
            _gameMock.Verify(g => g.SaveGame(), Times.Once);
        }

        [Fact]
        public void DealerBustedMakesPlayerWin()
        {
            _dealerHandMock.Setup(d => d.IsBusted()).Returns(true);

            _game.PayHands();
            Assert.Equal(HandStatus.Won, _playerHand.Status);
            _gameMock.Verify(g => g.SaveGame(), Times.Once);
        }
    }

    public class NeedToPlayDealerHandTests : GameTests
    {
        private readonly Mock<PlayerHand> _playerHandMock;

        public NeedToPlayDealerHandTests()
        {
            _playerHandMock = new Mock<PlayerHand>(_game) { CallBase = true };
            _game.PlayerHands.Add(_playerHandMock.Object);
        }

        [Fact]
        public void TrueWhenNotBlackjackOrBusted()
        {
            _playerHandMock.Setup(p => p.IsBlackjack()).Returns(false);
            _playerHandMock.Setup(p => p.IsBusted()).Returns(false);

            Assert.True(_game.NeedToPlayDealerHand());
        }

        [Fact]
        public void FalseWhenBusted()
        {
            _playerHandMock.Setup(p => p.IsBusted()).Returns(true);
            Assert.False(_game.NeedToPlayDealerHand());
        }

        [Fact]
        public void FalseWhenBlackjack()
        {
            _playerHandMock.Setup(p => p.IsBlackjack()).Returns(true);
            Assert.False(_game.NeedToPlayDealerHand());
        }
    }

    public class NoInsuranceTests : GameTests
    {
        [Fact]
        public void DealerNoBlackjackPlayerNotDoneShowsAction()
        {
            var playerMock = new Mock<PlayerHand>(_game) { CallBase = true };
            _game.PlayerHands.Add(playerMock.Object);

            var dealerMock = new Mock<DealerHand>(_game) { CallBase = true };
            _game.DealerHand = dealerMock.Object;
            dealerMock.Setup(d => d.IsBlackjack()).Returns(false);

            _gameMock.SetupSequence(g => g.GetChar()).Returns('s').Returns('q');
            _gameMock.Setup(g => g.PlayDealerHand());

            _game.NoInsurance();
            playerMock.Verify(p => p.GetAction(), Times.Once);
        }

        [Fact]
        public void DealerHasBlackjackPaysHands()
        {
            var dealerMock = new Mock<DealerHand>(_game) { CallBase = true };
            _game.DealerHand = dealerMock.Object;
            dealerMock.Setup(d => d.IsBlackjack()).Returns(true);

            _gameMock.Setup(g => g.GetChar()).Returns('q');

            _game.NoInsurance();
            _gameMock.Verify(g => g.PayHands(), Times.Once);
        }

        [Fact]
        public void DealerNoBlackjackPlayerDonePlaysDealer()
        {
            var dealerMock = new Mock<DealerHand>(_game) { CallBase = true };
            _game.DealerHand = dealerMock.Object;
            dealerMock.Setup(d => d.IsBlackjack()).Returns(false);
            _gameMock.Setup(g => g.PlayDealerHand());

            var playerMock = new Mock<PlayerHand>(_game) { CallBase = true };
            _game.PlayerHands.Add(playerMock.Object);
            playerMock.Setup(p => p.IsDone()).Returns(true);

            _gameMock.Setup(g => g.GetChar()).Returns('q');

            _game.NoInsurance();
            _gameMock.Verify(g => g.PlayDealerHand(), Times.Once);
        }
    }

    public class PlayDealerHandTests : GameTests
    {
        [Fact]
        public void NoPlayerHandsPlaysAndPays()
        {
            var dealerMock = new Mock<DealerHand>(_game) { CallBase = true };
            _game.DealerHand = dealerMock.Object;
            _game.PlayDealerHand();

            Assert.True(_game.DealerHand.Played);
            _gameMock.Verify(g => g.PayHands(), Times.Once);
        }

        [Fact]
        public void DealerHasBlackjackRevealsCard()
        {
            var dealerMock = new Mock<DealerHand>(_game) { CallBase = true };
            _game.DealerHand = dealerMock.Object;
            dealerMock.Setup(d => d.IsBlackjack()).Returns(true);

            _game.PlayDealerHand();

            Assert.False(_game.DealerHand.HideDownCard);
            _gameMock.Verify(g => g.PayHands(), Times.Once);
        }

        [Fact]
        public void DealsCardsUntilSoftCount()
        {
            var dealerMock = new Mock<DealerHand>(_game) { CallBase = true };
            _game.DealerHand = dealerMock.Object;

            _gameMock.Setup(g => g.NeedToPlayDealerHand()).Returns(true);

            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(4, 0))
                .Returns(new Card(4, 0))
                .Returns(new Card(4, 0))
                .Returns(new Card(1, 0));

            _game.PlayDealerHand();

            Assert.True(_game.DealerHand.Played);
            _gameMock.Verify(g => g.PayHands(), Times.Once);
        }

        [Fact]
        public void DealsCardsUntilHardCount()
        {
            var dealerMock = new Mock<DealerHand>(_game) { CallBase = true };
            _game.DealerHand = dealerMock.Object;

            _gameMock.Setup(g => g.NeedToPlayDealerHand()).Returns(true);

            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(4, 0))
                .Returns(new Card(4, 0))
                .Returns(new Card(4, 0))
                .Returns(new Card(2, 0));

            _game.PlayDealerHand();

            Assert.True(_game.DealerHand.Played);
            _gameMock.Verify(g => g.PayHands(), Times.Once);
        }
    }

    public class AskInsuranceTests : GameTests
    {
        [Fact]
        public void Yes()
        {
            _gameMock.Setup(g => g.GetChar()).Returns('y');
            _gameMock.Setup(g => g.InsureHand());

            _game.AskInsurance();
            _gameMock.Verify(g => g.InsureHand(), Times.Once);
        }

        [Fact]
        public void No()
        {
            _gameMock.Setup(g => g.GetChar()).Returns('n');
            _gameMock.Setup(g => g.NoInsurance());

            _game.AskInsurance();
            _gameMock.Verify(g => g.NoInsurance(), Times.Once);
        }

        [Fact]
        public void RetriesOnInvalid()
        {
            _gameMock.SetupSequence(g => g.GetChar()).Returns('x').Returns('n');
            _gameMock.Setup(g => g.NoInsurance());

            _game.AskInsurance();
            _gameMock.Verify(g => g.NoInsurance(), Times.Once);
        }
    }

    public class LoopTests : GameTests
    {
        [Fact]
        public void StopsWhenQuitting()
        {
            var count = 0;
            _gameMock.Setup(g => g.DealNewHand()).Callback(() =>
            {
                count++;
                if (count >= 2) _game.Quitting = true;
            });

            _game.Loop();

            _gameMock.Verify(g => g.DealNewHand(), Times.Exactly(2));
        }
    }

    public class SaveGameTests : GameTests
    {
        [Fact]
        public void SavesStateToFile()
        {
            _game.SaveGame();
            Assert.Equal("1|10000|500|1|1", File.ReadAllText(SaveFile));
        }

        [Fact]
        public void IgnoresWriteFailure()
        {
            File.WriteAllText(SaveFile, "");
            var info = new FileInfo(SaveFile) { IsReadOnly = true };

            try
            {
                _game.SaveGame();
            }
            finally
            {
                info.IsReadOnly = false;
            }
        }
    }

    public class MoreHandsToPlayTests : GameTests
    {
        [Fact]
        public void TrueWhenSplitHandsExist()
        {
            _game.PlayerHands.Add(new PlayerHand(_game));
            Assert.False(_game.MoreHandsToPlay());

            _game.PlayerHands.Add(new PlayerHand(_game));
            Assert.True(_game.MoreHandsToPlay());
        }
    }

    public class LoadGameTests : GameTests
    {
        [Fact]
        public void LoadsValidData()
        {
            File.WriteAllText(SaveFile, "8|10000|500|1|2");

            _game.LoadGame();

            Assert.Equal(8, _game.NumDecks);
            Assert.Equal(10000, _game.Money);
            Assert.Equal(500, _game.CurrentBet);
            Assert.Equal(1, _game.DeckType);
            Assert.Equal(2, _game.FaceType);
        }

        [Fact]
        public void NoSaveFileLeavesDefaults()
        {
            _game.LoadGame();

            Assert.Equal(1, _game.NumDecks);
            Assert.Equal(10000, _game.Money);
            Assert.Equal(500, _game.CurrentBet);
            Assert.Equal(1, _game.DeckType);
            Assert.Equal(1, _game.FaceType);
        }

        [Fact]
        public void MalformedSaveFileLeavesDefaults()
        {
            File.WriteAllText(SaveFile, "8|");

            _game.LoadGame();

            Assert.Equal(1, _game.NumDecks);
            Assert.Equal(10000, _game.Money);
            Assert.Equal(500, _game.CurrentBet);
            Assert.Equal(1, _game.DeckType);
            Assert.Equal(1, _game.FaceType);
        }

        [Fact]
        public void GivesMoneyToThePoor()
        {
            File.WriteAllText(SaveFile, "8|0|500|1|2");

            _game.LoadGame();

            Assert.Equal(8, _game.NumDecks);
            Assert.Equal(10000, _game.Money);
            Assert.Equal(500, _game.CurrentBet);
            Assert.Equal(1, _game.DeckType);
            Assert.Equal(2, _game.FaceType);
        }
    }
}

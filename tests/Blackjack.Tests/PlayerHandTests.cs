using Moq;

namespace Blackjack.Tests;

public class PlayerHandTests : IDisposable
{
    private const string SaveFile = "blackjack.txt";

    private readonly TextWriter _originalOut = Console.Out;
    private readonly StringWriter _output = new();
    private readonly Mock<Game> _gameMock;
    private readonly Mock<Shoe> _shoeMock;
    private readonly Mock<PlayerHand> _handMock;
    private readonly Game _game;
    private readonly Shoe _shoe;
    private readonly PlayerHand _playerHand;

    public PlayerHandTests()
    {
        DeleteSaveFile();
        Console.SetOut(_output);

        _gameMock = new Mock<Game> { CallBase = true };
        _game = _gameMock.Object;
        _shoeMock = new Mock<Shoe>(_game) { CallBase = true };
        _shoe = _shoeMock.Object;

        _gameMock.Setup(g => g.CurrentBet).Returns(500);
        _gameMock.Setup(g => g.Shoe).Returns(_shoe);

        _handMock = new Mock<PlayerHand>(_game) { CallBase = true };
        _playerHand = _handMock.Object;
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
    public void NewPlayerHandHasCorrectDefaults()
    {
        Assert.Equal(500, _playerHand.Bet);
        Assert.False(_playerHand.Paid);
        Assert.Empty(_playerHand.Cards);
    }

    [Fact]
    public void CloneCreatesDeepCopyWithSharedGameReference()
    {
        _playerHand.DealCards(2);
        var cloned = _playerHand.Clone();

        Assert.Equal(2, cloned.Cards.Count);
        Assert.Equal(_playerHand.Cards, cloned.Cards);
        Assert.Equal(_playerHand.Bet, cloned.Bet);
        Assert.Equal(_playerHand.Paid, cloned.Paid);
    }

    [Fact]
    public void IsBustedReturnsFalseUnder21()
    {
        _handMock.Setup(p => p.GetValue(CountMethod.Soft)).Returns(21);
        Assert.False(_playerHand.IsBusted());
    }

    [Fact]
    public void IsBustedReturnsTrueOver21()
    {
        _handMock.Setup(p => p.GetValue(CountMethod.Soft)).Returns(22);
        Assert.True(_playerHand.IsBusted());
    }

    [Fact]
    public void PaidStatusUpdates()
    {
        var hand = new PlayerHand(_game);
        Assert.False(hand.Paid);
        hand.Paid = true;
        Assert.True(hand.Paid);
        hand.Paid = false;
        Assert.False(hand.Paid);
    }

    [Fact]
    public void BetCanBeUpdated()
    {
        _playerHand.Bet = 100;
        Assert.Equal(100, _playerHand.Bet);
    }

    public class ToStringTests : PlayerHandTests
    {
        public ToStringTests()
        {
            _gameMock.Setup(g => g.CardFace(It.IsAny<int>(), It.IsAny<int>()))
                .Returns<int, int>((v, s) => Card.Faces[v][s]);

            _gameMock.Setup(g => g.PlayerHands).Returns(new List<PlayerHand> { _playerHand });
            _gameMock.Setup(g => g.CurrentHand).Returns(0);
        }

        [Fact]
        public void FormatsNormalHand()
        {
            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(9, 0))
                .Returns(new Card(10, 3));

            _playerHand.DealCards(2);

            var result = _playerHand.ToString();
            Assert.Contains("T♠", result);
            Assert.Contains("J♦", result);
            Assert.Contains("$5.00", result);
            Assert.Contains("⇐", result);
        }

        [Fact]
        public void ShowsCorrectStatusMessages()
        {
            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(8, 0))
                .Returns(new Card(8, 1))
                .Returns(new Card(8, 2));

            _playerHand.DealCards(2);
            _playerHand.Status = HandStatus.Won;

            var result = _playerHand.ToString();
            Assert.Contains("+$5.00", result);
            Assert.Contains("Win!", result);

            _playerHand.Status = HandStatus.Lost;
            result = _playerHand.ToString();
            Assert.Contains("-$5.00", result);
            Assert.Contains("Lose!", result);

            _playerHand.DealCard();
            result = _playerHand.ToString();
            Assert.Contains("Busted!", result);

            _playerHand.Status = HandStatus.Push;
            result = _playerHand.ToString();
            Assert.Contains("Push!", result);
        }

        [Fact]
        public void ShowsBlackjackMessage()
        {
            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(0, 0))
                .Returns(new Card(9, 1));

            _playerHand.DealCards(2);
            _playerHand.Status = HandStatus.Won;

            Assert.Contains("Blackjack!", _playerHand.ToString());
        }

        [Fact]
        public void DoesNotShowCurrentHandIndicatorForOtherHand()
        {
            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(9, 0))
                .Returns(new Card(10, 3));

            _playerHand.DealCards(2);

            var otherHand = new PlayerHand(_game);
            _gameMock.Setup(g => g.PlayerHands)
                .Returns(new List<PlayerHand> { _playerHand, otherHand });
            _gameMock.Setup(g => g.CurrentHand).Returns(1);

            Assert.DoesNotContain("⇐", _playerHand.ToString());
        }

        [Fact]
        public void DoesNotShowCurrentHandIndicatorForPlayedHand()
        {
            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(9, 0))
                .Returns(new Card(10, 3));

            _playerHand.DealCards(2);
            _playerHand.Played = true;

            Assert.DoesNotContain("⇐", _playerHand.ToString());
        }
    }

    public class GetActionTests : PlayerHandTests
    {
        [Fact]
        public void ShowsAllOptionsWhenAvailable()
        {
            _handMock.Setup(p => p.CanSplit()).Returns(true);
            _handMock.Setup(p => p.CanDbl()).Returns(true);
            _handMock.Setup(p => p.Hit());
            _gameMock.Setup(g => g.GetChar()).Returns('h');

            _playerHand.GetAction();

            Assert.Equal(" (H) Hit  (S) Stand  (P) Split  (D) Double" + Environment.NewLine, _output.ToString());
        }

        [Fact]
        public void HidesUnavailableOptions()
        {
            _handMock.Setup(p => p.CanSplit()).Returns(false);
            _handMock.Setup(p => p.CanDbl()).Returns(false);
            _handMock.Setup(p => p.Stand());
            _gameMock.Setup(g => g.GetChar()).Returns('s');

            _playerHand.GetAction();

            Assert.Equal(" (H) Hit  (S) Stand  " + Environment.NewLine, _output.ToString());
            _handMock.Verify(p => p.Stand(), Times.Once);
        }

        [Fact]
        public void HandCanBeSplit()
        {
            _handMock.Setup(p => p.CanSplit()).Returns(true);
            _gameMock.Setup(g => g.SplitCurrentHand());
            _gameMock.Setup(g => g.GetChar()).Returns('p');

            _playerHand.GetAction();

            _gameMock.Verify(g => g.SplitCurrentHand(), Times.Once);
        }

        [Fact]
        public void PressingPWhenCannotSplitDoesNothing()
        {
            _gameMock.Setup(g => g.SplitCurrentHand());
            _handMock.Setup(p => p.Stand());
            _gameMock.SetupSequence(g => g.GetChar()).Returns('p').Returns('s');

            _playerHand.GetAction();

            _gameMock.Verify(g => g.SplitCurrentHand(), Times.Never);
        }

        [Fact]
        public void PressingPWhenCannotSplitDoesNotDouble()
        {
            _handMock.Setup(p => p.CanSplit()).Returns(false);
            _handMock.Setup(p => p.CanDbl()).Returns(true);
            _handMock.Setup(p => p.Dbl());
            _handMock.Setup(p => p.Stand());
            _gameMock.SetupSequence(g => g.GetChar()).Returns('p').Returns('s');

            _playerHand.GetAction();

            _handMock.Verify(p => p.Dbl(), Times.Never);
        }

        [Fact]
        public void HandCanDbl()
        {
            _handMock.Setup(p => p.CanDbl()).Returns(true);
            _handMock.Setup(p => p.Dbl());
            _gameMock.Setup(g => g.GetChar()).Returns('d');

            _playerHand.GetAction();

            _handMock.Verify(p => p.Dbl(), Times.Once);
        }

        [Fact]
        public void TryingToDblHandThatCannotDbl()
        {
            _handMock.Setup(p => p.Dbl());
            _handMock.Setup(p => p.Stand());
            _gameMock.SetupSequence(g => g.GetChar()).Returns('d').Returns('s');

            _playerHand.GetAction();

            _handMock.Verify(p => p.Dbl(), Times.Never);
        }

        [Fact]
        public void HandlesInvalidInput()
        {
            _handMock.Setup(p => p.Stand());
            _gameMock.SetupSequence(g => g.GetChar()).Returns('x').Returns('s');

            _playerHand.GetAction();

            _handMock.Verify(p => p.Stand(), Times.Once);
        }
    }

    public class DoubleTests : PlayerHandTests
    {
        [Fact]
        public void DealsCardDoublesBetMarksPlayed()
        {
            _handMock.Setup(p => p.DealCard());
            _handMock.Setup(p => p.Process());
            _playerHand.Bet = 1000;

            _playerHand.Dbl();

            Assert.Equal(2000, _playerHand.Bet);
            Assert.True(_playerHand.Played);
        }

        [Fact]
        public void ProcessesHandIfDone()
        {
            _handMock.Setup(p => p.DealCard());
            _handMock.Setup(p => p.IsDone()).Returns(true);
            _handMock.Setup(p => p.Process());

            _playerHand.Dbl();

            _handMock.Verify(p => p.Process(), Times.Once);
        }

        [Fact]
        public void DoesNotProcessHandIfNotDone()
        {
            _handMock.Setup(p => p.DealCard());
            _handMock.Setup(p => p.IsDone()).Returns(false);

            _playerHand.Dbl();

            _handMock.Verify(p => p.Process(), Times.Never);
        }
    }

    public class HitTests : PlayerHandTests
    {
        [Fact]
        public void ProcessesAndDoesNotContinueWhenDone()
        {
            _handMock.Setup(p => p.DealCard());
            _handMock.Setup(p => p.IsDone()).Returns(true);
            _handMock.Setup(p => p.Process());

            _playerHand.Hit();

            _gameMock.Verify(g => g.DrawHands(), Times.Never);
            _gameMock.Verify(g => g.PlayerHands, Times.Never);
        }

        [Fact]
        public void ContinuesToNextActionWhenNotDone()
        {
            _gameMock.Setup(g => g.PlayerHands).Returns(new List<PlayerHand> { _playerHand });
            _gameMock.Setup(g => g.CurrentHand).Returns(0);
            _gameMock.Setup(g => g.DrawHands());
            _handMock.Setup(p => p.DealCard());
            _handMock.Setup(p => p.IsDone()).Returns(false);
            _handMock.Setup(p => p.GetAction());

            _playerHand.Hit();

            _gameMock.Verify(g => g.DrawHands(), Times.Once);
            _handMock.Verify(p => p.GetAction(), Times.Once);
        }
    }

    public class StandTests : PlayerHandTests
    {
        [Fact]
        public void MarksHandStatusWhenMoreHandsToPlay()
        {
            _gameMock.Setup(g => g.PlayMoreHands());
            _gameMock.Setup(g => g.MoreHandsToPlay()).Returns(true);

            _playerHand.Stand();

            _gameMock.Verify(g => g.PlayMoreHands(), Times.Once);
            _gameMock.Verify(g => g.PlayDealerHand(), Times.Never);
            _gameMock.Verify(g => g.DrawHands(), Times.Never);
            _gameMock.Verify(g => g.BetOptions(), Times.Never);

            Assert.True(_playerHand.Stood);
            Assert.True(_playerHand.Played);
        }

        [Fact]
        public void PlaysDealerHandWhenNoMoreHands()
        {
            _gameMock.Setup(g => g.PlayDealerHand());
            _gameMock.Setup(g => g.DrawHands());
            _gameMock.Setup(g => g.BetOptions());
            _gameMock.Setup(g => g.MoreHandsToPlay()).Returns(false);

            _playerHand.Stand();

            _gameMock.Verify(g => g.PlayMoreHands(), Times.Never);
            _gameMock.Verify(g => g.PlayDealerHand(), Times.Once);
            _gameMock.Verify(g => g.DrawHands(), Times.Once);
            _gameMock.Verify(g => g.BetOptions(), Times.Once);

            Assert.True(_playerHand.Stood);
            Assert.True(_playerHand.Played);
        }
    }

    public class ProcessTests : PlayerHandTests
    {
        [Fact]
        public void PlaysMoreHandsWhenAvailable()
        {
            _gameMock.Setup(g => g.MoreHandsToPlay()).Returns(true);
            _gameMock.Setup(g => g.PlayMoreHands());

            _playerHand.Process();

            _gameMock.Verify(g => g.PlayMoreHands(), Times.Once);
        }

        [Fact]
        public void DoesNotPlayMoreHandsWhenNoneRemain()
        {
            _gameMock.Setup(g => g.PlayDealerHand());
            _gameMock.Setup(g => g.DrawHands());
            _gameMock.Setup(g => g.BetOptions());
            _gameMock.Setup(g => g.MoreHandsToPlay()).Returns(false);

            _playerHand.Process();

            _gameMock.Verify(g => g.PlayMoreHands(), Times.Never);
        }
    }

    public class CanDblTests : PlayerHandTests
    {
        [Fact]
        public void ReturnsFalseWhenCannotCoverBet()
        {
            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(2, 0))
                .Returns(new Card(3, 0));
            _playerHand.DealCards(2);

            _game.Money = 0;

            Assert.False(_playerHand.CanDbl());
        }

        [Fact]
        public void ReturnsTrueWhenAllConditionsMet()
        {
            _gameMock.Setup(g => g.Money).Returns(10000);
            _gameMock.Setup(g => g.AllBets()).Returns(1000);
            _playerHand.Bet = 500;

            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(2, 0))
                .Returns(new Card(3, 0));
            _playerHand.DealCards(2);

            Assert.True(_playerHand.CanDbl());
        }

        [Fact]
        public void ReturnsFalseWhenAlreadyStood()
        {
            _gameMock.Setup(g => g.Money).Returns(10000);
            _gameMock.Setup(g => g.AllBets()).Returns(1000);
            _playerHand.Bet = 500;

            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(2, 0))
                .Returns(new Card(3, 0));
            _playerHand.DealCards(2);

            _gameMock.Setup(g => g.MoreHandsToPlay()).Returns(false);
            _gameMock.Setup(g => g.PlayDealerHand());
            _gameMock.Setup(g => g.DrawHands());
            _gameMock.Setup(g => g.BetOptions());

            _playerHand.Stand();

            Assert.False(_playerHand.CanDbl());
        }

        [Fact]
        public void ReturnsFalseWhenBlackjack()
        {
            _gameMock.Setup(g => g.Money).Returns(10000);
            _gameMock.Setup(g => g.AllBets()).Returns(1000);
            _playerHand.Bet = 500;

            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(0, 0))
                .Returns(new Card(9, 0));
            _playerHand.DealCards(2);

            Assert.False(_playerHand.CanDbl());
        }
    }

    public class CanSplitTests : PlayerHandTests
    {
        [Fact]
        public void ReturnsFalseWhenCannotCoverBet()
        {
            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(2, 0))
                .Returns(new Card(2, 0));
            _playerHand.DealCards(2);

            _game.Money = 0;

            Assert.False(_playerHand.CanSplit());
        }

        [Fact]
        public void ReturnsFalseWhenNotPair()
        {
            _gameMock.Setup(g => g.Money).Returns(10000);
            _gameMock.Setup(g => g.AllBets()).Returns(1000);
            _playerHand.Bet = 500;

            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(2, 0))
                .Returns(new Card(3, 0));
            _playerHand.DealCards(2);

            Assert.False(_playerHand.CanSplit());
        }

        [Fact]
        public void ReturnsFalseWhenAlreadyStood()
        {
            _gameMock.Setup(g => g.Money).Returns(10000);
            _gameMock.Setup(g => g.AllBets()).Returns(1000);
            _playerHand.Bet = 500;

            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(2, 0))
                .Returns(new Card(2, 1));
            _playerHand.DealCards(2);

            _gameMock.Setup(g => g.MoreHandsToPlay()).Returns(false);
            _gameMock.Setup(g => g.PlayDealerHand());
            _gameMock.Setup(g => g.DrawHands());
            _gameMock.Setup(g => g.BetOptions());

            _playerHand.Stand();

            Assert.False(_playerHand.CanSplit());
        }

        [Fact]
        public void ReturnsTrueWhenAllConditionsMet()
        {
            _gameMock.Setup(g => g.Money).Returns(10000);
            _gameMock.Setup(g => g.AllBets()).Returns(1000);
            _playerHand.Bet = 500;

            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(2, 0))
                .Returns(new Card(2, 1));
            _playerHand.DealCards(2);

            Assert.True(_playerHand.CanSplit());
        }

        [Fact]
        public void ReturnsFalseWhenTooManyHands()
        {
            _gameMock.Setup(g => g.Money).Returns(10000);
            _gameMock.Setup(g => g.AllBets()).Returns(1000);
            _playerHand.Bet = 500;

            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(2, 0))
                .Returns(new Card(2, 1));
            _playerHand.DealCards(2);

            for (var i = 0; i < Game.MaxPlayerHands; i++)
            {
                _game.PlayerHands.Add(new PlayerHand(_game));
            }

            Assert.False(_playerHand.CanSplit());
        }
    }

    public class IsDoneTests : PlayerHandTests
    {
        public IsDoneTests()
        {
            _game.Money = 10000;
        }

        [Fact]
        public void ReturnsTrueWhenStood()
        {
            _playerHand.Stood = true;
            Assert.True(_playerHand.IsDone());
        }

        [Fact]
        public void ReturnsTrueWhenBlackjack()
        {
            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(0, 0))
                .Returns(new Card(9, 0));
            _playerHand.DealCards(2);

            Assert.True(_playerHand.IsDone());
        }

        [Fact]
        public void ReturnsTrueWhen21Soft()
        {
            _handMock.Setup(p => p.GetValue(CountMethod.Soft)).Returns(21);
            Assert.True(_playerHand.IsDone());
        }

        [Fact]
        public void ReturnsTrueWhen21Hard()
        {
            _handMock.Setup(p => p.GetValue(CountMethod.Hard)).Returns(21);
            Assert.True(_playerHand.IsDone());
        }

        [Fact]
        public void ReturnsTrueWhenBusted()
        {
            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(8, 0))
                .Returns(new Card(8, 1))
                .Returns(new Card(8, 2));
            _playerHand.DealCards(3);

            Assert.True(_playerHand.IsDone());
            Assert.Equal(9500, _game.Money);
        }

        [Fact]
        public void ReturnsFalseWhenNotPlayed()
        {
            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(4, 0))
                .Returns(new Card(5, 1));
            _playerHand.DealCards(2);

            Assert.False(_playerHand.IsDone());
            Assert.Equal(10000, _game.Money);
        }

        [Fact]
        public void SkipsBustedPlayedHand()
        {
            _shoeMock.SetupSequence(s => s.GetNextCard())
                .Returns(new Card(8, 0))
                .Returns(new Card(8, 1))
                .Returns(new Card(8, 2));
            _playerHand.DealCards(3);

            _playerHand.Paid = true;

            Assert.True(_playerHand.IsDone());
            Assert.Equal(10000, _game.Money);
        }
    }
}

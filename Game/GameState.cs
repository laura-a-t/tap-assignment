using TapAssignment.Domain;

namespace TapAssignment.Game;

public sealed class GameState
{
    public const int SmallBlind = 1;
    public const int BigBlind = 2;
    public const int StartingStack = 100;

    private readonly Dictionary<PlayerId, int> _stacks;
    private readonly Dictionary<PlayerId, List<Card>> _holeCards;
    private readonly Dictionary<PlayerId, int> _roundContribution;
    private readonly Dictionary<PlayerId, bool> _hasActedThisRound;
    private readonly List<Card> _communityCards;
    private readonly List<PlayerAction> _actions;
    private readonly Deck _deck;

    public GameState()
    {
        _deck = new Deck();
        _stacks = new Dictionary<PlayerId, int>
        {
            [PlayerId.Hero] = StartingStack,
            [PlayerId.Villain] = StartingStack
        };
        _holeCards = new Dictionary<PlayerId, List<Card>>
        {
            [PlayerId.Hero] = [],
            [PlayerId.Villain] = []
        };
        _roundContribution = new Dictionary<PlayerId, int>
        {
            [PlayerId.Hero] = 0,
            [PlayerId.Villain] = 0
        };
        _hasActedThisRound = new Dictionary<PlayerId, bool>
        {
            [PlayerId.Hero] = false,
            [PlayerId.Villain] = false
        };
        _communityCards = [];
        _actions = [];

        Street = Street.Preflop;
        ActorToAct = PlayerId.Hero;
        StartHand();
    }

    public Street Street { get; private set; }
    public PlayerId ActorToAct { get; private set; }
    public int Pot { get; private set; }
    public int CurrentBet { get; private set; }
    public bool IsHandOver { get; private set; }
    public bool IsShowdown => Street == Street.Showdown;
    public bool IsTie { get; private set; }
    public PlayerId? Winner { get; private set; }
    public HandCombination? HeroCombination { get; private set; }
    public HandCombination? VillainCombination { get; private set; }
    public IReadOnlyList<Card> CommunityCards => _communityCards;
    public IReadOnlyList<PlayerAction> Actions => _actions;

    public IReadOnlyList<Card> GetHoleCards(PlayerId player)
    {
        return _holeCards[player].ToList();
    }

    public int GetStack(PlayerId player)
    {
        return _stacks[player];
    }

    public int GetAmountToCall(PlayerId player)
    {
        return Math.Max(0, CurrentBet - _roundContribution[player]);
    }

    public IReadOnlyList<ActionType> GetLegalActions(PlayerId player)
    {
        if (IsHandOver || player != ActorToAct)
        {
            return [];
        }

        var legal = new List<ActionType>();
        var amountToCall = GetAmountToCall(player);

        if (amountToCall > 0)
        {
            legal.Add(ActionType.Fold);

            if (_stacks[player] > 0)
            {
                legal.Add(ActionType.Call);
            }
        }
        else
        {
            legal.Add(ActionType.Check);
        }

        if (CanRaise(player))
        {
            legal.Add(ActionType.Raise);
        }

        return legal;
    }

    public int GetFixedRaiseToAmount()
    {
        return CurrentBet == 0 ? BigBlind : CurrentBet * 2;
    }

    public PlayerAction ApplyAction(PlayerId player, ActionType actionType)
    {
        if (IsHandOver)
        {
            throw new InvalidOperationException("The hand has already ended.");
        }

        if (player != ActorToAct)
        {
            throw new InvalidOperationException("It is not this player's turn.");
        }

        var legalActions = GetLegalActions(player);
        if (!legalActions.Contains(actionType))
        {
            throw new InvalidOperationException($"Action '{actionType}' is not legal right now.");
        }

        var action = actionType switch
        {
            ActionType.Fold => HandleFold(player),
            ActionType.Check => HandleCheck(player),
            ActionType.Call => HandleCall(player),
            ActionType.Raise => HandleRaise(player),
            _ => throw new ArgumentOutOfRangeException(nameof(actionType), actionType, "Unsupported action.")
        };

        _actions.Add(action);

        if (IsHandOver)
        {
            return action;
        }

        if (ShouldRunOutBoard())
        {
            RunOutBoardAndResolveShowdown();
            return action;
        }

        if (IsBettingRoundComplete())
        {
            AdvanceStreetOrResolveShowdown();
            return action;
        }

        ActorToAct = OtherPlayer(player);
        return action;
    }

    public GameStateSnapshot CreateSnapshotForHero()
    {
        return new GameStateSnapshot(
            _holeCards[PlayerId.Hero].ToList(),
            _communityCards.ToList(),
            Street,
            _stacks[PlayerId.Hero],
            _stacks[PlayerId.Villain],
            Pot,
            CurrentBet,
            GetAmountToCall(PlayerId.Hero),
            GetLegalActions(PlayerId.Hero).ToList(),
            GetFixedRaiseToAmount());
    }

    private void StartHand()
    {
        DealHoleCards();
        PostBlind(PlayerId.Hero, SmallBlind);
        PostBlind(PlayerId.Villain, BigBlind);
        CurrentBet = BigBlind;
        ActorToAct = PlayerId.Hero;
        Street = Street.Preflop;
    }

    private void DealHoleCards()
    {
        _holeCards[PlayerId.Hero].Add(_deck.Draw());
        _holeCards[PlayerId.Villain].Add(_deck.Draw());
        _holeCards[PlayerId.Hero].Add(_deck.Draw());
        _holeCards[PlayerId.Villain].Add(_deck.Draw());
    }

    private void PostBlind(PlayerId player, int blindAmount)
    {
        CommitChips(player, blindAmount);
    }

    private PlayerAction HandleFold(PlayerId player)
    {
        var winner = OtherPlayer(player);
        _hasActedThisRound[player] = true;
        IsHandOver = true;
        Winner = winner;
        _stacks[winner] += Pot;
        Pot = 0;
        return new PlayerAction(player, ActionType.Fold, 0, Street);
    }

    private PlayerAction HandleCheck(PlayerId player)
    {
        _hasActedThisRound[player] = true;
        return new PlayerAction(player, ActionType.Check, 0, Street);
    }

    private PlayerAction HandleCall(PlayerId player)
    {
        var amountToCall = GetAmountToCall(player);
        var amount = Math.Min(amountToCall, _stacks[player]);
        CommitChips(player, amount);
        _hasActedThisRound[player] = true;
        return new PlayerAction(player, ActionType.Call, amount, Street);
    }

    private PlayerAction HandleRaise(PlayerId player)
    {
        var raiseTo = GetFixedRaiseToAmount();
        var amount = raiseTo - _roundContribution[player];
        CommitChips(player, amount);
        CurrentBet = raiseTo;
        _hasActedThisRound[player] = true;
        _hasActedThisRound[OtherPlayer(player)] = false;
        return new PlayerAction(player, ActionType.Raise, raiseTo, Street);
    }

    private void CommitChips(PlayerId player, int amount)
    {
        if (amount < 0)
        {
            throw new InvalidOperationException("Chip amount cannot be negative.");
        }

        if (amount > _stacks[player])
        {
            throw new InvalidOperationException("Player does not have enough chips.");
        }

        _stacks[player] -= amount;
        _roundContribution[player] += amount;
        Pot += amount;
    }

    private bool CanRaise(PlayerId player)
    {
        var raiseTo = GetFixedRaiseToAmount();
        var amountRequired = raiseTo - _roundContribution[player];
        return amountRequired > 0 && _stacks[player] >= amountRequired;
    }

    private bool IsBettingRoundComplete()
    {
        if (!_hasActedThisRound[PlayerId.Hero] || !_hasActedThisRound[PlayerId.Villain])
        {
            return false;
        }

        return _roundContribution[PlayerId.Hero] == _roundContribution[PlayerId.Villain];
    }

    private bool ShouldRunOutBoard()
    {
        var onePlayerIsAllIn = _stacks[PlayerId.Hero] == 0 || _stacks[PlayerId.Villain] == 0;
        if (!onePlayerIsAllIn)
        {
            return false;
        }

        return GetAmountToCall(PlayerId.Hero) == 0 && GetAmountToCall(PlayerId.Villain) == 0;
    }

    private void AdvanceStreetOrResolveShowdown()
    {
        if (Street == Street.River)
        {
            ResolveShowdown();
            return;
        }

        if (Street == Street.Preflop)
        {
            Street = Street.Flop;
            _communityCards.Add(_deck.Draw());
            _communityCards.Add(_deck.Draw());
            _communityCards.Add(_deck.Draw());
        }
        else if (Street == Street.Flop)
        {
            Street = Street.Turn;
            _communityCards.Add(_deck.Draw());
        }
        else if (Street == Street.Turn)
        {
            Street = Street.River;
            _communityCards.Add(_deck.Draw());
        }

        StartBettingRoundForCurrentStreet();

        if (ShouldRunOutBoard())
        {
            RunOutBoardAndResolveShowdown();
        }
    }

    private void StartBettingRoundForCurrentStreet()
    {
        _roundContribution[PlayerId.Hero] = 0;
        _roundContribution[PlayerId.Villain] = 0;
        _hasActedThisRound[PlayerId.Hero] = false;
        _hasActedThisRound[PlayerId.Villain] = false;
        CurrentBet = 0;
        ActorToAct = FirstActorForStreet(Street);
    }

    private void RunOutBoardAndResolveShowdown()
    {
        while (_communityCards.Count < 5)
        {
            _communityCards.Add(_deck.Draw());
            Street = _communityCards.Count switch
            {
                3 => Street.Flop,
                4 => Street.Turn,
                5 => Street.River,
                _ => Street
            };
        }

        ResolveShowdown();
    }

    private void ResolveShowdown()
    {
        HeroCombination = HandEvaluator.EvaluateBest(_holeCards[PlayerId.Hero], _communityCards);
        VillainCombination = HandEvaluator.EvaluateBest(_holeCards[PlayerId.Villain], _communityCards);
        var comparison = HeroCombination.CompareTo(VillainCombination);

        IsHandOver = true;
        Street = Street.Showdown;

        if (comparison > 0)
        {
            Winner = PlayerId.Hero;
            _stacks[PlayerId.Hero] += Pot;
        }
        else if (comparison < 0)
        {
            Winner = PlayerId.Villain;
            _stacks[PlayerId.Villain] += Pot;
        }
        else
        {
            IsTie = true;
            Winner = null;
            var split = Pot / 2;
            var remainder = Pot % 2;
            _stacks[PlayerId.Hero] += split + remainder;
            _stacks[PlayerId.Villain] += split;
        }

        Pot = 0;
    }

    private static PlayerId FirstActorForStreet(Street street)
    {
        return street == Street.Preflop ? PlayerId.Hero : PlayerId.Villain;
    }

    private static PlayerId OtherPlayer(PlayerId player)
    {
        return player == PlayerId.Hero ? PlayerId.Villain : PlayerId.Hero;
    }
}

public sealed record GameStateSnapshot(
    IReadOnlyList<Card> HeroHoleCards,
    IReadOnlyList<Card> CommunityCards,
    Street Street,
    int HeroStack,
    int VillainStack,
    int PotSize,
    int CurrentBet,
    int AmountToCall,
    IReadOnlyList<ActionType> LegalActions,
    int FixedRaiseToAmount);

public sealed class Deck
{
    private readonly List<Card> _cards;
    private int _nextCardIndex;

    public Deck()
    {
        _cards = BuildStandardDeck();
        Shuffle(_cards);
        _nextCardIndex = 0;
    }

    public Card Draw()
    {
        if (_nextCardIndex >= _cards.Count)
        {
            throw new InvalidOperationException("Cannot draw from an empty deck.");
        }

        var card = _cards[_nextCardIndex];
        _nextCardIndex++;
        return card;
    }

    private static List<Card> BuildStandardDeck()
    {
        var cards = new List<Card>(52);
        foreach (Suit suit in Enum.GetValues<Suit>())
        {
            foreach (Rank rank in Enum.GetValues<Rank>())
            {
                cards.Add(new Card(rank, suit));
            }
        }

        return cards;
    }

    private static void Shuffle(List<Card> cards)
    {
        for (var i = cards.Count - 1; i > 0; i--)
        {
            var randomIndex = Random.Shared.Next(i + 1);
            (cards[i], cards[randomIndex]) = (cards[randomIndex], cards[i]);
        }
    }
}

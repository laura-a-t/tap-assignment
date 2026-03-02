using TapAssignment.Domain;
using TapAssignment.Engine;
using TapAssignment.Game;

namespace TapAssignment.Tests;

public sealed class DecisionEngineTests
{
    [Fact]
    public void GetDecision_ThrowsWhenNoLegalActions()
    {
        var engine = new DecisionEngine();
        var state = BuildPreflopSnapshot(
            [new Card(Rank.Ace, Suit.Spades), new Card(Rank.King, Suit.Spades)],
            amountToCall: 0,
            legalActions: []);

        Assert.Throws<InvalidOperationException>(() => engine.GetDecision(state));
    }

    [Fact]
    public void GetDecision_FreeActionWithStrongHand_RaisesWhenLegal()
    {
        var engine = new DecisionEngine();
        var state = BuildPreflopSnapshot(
            [new Card(Rank.Ace, Suit.Spades), new Card(Rank.King, Suit.Spades)],
            amountToCall: 0,
            legalActions: [ActionType.Check, ActionType.Raise],
            fixedRaiseToAmount: 2,
            currentBet: 0);

        var decision = engine.GetDecision(state);

        Assert.Equal(ActionType.Raise, decision.ActionType);
        Assert.Equal(2, decision.RaiseToAmount);
    }

    [Fact]
    public void GetDecision_FreeActionWithWeakHand_Checks()
    {
        var engine = new DecisionEngine();
        var state = BuildPreflopSnapshot(
            [new Card(Rank.Seven, Suit.Clubs), new Card(Rank.Two, Suit.Diamonds)],
            amountToCall: 0,
            legalActions: [ActionType.Check, ActionType.Raise],
            fixedRaiseToAmount: 2,
            currentBet: 0);

        var decision = engine.GetDecision(state);

        Assert.Equal(ActionType.Check, decision.ActionType);
        Assert.Null(decision.RaiseToAmount);
    }

    [Fact]
    public void GetDecision_PreflopSmallBlindFacingOneChip_Calls()
    {
        var engine = new DecisionEngine();
        var state = BuildPreflopSnapshot(
            [new Card(Rank.Seven, Suit.Clubs), new Card(Rank.Two, Suit.Diamonds)],
            amountToCall: GameState.SmallBlind,
            legalActions: [ActionType.Fold, ActionType.Call, ActionType.Raise]);

        var decision = engine.GetDecision(state);

        Assert.Equal(ActionType.Call, decision.ActionType);
        Assert.Null(decision.RaiseToAmount);
    }

    [Fact]
    public void GetDecision_WeakHandFacingBet_FoldsWhenLegal()
    {
        var engine = new DecisionEngine();
        var state = BuildPreflopSnapshot(
            [new Card(Rank.Seven, Suit.Clubs), new Card(Rank.Two, Suit.Diamonds)],
            amountToCall: 2,
            legalActions: [ActionType.Fold, ActionType.Call]);

        var decision = engine.GetDecision(state);

        Assert.Equal(ActionType.Fold, decision.ActionType);
    }

    [Fact]
    public void GetDecision_StrongHandFacingBet_RaisesWhenLegal()
    {
        var engine = new DecisionEngine();
        var state = BuildPreflopSnapshot(
            [new Card(Rank.Ace, Suit.Spades), new Card(Rank.King, Suit.Spades)],
            amountToCall: 2,
            legalActions: [ActionType.Fold, ActionType.Call, ActionType.Raise],
            fixedRaiseToAmount: 4);

        var decision = engine.GetDecision(state);

        Assert.Equal(ActionType.Raise, decision.ActionType);
        Assert.Equal(4, decision.RaiseToAmount);
    }

    [Fact]
    public void GetDecision_StrongHandWithoutRaiseOption_Calls()
    {
        var engine = new DecisionEngine();
        var state = BuildPreflopSnapshot(
            [new Card(Rank.Ace, Suit.Hearts), new Card(Rank.Queen, Suit.Clubs)],
            amountToCall: 2,
            legalActions: [ActionType.Fold, ActionType.Call]);

        var decision = engine.GetDecision(state);

        Assert.Equal(ActionType.Call, decision.ActionType);
    }

    [Fact]
    public void GetDecision_FreeActionWithoutCheck_FallsBackToFirstLegalAction()
    {
        var engine = new DecisionEngine();
        var state = BuildPreflopSnapshot(
            [new Card(Rank.Seven, Suit.Clubs), new Card(Rank.Two, Suit.Diamonds)],
            amountToCall: 0,
            legalActions: [ActionType.Raise],
            fixedRaiseToAmount: 2,
            currentBet: 0);

        var decision = engine.GetDecision(state);

        Assert.Equal(ActionType.Raise, decision.ActionType);
        Assert.Equal(2, decision.RaiseToAmount);
    }

    [Fact]
    public void ObserveAction_AppendsActionsInOrder()
    {
        var engine = new DecisionEngine();
        var first = new PlayerAction(PlayerId.Hero, ActionType.Call, 1, Street.Preflop);
        var second = new PlayerAction(PlayerId.Villain, ActionType.Raise, 4, Street.Preflop);

        engine.ObserveAction(first);
        engine.ObserveAction(second);

        Assert.Equal(2, engine.ActionHistory.Count);
        Assert.Equal(first, engine.ActionHistory[0]);
        Assert.Equal(second, engine.ActionHistory[1]);
    }

    private static GameStateSnapshot BuildPreflopSnapshot(
        IReadOnlyList<Card> heroHoleCards,
        int amountToCall,
        IReadOnlyList<ActionType> legalActions,
        int fixedRaiseToAmount = 4,
        int currentBet = 2)
    {
        return new GameStateSnapshot(
            HeroHoleCards: heroHoleCards,
            CommunityCards: [],
            Street: Street.Preflop,
            HeroStack: 100,
            VillainStack: 100,
            PotSize: 3,
            CurrentBet: currentBet,
            AmountToCall: amountToCall,
            LegalActions: legalActions,
            FixedRaiseToAmount: fixedRaiseToAmount);
    }
}

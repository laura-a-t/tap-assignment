using TapAssignment.Domain;
using TapAssignment.Game;

namespace TapAssignment.Tests;

public sealed class GameStateActionTests
{
    [Fact]
    public void InitialState_HeroHasFoldCallRaise()
    {
        var state = new GameState();

        var legalActions = state.GetLegalActions(PlayerId.Hero);

        Assert.Equal(
            [ActionType.Fold, ActionType.Call, ActionType.Raise],
            legalActions);
    }

    [Fact]
    public void InitialState_VillainHasNoLegalActions_WhenNotActor()
    {
        var state = new GameState();

        var legalActions = state.GetLegalActions(PlayerId.Villain);

        Assert.Empty(legalActions);
    }

    [Fact]
    public void GetLegalActions_AfterHandEnds_ReturnsEmptyForBothPlayers()
    {
        var state = new GameState();
        state.ApplyAction(PlayerId.Hero, ActionType.Fold);

        Assert.Empty(state.GetLegalActions(PlayerId.Hero));
        Assert.Empty(state.GetLegalActions(PlayerId.Villain));
    }

    [Fact]
    public void ApplyAction_ThrowsWhenWrongPlayerActs()
    {
        var state = new GameState();

        Assert.Throws<InvalidOperationException>(() => state.ApplyAction(PlayerId.Villain, ActionType.Call));
    }

    [Fact]
    public void ApplyAction_ThrowsWhenHandAlreadyEnded()
    {
        var state = new GameState();
        state.ApplyAction(PlayerId.Hero, ActionType.Fold);

        Assert.Throws<InvalidOperationException>(() => state.ApplyAction(PlayerId.Villain, ActionType.Check));
    }

    [Fact]
    public void Call_ActionUpdatesPotAndTurn()
    {
        var state = new GameState();

        var action = state.ApplyAction(PlayerId.Hero, ActionType.Call);

        Assert.Equal(ActionType.Call, action.ActionType);
        Assert.Equal(1, action.Amount);
        Assert.Equal(4, state.Pot);
        Assert.Equal(98, state.GetStack(PlayerId.Hero));
        Assert.Equal(PlayerId.Villain, state.ActorToAct);
        Assert.False(state.IsHandOver);
    }

    [Fact]
    public void Check_ActionIsLegalWhenAmountToCallIsZero()
    {
        var state = new GameState();
        state.ApplyAction(PlayerId.Hero, ActionType.Call);

        var legalActions = state.GetLegalActions(PlayerId.Villain);
        var action = state.ApplyAction(PlayerId.Villain, ActionType.Check);

        Assert.Equal([ActionType.Check, ActionType.Raise], legalActions);
        Assert.Equal(ActionType.Check, action.ActionType);
        Assert.Equal(Street.Flop, state.Street);
        Assert.Equal(3, state.CommunityCards.Count);
        Assert.Equal(0, state.CurrentBet);
        Assert.Equal(PlayerId.Villain, state.ActorToAct);
    }

    [Fact]
    public void CheckCheckAcrossStreets_AdvancesToShowdownWithExpectedTurnOrder()
    {
        var state = new GameState();

        state.ApplyAction(PlayerId.Hero, ActionType.Call);
        state.ApplyAction(PlayerId.Villain, ActionType.Check);
        Assert.Equal(Street.Flop, state.Street);
        Assert.Equal(PlayerId.Villain, state.ActorToAct);

        state.ApplyAction(PlayerId.Villain, ActionType.Check);
        Assert.Equal(PlayerId.Hero, state.ActorToAct);
        state.ApplyAction(PlayerId.Hero, ActionType.Check);
        Assert.Equal(Street.Turn, state.Street);
        Assert.Equal(PlayerId.Villain, state.ActorToAct);

        state.ApplyAction(PlayerId.Villain, ActionType.Check);
        state.ApplyAction(PlayerId.Hero, ActionType.Check);
        Assert.Equal(Street.River, state.Street);
        Assert.Equal(PlayerId.Villain, state.ActorToAct);

        state.ApplyAction(PlayerId.Villain, ActionType.Check);
        state.ApplyAction(PlayerId.Hero, ActionType.Check);
        Assert.True(state.IsHandOver);
        Assert.True(state.IsShowdown);
        Assert.Equal(Street.Showdown, state.Street);
        Assert.Equal(5, state.CommunityCards.Count);
    }

    [Fact]
    public void Raise_ActionUsesFixedRaiseRule()
    {
        var state = new GameState();

        var action = state.ApplyAction(PlayerId.Hero, ActionType.Raise);

        Assert.Equal(ActionType.Raise, action.ActionType);
        Assert.Equal(4, action.Amount);
        Assert.Equal(4, state.CurrentBet);
        Assert.Equal(6, state.Pot);
        Assert.Equal(96, state.GetStack(PlayerId.Hero));
        Assert.Equal(PlayerId.Villain, state.ActorToAct);
    }

    [Fact]
    public void Raise_WhenCurrentBetIsZero_OpensToTwoChips()
    {
        var state = new GameState();
        state.ApplyAction(PlayerId.Hero, ActionType.Call);
        state.ApplyAction(PlayerId.Villain, ActionType.Check);

        var action = state.ApplyAction(PlayerId.Villain, ActionType.Raise);

        Assert.Equal(ActionType.Raise, action.ActionType);
        Assert.Equal(2, action.Amount);
        Assert.Equal(2, state.CurrentBet);
    }

    [Fact]
    public void Raise_IsNotLegal_WhenPlayerCannotAffordRaiseTo()
    {
        var state = CreateStateAtPreflopVillainFacingLargeBet();

        var legalActions = state.GetLegalActions(PlayerId.Villain);

        Assert.Equal([ActionType.Fold, ActionType.Call], legalActions);
        Assert.DoesNotContain(ActionType.Raise, legalActions);
    }

    [Fact]
    public void AllInLine_RunsOutBoardAndResolvesShowdown()
    {
        var state = CreateStateAtPreflopVillainFacingLargeBet();

        state.ApplyAction(PlayerId.Villain, ActionType.Call);
        state.ApplyAction(PlayerId.Villain, ActionType.Raise);
        state.ApplyAction(PlayerId.Hero, ActionType.Raise);
        state.ApplyAction(PlayerId.Villain, ActionType.Raise);
        state.ApplyAction(PlayerId.Hero, ActionType.Raise);
        state.ApplyAction(PlayerId.Villain, ActionType.Raise);
        state.ApplyAction(PlayerId.Hero, ActionType.Call);
        state.ApplyAction(PlayerId.Villain, ActionType.Raise);
        state.ApplyAction(PlayerId.Hero, ActionType.Raise);
        state.ApplyAction(PlayerId.Villain, ActionType.Call);

        Assert.True(state.IsHandOver);
        Assert.True(state.IsShowdown);
        Assert.Equal(Street.Showdown, state.Street);
        Assert.Equal(5, state.CommunityCards.Count);
        Assert.NotNull(state.HeroCombination);
        Assert.NotNull(state.VillainCombination);
        Assert.True(state.IsTie || state.Winner is PlayerId.Hero or PlayerId.Villain);
    }

    [Fact]
    public void Fold_ActionEndsHandAndAwardsPot()
    {
        var state = new GameState();

        var action = state.ApplyAction(PlayerId.Hero, ActionType.Fold);

        Assert.Equal(ActionType.Fold, action.ActionType);
        Assert.True(state.IsHandOver);
        Assert.Equal(PlayerId.Villain, state.Winner);
        Assert.Equal(0, state.Pot);
        Assert.Equal(101, state.GetStack(PlayerId.Villain));
    }

    [Fact]
    public void VillainFold_AwardsPotToHero()
    {
        var state = new GameState();
        state.ApplyAction(PlayerId.Hero, ActionType.Call);

        var action = state.ApplyAction(PlayerId.Villain, ActionType.Fold);

        Assert.Equal(ActionType.Fold, action.ActionType);
        Assert.True(state.IsHandOver);
        Assert.Equal(PlayerId.Hero, state.Winner);
        Assert.Equal(101, state.GetStack(PlayerId.Hero));
        Assert.Equal(99, state.GetStack(PlayerId.Villain));
        Assert.Equal(0, state.Pot);
    }

    [Fact]
    public void CreateSnapshotForHero_ReflectsCurrentState()
    {
        var state = new GameState();
        state.ApplyAction(PlayerId.Hero, ActionType.Call);
        state.ApplyAction(PlayerId.Villain, ActionType.Check);
        state.ApplyAction(PlayerId.Villain, ActionType.Check);

        var snapshot = state.CreateSnapshotForHero();

        Assert.Equal(Street.Flop, snapshot.Street);
        Assert.Equal(2, snapshot.HeroHoleCards.Count);
        Assert.Equal(3, snapshot.CommunityCards.Count);
        Assert.Equal(98, snapshot.HeroStack);
        Assert.Equal(98, snapshot.VillainStack);
        Assert.Equal(4, snapshot.PotSize);
        Assert.Equal(0, snapshot.CurrentBet);
        Assert.Equal(0, snapshot.AmountToCall);
        Assert.Equal([ActionType.Check, ActionType.Raise], snapshot.LegalActions);
        Assert.Equal(2, snapshot.FixedRaiseToAmount);
    }

    [Fact]
    public void ActionsHistory_TracksAppliedActionsWithStreetAndAmount()
    {
        var state = new GameState();
        state.ApplyAction(PlayerId.Hero, ActionType.Call);
        state.ApplyAction(PlayerId.Villain, ActionType.Check);
        state.ApplyAction(PlayerId.Villain, ActionType.Check);

        Assert.Equal(3, state.Actions.Count);
        Assert.Collection(
            state.Actions,
            action =>
            {
                Assert.Equal(PlayerId.Hero, action.PlayerId);
                Assert.Equal(ActionType.Call, action.ActionType);
                Assert.Equal(1, action.Amount);
                Assert.Equal(Street.Preflop, action.Street);
            },
            action =>
            {
                Assert.Equal(PlayerId.Villain, action.PlayerId);
                Assert.Equal(ActionType.Check, action.ActionType);
                Assert.Equal(0, action.Amount);
                Assert.Equal(Street.Preflop, action.Street);
            },
            action =>
            {
                Assert.Equal(PlayerId.Villain, action.PlayerId);
                Assert.Equal(ActionType.Check, action.ActionType);
                Assert.Equal(0, action.Amount);
                Assert.Equal(Street.Flop, action.Street);
            });
    }

    [Fact]
    public void IllegalAction_Throws()
    {
        var state = new GameState();

        Assert.Throws<InvalidOperationException>(() => state.ApplyAction(PlayerId.Hero, ActionType.Check));
    }

    private static GameState CreateStateAtPreflopVillainFacingLargeBet()
    {
        var state = new GameState();
        state.ApplyAction(PlayerId.Hero, ActionType.Raise);
        state.ApplyAction(PlayerId.Villain, ActionType.Raise);
        state.ApplyAction(PlayerId.Hero, ActionType.Raise);
        state.ApplyAction(PlayerId.Villain, ActionType.Raise);
        state.ApplyAction(PlayerId.Hero, ActionType.Raise);
        return state;
    }
}

using TapAssignment.Domain;
using TapAssignment.Game;

namespace TapAssignment.Engine;

public sealed class DecisionEngine
{
    private readonly List<PlayerAction> _actionHistory;

    public DecisionEngine()
    {
        _actionHistory = [];
    }

    public IReadOnlyList<PlayerAction> ActionHistory => _actionHistory;

    public DecisionResult GetDecision(GameStateSnapshot state)
    {
        var legalActions = state.LegalActions;
        if (legalActions.Count == 0)
        {
            throw new InvalidOperationException("No legal actions are available for the decision engine.");
        }

        var strength = EvaluateStrength(state);

        if (state.AmountToCall == 0)
        {
            if (strength >= HandStrength.Strong && legalActions.Contains(ActionType.Raise))
            {
                return DecisionResult.RaiseTo(state.FixedRaiseToAmount);
            }

            if (legalActions.Contains(ActionType.Check))
            {
                return DecisionResult.Of(ActionType.Check);
            }

            return DecisionResult.Of(legalActions[0]);
        }

        if (state.Street == Street.Preflop &&
            state.AmountToCall <= GameState.SmallBlind &&
            legalActions.Contains(ActionType.Call))
        {
            return DecisionResult.Of(ActionType.Call);
        }

        if (strength == HandStrength.Weak && legalActions.Contains(ActionType.Fold))
        {
            return DecisionResult.Of(ActionType.Fold);
        }

        if (strength >= HandStrength.Strong && legalActions.Contains(ActionType.Raise))
        {
            return DecisionResult.RaiseTo(state.FixedRaiseToAmount);
        }

        if (legalActions.Contains(ActionType.Call))
        {
            return DecisionResult.Of(ActionType.Call);
        }

        if (legalActions.Contains(ActionType.Fold))
        {
            return DecisionResult.Of(ActionType.Fold);
        }

        return DecisionResult.Of(legalActions[0]);
    }

    public void ObserveAction(PlayerAction action)
    {
        _actionHistory.Add(action);
    }

    private static HandStrength EvaluateStrength(GameStateSnapshot state)
    {
        return state.Street == Street.Preflop
            ? EvaluatePreflopStrength(state.HeroHoleCards)
            : EvaluatePostflopStrength(state.HeroHoleCards, state.CommunityCards);
    }

    private static HandStrength EvaluatePreflopStrength(IReadOnlyList<Card> holeCards)
    {
        var first = holeCards[0];
        var second = holeCards[1];
        var highRank = Math.Max((int)first.Rank, (int)second.Rank);
        var lowRank = Math.Min((int)first.Rank, (int)second.Rank);
        var isPair = first.Rank == second.Rank;
        var isSuited = first.Suit == second.Suit;
        var isBroadway = highRank >= 11 && lowRank >= 10;

        if (isPair && highRank >= 10)
        {
            return HandStrength.Strong;
        }

        if (isPair && highRank >= 7)
        {
            return HandStrength.Medium;
        }

        if (highRank == (int)Rank.Ace && lowRank >= 11)
        {
            return HandStrength.Strong;
        }

        if (isBroadway && isSuited)
        {
            return HandStrength.Medium;
        }

        if (highRank == (int)Rank.Ace && lowRank >= 8)
        {
            return HandStrength.Medium;
        }

        return HandStrength.Weak;
    }

    private static HandStrength EvaluatePostflopStrength(
        IReadOnlyList<Card> holeCards,
        IReadOnlyList<Card> communityCards)
    {
        var combination = HandEvaluator.EvaluateBest(holeCards, communityCards);

        if (combination.Category >= HandCategory.Straight)
        {
            return HandStrength.Strong;
        }

        if (combination.Category == HandCategory.ThreeOfAKind || combination.Category == HandCategory.TwoPair)
        {
            return HandStrength.Strong;
        }

        if (combination.Category == HandCategory.Pair)
        {
            var pairRank = combination.TiebreakValues[0];
            return pairRank >= (int)Rank.Jack ? HandStrength.Medium : HandStrength.Weak;
        }

        return HandStrength.Weak;
    }

    private enum HandStrength
    {
        Weak = 0,
        Medium = 1,
        Strong = 2
    }
}

public sealed class DecisionResult
{
    private DecisionResult(ActionType actionType, int? raiseToAmount)
    {
        if (actionType == ActionType.Raise && (!raiseToAmount.HasValue || raiseToAmount.Value <= 0))
        {
            throw new ArgumentException("Raise decisions require a positive raise-to amount.");
        }

        if (actionType != ActionType.Raise && raiseToAmount.HasValue)
        {
            throw new ArgumentException("Only raise decisions can include a raise amount.");
        }

        ActionType = actionType;
        RaiseToAmount = raiseToAmount;
    }

    public ActionType ActionType { get; }
    public int? RaiseToAmount { get; }

    public static DecisionResult Of(ActionType actionType)
    {
        return new DecisionResult(actionType, null);
    }

    public static DecisionResult RaiseTo(int raiseToAmount)
    {
        return new DecisionResult(ActionType.Raise, raiseToAmount);
    }
}

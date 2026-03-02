using TapAssignment.Domain;
using TapAssignment.Engine;
using TapAssignment.Game;

namespace TapAssignment.Cli;

public sealed class ConsoleGame
{
    private readonly DecisionEngine _decisionEngine;
    private readonly GameState _gameState;

    public ConsoleGame()
    {
        _gameState = new GameState();
        _decisionEngine = new DecisionEngine();
    }

    public void Run()
    {
        Console.WriteLine("Texas Hold'em Decision Engine");
        Console.WriteLine("One hand per run. Hero is AI, Villain is command-line player.");
        Console.WriteLine();

        while (!_gameState.IsHandOver)
        {
            if (_gameState.ActorToAct == PlayerId.Hero)
            {
                HandleHeroTurn();
            }
            else
            {
                HandleVillainTurn();
            }

            if (!_gameState.IsHandOver)
            {
                PrintState();
            }
        }

        PrintResult();
    }

    private void HandleHeroTurn()
    {
        var snapshot = _gameState.CreateSnapshotForHero();
        var decision = _decisionEngine.GetDecision(snapshot);
        var appliedAction = _gameState.ApplyAction(PlayerId.Hero, decision.ActionType);
        _decisionEngine.ObserveAction(appliedAction);

        Console.WriteLine();
        Console.WriteLine(
            decision.ActionType == ActionType.Raise
                ? $"Hero action: RAISE to {decision.RaiseToAmount}"
                : $"Hero action: {decision.ActionType.ToString().ToUpperInvariant()}");
        Console.WriteLine();
    }

    private void HandleVillainTurn()
    {
        while (true)
        {
            var legalActions = _gameState.GetLegalActions(PlayerId.Villain);
            Console.Write($"Villain action [{ToCommandHelp(legalActions)}]: ");
            var input = Console.ReadLine();

            if (!CommandParser.TryParseAction(input, out var actionType))
            {
                PrintHelpText();
                continue;
            }

            try
            {
                var appliedAction = _gameState.ApplyAction(PlayerId.Villain, actionType);
                _decisionEngine.ObserveAction(appliedAction);
                Console.WriteLine(
                    actionType == ActionType.Raise
                        ? $"Villain action: RAISE to {appliedAction.Amount}"
                        : $"Villain action: {actionType.ToString().ToUpperInvariant()}");
                return;
            }
            catch (InvalidOperationException error)
            {
                Console.WriteLine(error.Message);
                PrintHelpText();
            }
        }
    }

    private void PrintState()
    {
        Console.WriteLine();
        Console.WriteLine($"Street: {_gameState.Street}");
        Console.WriteLine($"Community Cards: {FormatCards(_gameState.CommunityCards)}");
        Console.WriteLine($"Villain cards: {FormatCards(_gameState.GetHoleCards(PlayerId.Villain))}");
        Console.WriteLine(
            $"Stacks - Hero: {_gameState.GetStack(PlayerId.Hero)}, Villain: {_gameState.GetStack(PlayerId.Villain)}");
        Console.WriteLine($"Pot: {_gameState.Pot}");
        Console.WriteLine($"Current bet: {_gameState.CurrentBet}");
        Console.WriteLine(
            $"Actor: {_gameState.ActorToAct}, To call: {_gameState.GetAmountToCall(_gameState.ActorToAct)}");
    }

    private void PrintResult()
    {
        Console.WriteLine();
        Console.WriteLine("Hand finished.");

        if (_gameState.IsShowdown)
        {
            Console.WriteLine($"Community Cards: {FormatCards(_gameState.CommunityCards)}");
            Console.WriteLine($"Hero cards: {FormatCards(_gameState.GetHoleCards(PlayerId.Hero))}");
            Console.WriteLine($"Villain cards: {FormatCards(_gameState.GetHoleCards(PlayerId.Villain))}");
            Console.WriteLine(
                $"Hero combination: {_gameState.HeroCombination!.CategoryName} ({FormatCards(_gameState.HeroCombination.Cards)})");
            Console.WriteLine(
                $"Villain combination: {_gameState.VillainCombination!.CategoryName} ({FormatCards(_gameState.VillainCombination.Cards)})");

            if (_gameState.IsTie)
            {
                Console.WriteLine("Result: Tie.");
            }
            else
            {
                var winnerCombination = _gameState.Winner == PlayerId.Hero
                    ? _gameState.HeroCombination
                    : _gameState.VillainCombination;
                Console.WriteLine(
                    $"Winner: {_gameState.Winner} with {winnerCombination!.CategoryName} ({FormatCards(winnerCombination.Cards)})");
            }
        }
        else
        {
            Console.WriteLine($"Winner: {_gameState.Winner} by fold.");
            Console.WriteLine("Winning combination: no showdown (hand ended by fold).");
        }

        Console.WriteLine($"Final stacks - Hero: {_gameState.GetStack(PlayerId.Hero)}, Villain: {_gameState.GetStack(PlayerId.Villain)}");
        Console.WriteLine();
        Console.WriteLine("Action history:");
        foreach (var action in _gameState.Actions)
        {
            Console.WriteLine($"- {action.Street}: {action.PlayerId} {action.ActionType} {action.Amount}");
        }
    }

    private static string FormatCards(IReadOnlyList<Card> cards)
    {
        return cards.Count == 0 ? "(none yet)" : string.Join(" ", cards);
    }

    private static string ToCommandHelp(IReadOnlyList<ActionType> legalActions)
    {
        return string.Join("/", legalActions.Select(action => action.ToString().ToLowerInvariant()));
    }

    private static void PrintHelpText()
    {
        foreach (var line in CommandParser.HelpLines)
        {
            Console.WriteLine(line);
        }
    }
}

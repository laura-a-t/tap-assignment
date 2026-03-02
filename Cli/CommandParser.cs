using TapAssignment.Domain;

namespace TapAssignment.Cli;

public static class CommandParser
{
    public static bool TryParseAction(string? input, out ActionType actionType)
    {
        actionType = default;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var command = input.Trim().ToLowerInvariant();
        switch (command)
        {
            case "fold":
            case "f":
                actionType = ActionType.Fold;
                return true;
            case "check":
            case "k":
                actionType = ActionType.Check;
                return true;
            case "call":
            case "c":
                actionType = ActionType.Call;
                return true;
            case "raise":
            case "r":
                actionType = ActionType.Raise;
                return true;
            default:
                return false;
        }
    }

    public static IReadOnlyList<string> HelpLines =>
    [
        "Commands: fold | check | call | raise",
        "Raise size is fixed: raise-to 2x current bet, or 2 when current bet is 0."
    ];
}

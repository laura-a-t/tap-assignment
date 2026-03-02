using TapAssignment.Cli;
using TapAssignment.Domain;

namespace TapAssignment.Tests;

public sealed class CommandParserTests
{
    [Theory]
    [InlineData("fold", ActionType.Fold)]
    [InlineData("f", ActionType.Fold)]
    [InlineData("check", ActionType.Check)]
    [InlineData("k", ActionType.Check)]
    [InlineData("call", ActionType.Call)]
    [InlineData("c", ActionType.Call)]
    [InlineData("raise", ActionType.Raise)]
    [InlineData("r", ActionType.Raise)]
    [InlineData("FOLD", ActionType.Fold)]
    [InlineData("RaIsE", ActionType.Raise)]
    [InlineData("\tcall\n", ActionType.Call)]
    [InlineData("  raise  ", ActionType.Raise)]
    public void TryParseAction_ValidInput_ReturnsTrue(string input, ActionType expectedAction)
    {
        var parsed = CommandParser.TryParseAction(input, out var actionType);

        Assert.True(parsed);
        Assert.Equal(expectedAction, actionType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("unknown")]
    [InlineData("raise 10")]
    [InlineData("check-now")]
    [InlineData("k k")]
    [InlineData("c/r")]
    public void TryParseAction_InvalidInput_ReturnsFalse(string input)
    {
        var parsed = CommandParser.TryParseAction(input, out _);

        Assert.False(parsed);
    }

    [Fact]
    public void TryParseAction_NullInput_ReturnsFalse()
    {
        var parsed = CommandParser.TryParseAction(null, out _);

        Assert.False(parsed);
    }
}

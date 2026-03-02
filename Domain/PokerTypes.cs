namespace TapAssignment.Domain;

public enum ActionType
{
    Fold,
    Check,
    Call,
    Raise
}

public enum Street
{
    Preflop,
    Flop,
    Turn,
    River,
    Showdown
}

public enum Suit
{
    Clubs,
    Diamonds,
    Hearts,
    Spades
}

public enum Rank
{
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13,
    Ace = 14
}

public enum HandCategory
{
    HighCard = 1,
    Pair = 2,
    TwoPair = 3,
    ThreeOfAKind = 4,
    Straight = 5,
    Flush = 6,
    FullHouse = 7,
    FourOfAKind = 8,
    StraightFlush = 9
}

public enum PlayerId
{
    Hero,
    Villain
}

public readonly record struct Card(Rank Rank, Suit Suit)
{
    public override string ToString()
    {
        return $"{RankToText(Rank)}{SuitToText(Suit)}";
    }

    private static string RankToText(Rank rank)
    {
        return rank switch
        {
            Rank.Two => "2",
            Rank.Three => "3",
            Rank.Four => "4",
            Rank.Five => "5",
            Rank.Six => "6",
            Rank.Seven => "7",
            Rank.Eight => "8",
            Rank.Nine => "9",
            Rank.Ten => "10",
            Rank.Jack => "J",
            Rank.Queen => "Q",
            Rank.King => "K",
            Rank.Ace => "A",
            _ => ((int)rank).ToString()
        };
    }

    private static string SuitToText(Suit suit)
    {
        return suit switch
        {
            Suit.Clubs => "C",
            Suit.Diamonds => "D",
            Suit.Hearts => "H",
            Suit.Spades => "S",
            _ => "?"
        };
    }
}

public sealed record PlayerAction(
    PlayerId PlayerId,
    ActionType ActionType,
    int Amount,
    Street Street);

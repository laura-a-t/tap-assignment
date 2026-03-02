namespace TapAssignment.Domain;

public sealed class HandCombination : IComparable<HandCombination>
{
    public HandCombination(
        HandCategory category,
        IReadOnlyList<int> tiebreakValues,
        IReadOnlyList<Card> cards)
    {
        Category = category;
        TiebreakValues = tiebreakValues;
        Cards = cards;
    }

    public HandCategory Category { get; }
    public IReadOnlyList<int> TiebreakValues { get; }
    public IReadOnlyList<Card> Cards { get; }

    public int CompareTo(HandCombination? other)
    {
        if (other is null)
        {
            return 1;
        }

        var categoryComparison = Category.CompareTo(other.Category);
        if (categoryComparison != 0)
        {
            return categoryComparison;
        }

        var maxLength = Math.Max(TiebreakValues.Count, other.TiebreakValues.Count);
        for (var i = 0; i < maxLength; i++)
        {
            var left = i < TiebreakValues.Count ? TiebreakValues[i] : 0;
            var right = i < other.TiebreakValues.Count ? other.TiebreakValues[i] : 0;
            if (left != right)
            {
                return left.CompareTo(right);
            }
        }

        return 0;
    }

    public string CategoryName => Category switch
    {
        HandCategory.HighCard => "High Card",
        HandCategory.Pair => "Pair",
        HandCategory.TwoPair => "Two Pair",
        HandCategory.ThreeOfAKind => "Three of a Kind",
        HandCategory.Straight => "Straight",
        HandCategory.Flush => "Flush",
        HandCategory.FullHouse => "Full House",
        HandCategory.FourOfAKind => "Four of a Kind",
        HandCategory.StraightFlush => "Straight Flush",
        _ => Category.ToString()
    };
}

public static class HandEvaluator
{
    public static HandCombination EvaluateBest(IReadOnlyList<Card> holeCards, IReadOnlyList<Card> communityCards)
    {
        var allCards = holeCards.Concat(communityCards).ToList();
        if (allCards.Count < 5)
        {
            throw new InvalidOperationException("At least 5 cards are required to evaluate a hand.");
        }

        HandCombination? best = null;
        foreach (var fiveCards in SelectFiveCardCombinations(allCards))
        {
            var current = EvaluateFiveCards(fiveCards);
            if (best is null || current.CompareTo(best) > 0)
            {
                best = current;
            }
        }

        return best!;
    }

    private static IEnumerable<IReadOnlyList<Card>> SelectFiveCardCombinations(IReadOnlyList<Card> cards)
    {
        for (var i = 0; i < cards.Count - 4; i++)
        {
            for (var j = i + 1; j < cards.Count - 3; j++)
            {
                for (var k = j + 1; k < cards.Count - 2; k++)
                {
                    for (var l = k + 1; l < cards.Count - 1; l++)
                    {
                        for (var m = l + 1; m < cards.Count; m++)
                        {
                            yield return
                            [
                                cards[i],
                                cards[j],
                                cards[k],
                                cards[l],
                                cards[m]
                            ];
                        }
                    }
                }
            }
        }
    }

    private static HandCombination EvaluateFiveCards(IReadOnlyList<Card> cards)
    {
        var orderedByRank = cards.OrderByDescending(card => (int)card.Rank).ToList();
        var rankGroups = cards
            .GroupBy(card => card.Rank)
            .Select(group => new RankGroup(group.Key, group.Count()))
            .OrderByDescending(group => group.Count)
            .ThenByDescending(group => (int)group.Rank)
            .ToList();

        var isFlush = cards.Select(card => card.Suit).Distinct().Count() == 1;
        var isStraight = TryGetStraightHighRank(cards, out var straightHighRank);

        if (isFlush && isStraight)
        {
            return new HandCombination(
                HandCategory.StraightFlush,
                [straightHighRank],
                OrderStraightCards(cards, straightHighRank));
        }

        if (rankGroups[0].Count == 4)
        {
            var kicker = rankGroups[1].Rank;
            return new HandCombination(
                HandCategory.FourOfAKind,
                [(int)rankGroups[0].Rank, (int)kicker],
                OrderByPattern(cards, [rankGroups[0].Rank, kicker]));
        }

        if (rankGroups[0].Count == 3 && rankGroups[1].Count == 2)
        {
            return new HandCombination(
                HandCategory.FullHouse,
                [(int)rankGroups[0].Rank, (int)rankGroups[1].Rank],
                OrderByPattern(cards, [rankGroups[0].Rank, rankGroups[1].Rank]));
        }

        if (isFlush)
        {
            return new HandCombination(
                HandCategory.Flush,
                orderedByRank.Select(card => (int)card.Rank).ToList(),
                orderedByRank);
        }

        if (isStraight)
        {
            return new HandCombination(
                HandCategory.Straight,
                [straightHighRank],
                OrderStraightCards(cards, straightHighRank));
        }

        if (rankGroups[0].Count == 3)
        {
            var kickers = rankGroups
                .Where(group => group.Count == 1)
                .Select(group => (int)group.Rank)
                .OrderByDescending(rank => rank)
                .ToList();

            var tiebreak = new List<int> { (int)rankGroups[0].Rank };
            tiebreak.AddRange(kickers);

            return new HandCombination(
                HandCategory.ThreeOfAKind,
                tiebreak,
                OrderByPattern(cards, [rankGroups[0].Rank]));
        }

        if (rankGroups[0].Count == 2 && rankGroups[1].Count == 2)
        {
            var highPair = rankGroups[0].Rank;
            var lowPair = rankGroups[1].Rank;
            var kicker = rankGroups[2].Rank;
            return new HandCombination(
                HandCategory.TwoPair,
                [(int)highPair, (int)lowPair, (int)kicker],
                OrderByPattern(cards, [highPair, lowPair, kicker]));
        }

        if (rankGroups[0].Count == 2)
        {
            var kickers = rankGroups
                .Where(group => group.Count == 1)
                .Select(group => (int)group.Rank)
                .OrderByDescending(rank => rank)
                .ToList();

            var tiebreak = new List<int> { (int)rankGroups[0].Rank };
            tiebreak.AddRange(kickers);

            return new HandCombination(
                HandCategory.Pair,
                tiebreak,
                OrderByPattern(cards, [rankGroups[0].Rank]));
        }

        return new HandCombination(
            HandCategory.HighCard,
            orderedByRank.Select(card => (int)card.Rank).ToList(),
            orderedByRank);
    }

    private static IReadOnlyList<Card> OrderByPattern(IReadOnlyList<Card> cards, IReadOnlyList<Rank> rankPriority)
    {
        return cards
            .OrderBy(card =>
            {
                var index = IndexOfRank(rankPriority, card.Rank);
                return index >= 0 ? index : rankPriority.Count;
            })
            .ThenByDescending(card => (int)card.Rank)
            .ToList();
    }

    private static IReadOnlyList<Card> OrderStraightCards(IReadOnlyList<Card> cards, int straightHighRank)
    {
        if (straightHighRank == 5)
        {
            var wheelOrder = new[] { Rank.Five, Rank.Four, Rank.Three, Rank.Two, Rank.Ace };
            return cards.OrderBy(card => Array.IndexOf(wheelOrder, card.Rank)).ToList();
        }

        return cards.OrderByDescending(card => (int)card.Rank).ToList();
    }

    private static bool TryGetStraightHighRank(IReadOnlyList<Card> cards, out int highRank)
    {
        highRank = 0;
        var distinctRanks = cards
            .Select(card => (int)card.Rank)
            .Distinct()
            .OrderByDescending(rank => rank)
            .ToList();

        if (distinctRanks.Count != 5)
        {
            return false;
        }

        var isRegularStraight = true;
        for (var i = 0; i < distinctRanks.Count - 1; i++)
        {
            if (distinctRanks[i] - 1 != distinctRanks[i + 1])
            {
                isRegularStraight = false;
                break;
            }
        }

        if (isRegularStraight)
        {
            highRank = distinctRanks[0];
            return true;
        }

        // Wheel straight: A-2-3-4-5
        var wheel = new[] { 14, 5, 4, 3, 2 };
        if (wheel.All(distinctRanks.Contains))
        {
            highRank = 5;
            return true;
        }

        return false;
    }

    private static int IndexOfRank(IReadOnlyList<Rank> ranks, Rank rank)
    {
        for (var i = 0; i < ranks.Count; i++)
        {
            if (ranks[i] == rank)
            {
                return i;
            }
        }

        return -1;
    }

    private sealed record RankGroup(Rank Rank, int Count);
}

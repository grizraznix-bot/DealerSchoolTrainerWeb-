using DealerSchoolTrainerWeb.Core;

namespace DealerSchoolTrainerWeb.Games.HoldemTrainer;

public enum HandRank
{
    HighCard = 1,
    Pair = 2,
    TwoPair = 3,
    ThreeOfAKind = 4,
    Straight = 5,
    Flush = 6,
    FullHouse = 7,
    FourOfAKind = 8,
    StraightFlush = 9,
    RoyalFlush = 10
}

/// <summary>
/// Direct translation of modHEHandEvaluator.bas. Category only —
/// grading a single hand's classification doesn't need kicker/tiebreak
/// comparison. Relies on Core.Rank already being Ace-HIGH (2-14),
/// which matches clsHECard.RankValue exactly with no conversion needed.
/// </summary>
public static class HandEvaluator
{
    public static string HandRankName(HandRank rank) => rank switch
    {
        HandRank.HighCard => "High Card",
        HandRank.Pair => "Pair",
        HandRank.TwoPair => "Two Pair",
        HandRank.ThreeOfAKind => "Three of a Kind",
        HandRank.Straight => "Straight",
        HandRank.Flush => "Flush",
        HandRank.FullHouse => "Full House",
        HandRank.FourOfAKind => "Four of a Kind",
        HandRank.StraightFlush => "Straight Flush",
        HandRank.RoyalFlush => "Royal Flush",
        _ => "Unknown"
    };

    /// <summary>Best 5-card hand category from a 7-card set (2 hole + 5 community).</summary>
    public static HandRank BestHandRank(IReadOnlyList<Card> cards)
    {
        Dictionary<int, int> rankCounts = new();
        Dictionary<Suit, int> suitCounts = new();

        foreach (Card c in cards)
        {
            rankCounts[(int)c.Rank] = rankCounts.GetValueOrDefault((int)c.Rank) + 1;
            suitCounts[c.Suit] = suitCounts.GetValueOrDefault(c.Suit) + 1;
        }

        // Flush suit (5+ cards of one suit), if any.
        Suit? flushSuit = null;
        foreach (var kv in suitCounts)
        {
            if (kv.Value >= 5) flushSuit = kv.Key;
        }

        // Straight flush / Royal flush - only among the flush suit's own ranks.
        if (flushSuit.HasValue)
        {
            List<int> flushRanks = cards.Where(c => c.Suit == flushSuit.Value)
                                         .Select(c => (int)c.Rank)
                                         .ToList();

            int sfHigh = HighestStraight(flushRanks);

            if (sfHigh == 14) return HandRank.RoyalFlush;
            if (sfHigh > 0) return HandRank.StraightFlush;
        }

        // Quads / Full House / Trips / Two Pair / Pair, from rank counts.
        int[] countOfCount = new int[5]; // countOfCount[n] = how many ranks appear exactly n times
        foreach (int cnt in rankCounts.Values)
        {
            if (cnt is >= 1 and <= 4)
                countOfCount[cnt]++;
        }

        if (countOfCount[4] >= 1) return HandRank.FourOfAKind;
        if (countOfCount[3] >= 2) return HandRank.FullHouse;
        if (countOfCount[3] == 1 && countOfCount[2] >= 1) return HandRank.FullHouse;
        if (flushSuit.HasValue) return HandRank.Flush;

        if (HighestStraight(rankCounts.Keys.ToList()) > 0) return HandRank.Straight;

        if (countOfCount[3] == 1) return HandRank.ThreeOfAKind;
        if (countOfCount[2] >= 2) return HandRank.TwoPair;
        if (countOfCount[2] == 1) return HandRank.Pair;

        return HandRank.HighCard;
    }

    /// <summary>
    /// Returns the HIGH card value of the best 5-in-a-row straight
    /// among the given rank values (2-14), 0 if none. Handles the
    /// wheel (A-2-3-4-5, high card = 5) via a virtual low Ace (value 1).
    /// </summary>
    private static int HighestStraight(IReadOnlyList<int> ranks)
    {
        bool[] present = new bool[15]; // index 1-14 used

        foreach (int r in ranks)
            present[r] = true;

        if (present[14]) present[1] = true; // wheel: Ace also counts low

        int runLen = 0, bestHigh = 0;

        for (int i = 1; i <= 14; i++)
        {
            if (present[i])
            {
                runLen++;
                if (runLen >= 5) bestHigh = i;
            }
            else
            {
                runLen = 0;
            }
        }

        return bestHigh;
    }
}

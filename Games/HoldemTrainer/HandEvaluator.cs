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

    /// <summary>
    /// Full comparable strength score for an exactly-5-card hand
    /// (category + kicker tie-breaks), encoded as a single long for
    /// direct comparison. BestHandRank only returns the category,
    /// which was sufficient for the original "classify this hand"
    /// task -- the player-vs-dealer wager comparison (Advanced mode)
    /// genuinely needs to know which of two same-category hands is
    /// actually stronger, so this is new, separate logic.
    /// </summary>
    public static long HandScore(IReadOnlyList<Card> fiveCards)
    {
        HandRank category = BestHandRank(fiveCards);
        List<int> ranks = fiveCards.Select(c => (int)c.Rank).ToList();

        List<int> kickerOrder;
        if (category is HandRank.Straight or HandRank.StraightFlush or HandRank.RoyalFlush)
        {
            // Straights (including straight flush) compare purely by
            // high card -- the wheel (A-2-3-4-5) counts its high card
            // as 5, not 14.
            int high = HighestStraight(ranks);
            kickerOrder = new List<int> { high, 0, 0, 0, 0 };
        }
        else
        {
            Dictionary<int, int> counts = new();
            foreach (int r in ranks) counts[r] = counts.GetValueOrDefault(r) + 1;

            // Same "group by count first, then rank" convention already
            // used elsewhere in this project (Three Card Prime, Pai Gow
            // display sorting) for ordering pairs/trips ahead of kickers.
            kickerOrder = ranks.Distinct()
                .OrderByDescending(r => counts[r])
                .ThenByDescending(r => r)
                .ToList();
            while (kickerOrder.Count < 5) kickerOrder.Add(0);
        }

        long score = (long)category * 100_000_000L;
        long placeValue = 1_000_000L;
        foreach (int k in kickerOrder.Take(5))
        {
            score += (long)k * placeValue;
            placeValue /= 100L;
        }

        return score;
    }

    /// <summary>
    /// Determines which of the dealer's 7 cards (2 hole + 5 community)
    /// should be excluded to form their actual best 5-card hand,
    /// preferring to exclude BOTH hole cards ("plays the board") when
    /// the community cards alone already tie the best achievable
    /// category, then preferring to exclude just ONE hole card over
    /// using both when that ties the full 7-card best. Category-level
    /// comparison only (matching BestHandRank's own established
    /// simplification, no kicker tie-breaking here) -- a genuine tie
    /// (e.g. a straight flush entirely on the board) is treated as
    /// exactly one correct declaration, not several equally-valid ones.
    /// </summary>
    public static (bool ExcludeHole1, bool ExcludeHole2, bool[] ExcludeCommunity) BestDealerExclusion(
        Card hole1, Card hole2, IReadOnlyList<Card> community)
    {
        HandRank communityOnlyRank = BestHandRank(community);
        List<Card> fullPool = new List<Card> { hole1, hole2 }.Concat(community).ToList();
        HandRank fullRank = BestHandRank(fullPool);

        if (communityOnlyRank >= fullRank)
        {
            return (true, true, new bool[5]);
        }

        List<Card> pool1 = new List<Card> { hole1 }.Concat(community).ToList(); // index 0 = hole1, 1-5 = community
        List<Card> pool2 = new List<Card> { hole2 }.Concat(community).ToList();
        HandRank withHole1 = BestHandRank(pool1);
        HandRank withHole2 = BestHandRank(pool2);

        if (withHole1 >= fullRank || withHole2 >= fullRank)
        {
            bool useHole1 = withHole1 >= fullRank;
            List<Card> chosenPool = useHole1 ? pool1 : pool2;
            List<int> bestIndices = FindBestFiveIndices(chosenPool);

            bool[] excludeCommunity = new bool[5];
            for (int communityIdx = 0; communityIdx < 5; communityIdx++)
            {
                excludeCommunity[communityIdx] = !bestIndices.Contains(communityIdx + 1);
            }

            return useHole1 ? (false, true, excludeCommunity) : (true, false, excludeCommunity);
        }

        // Both hole cards genuinely needed -- find the true best-5-of-7.
        List<int> bestFullIndices = FindBestFiveIndices(fullPool); // index 0=hole1, 1=hole2, 2-6=community

        bool[] excludeCommunityFull = new bool[5];
        for (int communityIdx = 0; communityIdx < 5; communityIdx++)
        {
            excludeCommunityFull[communityIdx] = !bestFullIndices.Contains(communityIdx + 2);
        }

        return (false, false, excludeCommunityFull);
    }

    /// <summary>Indices (within pool) of the 5 cards forming the best possible hand -- ties broken by whichever qualifying combination is enumerated first, since only the category is compared, matching this file's established no-kicker-tiebreak convention for anything other than direct HandScore comparisons.</summary>
    /// <summary>The actual 5 cards (not indices) forming the best possible hand from an arbitrary pool -- used to get a player's best 5-of-7 for HandScore comparison against the dealer in Advanced mode.</summary>
    public static List<Card> BestFiveCards(IReadOnlyList<Card> pool) =>
        FindBestFiveIndices(pool).Select(i => pool[i]).ToList();

    // Compares by full HandScore (category + kickers), not just
    // category -- comparing by category alone was a real bug: once
    // ANY 5-card combination matching the best category was found,
    // it would never be replaced by a different combination of that
    // SAME category with better kickers, even though this method's
    // result (via BestFiveCards) is used for player-vs-dealer wager
    // grading, where kickers genuinely decide Win/Lose/Push.
    private static List<int> FindBestFiveIndices(IReadOnlyList<Card> pool)
    {
        List<int> bestCombo = null!;
        long bestScore = -1;

        foreach (int[] combo in Combinations(pool.Count, 5))
        {
            List<Card> fiveCards = combo.Select(i => pool[i]).ToList();
            long score = HandScore(fiveCards);
            if (score > bestScore)
            {
                bestScore = score;
                bestCombo = combo.ToList();
            }
        }

        return bestCombo;
    }

    private static IEnumerable<int[]> Combinations(int n, int k)
    {
        int[] indices = Enumerable.Range(0, k).ToArray();
        while (true)
        {
            yield return (int[])indices.Clone();

            int i = k - 1;
            while (i >= 0 && indices[i] == n - k + i) i--;
            if (i < 0) yield break;

            indices[i]++;
            for (int j = i + 1; j < k; j++) indices[j] = indices[j - 1] + 1;
        }
    }
}

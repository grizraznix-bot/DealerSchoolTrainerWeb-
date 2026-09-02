using DealerSchoolTrainerWeb.Core;
using DealerSchoolTrainerWeb.Games.HoldemTrainer;

namespace DealerSchoolTrainerWeb.Games.ThreeCardTrainer;

/// <summary>
/// 3-card hand categories. In 3-card poker, Straight ranks ABOVE
/// Flush (the reverse of standard 5-card poker) - a Straight is
/// mathematically rarer with only 3 cards.
/// </summary>
public enum ThreeCardCategory
{
    HighCard = 1,
    Pair = 2,
    Flush = 3,
    Straight = 4,
    ThreeOfAKind = 5,
    StraightFlush = 6
}

public enum WagerOutcome
{
    Win = 1,
    Lose = 2,
    Push = 3
}

/// <summary>
/// Direct translation of modTCHandEvaluator.bas. The All 6 wager's
/// best-5-of-6 poker rank reuses Games.HoldemTrainer.HandEvaluator
/// rather than duplicating a rank-counting evaluator — ResolveAllSix
/// only ever checks ">= Three of a Kind", and Holdem's HandRank enum
/// is numerically compatible up through that threshold (its one extra
/// category, Royal Flush, still satisfies ">=" against Three of a
/// Kind), so the reuse is exact for this specific comparison.
/// </summary>
public static class ThreeCardEvaluator
{
    public static ThreeCardCategory GetCategory(IReadOnlyList<Card> cards)
    {
        bool isFlush = cards[0].Suit == cards[1].Suit && cards[1].Suit == cards[2].Suit;
        bool isTrips = cards[0].Rank == cards[1].Rank && cards[1].Rank == cards[2].Rank;

        int[] r = { (int)cards[0].Rank, (int)cards[1].Rank, (int)cards[2].Rank };
        Array.Sort(r);

        // No wheel (A-2-3) in 3-card poker - Ace is high only... except
        // the wheel IS still recognized as a straight below, per the
        // original VBA (Ace sorts as 14, so r = [2,3,14] triggers this).
        bool isStraight = (r[1] == r[0] + 1 && r[2] == r[1] + 1)
                           || (r[0] == 2 && r[1] == 3 && r[2] == 14);

        bool isPair = !isTrips &&
                      (cards[0].Rank == cards[1].Rank || cards[1].Rank == cards[2].Rank || cards[0].Rank == cards[2].Rank);

        if (isStraight && isFlush) return ThreeCardCategory.StraightFlush;
        if (isTrips) return ThreeCardCategory.ThreeOfAKind;
        if (isStraight) return ThreeCardCategory.Straight;
        if (isFlush) return ThreeCardCategory.Flush;
        if (isPair) return ThreeCardCategory.Pair;
        return ThreeCardCategory.HighCard;
    }

    /// <summary>Category plus tiebreak, for full player-vs-dealer comparison (not just category).</summary>
    public static double GetScore(IReadOnlyList<Card> cards)
    {
        ThreeCardCategory category = GetCategory(cards);

        int[] rDesc = { (int)cards[0].Rank, (int)cards[1].Rank, (int)cards[2].Rank };
        Array.Sort(rDesc);
        Array.Reverse(rDesc);

        double tieValue;

        if (category == ThreeCardCategory.Pair)
        {
            int pairRank, kicker;
            if (cards[0].Rank == cards[1].Rank) { pairRank = (int)cards[0].Rank; kicker = (int)cards[2].Rank; }
            else if (cards[0].Rank == cards[2].Rank) { pairRank = (int)cards[0].Rank; kicker = (int)cards[1].Rank; }
            else { pairRank = (int)cards[1].Rank; kicker = (int)cards[0].Rank; }

            tieValue = (pairRank * 100) + kicker;
        }
        else if ((category is ThreeCardCategory.Straight or ThreeCardCategory.StraightFlush)
                 && rDesc[0] == 14 && rDesc[1] == 3 && rDesc[2] == 2)
        {
            // Wheel (A-2-3) is the LOWEST straight, standard poker
            // convention - the Ace counts as 1 here for ranking purposes
            // only, even though it still displays and sorts as the
            // highest card on screen.
            tieValue = 3;
        }
        else
        {
            tieValue = (rDesc[0] * 10000) + (rDesc[1] * 100) + rDesc[2];
        }

        return ((int)category * 1000000.0) + tieValue;
    }

    /// <summary>Returns 1 = player wins, -1 = dealer wins, 0 = push.</summary>
    public static int Compare(IReadOnlyList<Card> playerCards, IReadOnlyList<Card> dealerCards)
    {
        double pScore = GetScore(playerCards);
        double dScore = GetScore(dealerCards);

        if (pScore > dScore) return 1;
        if (pScore < dScore) return -1;
        return 0;
    }

    /// <summary>
    /// Queen or better. Any made hand (pair or higher) automatically
    /// qualifies; a High Card hand qualifies only if its top card is
    /// a Queen, King, or Ace.
    /// </summary>
    public static bool DealerQualifies(IReadOnlyList<Card> dealerCards)
    {
        if (GetCategory(dealerCards) > ThreeCardCategory.HighCard) return true;

        int highest = dealerCards.Max(c => (int)c.Rank);
        return highest >= 12; // Queen = 12
    }

    public static (WagerOutcome Ante, WagerOutcome Play) ResolveAntePlay(IReadOnlyList<Card> playerCards, IReadOnlyList<Card> dealerCards)
    {
        if (!DealerQualifies(dealerCards))
            return (WagerOutcome.Win, WagerOutcome.Push);

        int cmp = Compare(playerCards, dealerCards);
        return cmp switch
        {
            1 => (WagerOutcome.Win, WagerOutcome.Win),
            -1 => (WagerOutcome.Lose, WagerOutcome.Lose),
            _ => (WagerOutcome.Push, WagerOutcome.Push)
        };
    }

    /// <summary>Pays Pair or better.</summary>
    public static WagerOutcome ResolvePairPlus(IReadOnlyList<Card> playerCards) =>
        GetCategory(playerCards) >= ThreeCardCategory.Pair ? WagerOutcome.Win : WagerOutcome.Lose;

    /// <summary>
    /// Color only, never suit or rank. Wins if the player's 3 cards
    /// share one color, OR if all 6 cards (player + dealer) share one color.
    /// </summary>
    public static WagerOutcome ResolvePrime(IReadOnlyList<Card> playerCards, IReadOnlyList<Card> dealerCards)
    {
        if (AllSameColor(playerCards)) return WagerOutcome.Win;

        List<Card> sixCards = playerCards.Concat(dealerCards).ToList();
        return AllSameColor(sixCards) ? WagerOutcome.Win : WagerOutcome.Lose;
    }

    private static bool AllSameColor(IReadOnlyList<Card> cards)
    {
        bool firstIsRed = IsRed(cards[0]);
        return cards.Skip(1).All(c => IsRed(c) == firstIsRed);
    }

    private static bool IsRed(Card card) => card.Suit is Suit.Hearts or Suit.Diamonds;

    /// <summary>Best 5-card poker hand from all 6 cards (3 player + 3 dealer). Pays Three of a Kind or better.</summary>
    public static WagerOutcome ResolveAllSix(IReadOnlyList<Card> playerCards, IReadOnlyList<Card> dealerCards)
    {
        List<Card> sixCards = playerCards.Concat(dealerCards).ToList();
        HandRank best = HandEvaluator.BestHandRank(sixCards);
        return best >= HandRank.ThreeOfAKind ? WagerOutcome.Win : WagerOutcome.Lose;
    }

    /// <summary>
    /// Ante Bonus: pays on a Straight, Three of a Kind, or Straight
    /// Flush, regardless of whether the Ante wager itself wins or
    /// loses against the dealer -- it's judged on the player's hand
    /// alone. Those three categories are exactly the top three values
    /// in ThreeCardCategory's ordering (Straight=4, ThreeOfAKind=5,
    /// StraightFlush=6), so ">= Straight" captures all three and
    /// nothing else.
    /// </summary>
    public static bool QualifiesAnteBonus(IReadOnlyList<Card> playerCards) =>
        GetCategory(playerCards) >= ThreeCardCategory.Straight;
}

using DealerSchoolTrainerWeb.Core;

namespace DealerSchoolTrainerWeb.Games.SideWagerTrainer;

/// <summary>
/// Direct translation of modBJSWSideWagerRules.bas (itself a per-form
/// copy of modSideWagerRules.bas). Only one copy is needed here since
/// C#'s Card type is already shared across every trainer.
/// </summary>
public static class SideWagerRules
{
    public static bool QualifiesLuckyLadies(Card c1, Card c2) => TwoCardTotal(c1, c2) == 20;

    private static int TwoCardTotal(Card c1, Card c2)
    {
        int total = c1.BlackjackHardValue() + c2.BlackjackHardValue();
        int aces = (c1.IsAce() ? 1 : 0) + (c2.IsAce() ? 1 : 0);

        while (aces > 0 && total + 10 <= 21)
        {
            total += 10;
            aces--;
        }

        return total;
    }

    public static bool Qualifies21Plus3(Card p1, Card p2, Card dealerUp)
    {
        bool isFlush = p1.Suit == p2.Suit && p2.Suit == dealerUp.Suit;
        bool isThreeKind = p1.Rank == p2.Rank && p2.Rank == dealerUp.Rank;
        bool isStraight = false;

        if (!isThreeKind)
        {
            int[] ranks = { p1.AceLowRankOrder(), p2.AceLowRankOrder(), dealerUp.AceLowRankOrder() };
            isStraight = IsThreeCardStraight(ranks);
        }

        return isFlush || isThreeKind || isStraight;
    }

    private static bool IsThreeCardStraight(int[] ranks)
    {
        int[] sorted = (int[])ranks.Clone();
        Array.Sort(sorted);

        if (sorted[1] == sorted[0] + 1 && sorted[2] == sorted[1] + 1)
            return true;

        // Ace-high run: Q-K-A -> AceLowRankOrder 12,13,1
        if (sorted[0] == 1 && sorted[1] == 12 && sorted[2] == 13)
            return true;

        return false;
    }
}

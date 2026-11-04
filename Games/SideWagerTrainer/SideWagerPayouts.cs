using DealerSchoolTrainerWeb.Core;
using DealerSchoolTrainerWeb.Games.ThreeCardTrainer;

namespace DealerSchoolTrainerWeb.Games.SideWagerTrainer;

/// <summary>
/// One Side Wager Payouts round: the player's two cards, the dealer's
/// up-card (plus the hole card when the up-card is an Ace), and a 21 + 3
/// and a Lucky Ladies wager. Every round wins at least one of the two.
/// </summary>
public sealed class SideWagerPayoutRound
{
    public required Card Player1 { get; init; }
    public required Card Player2 { get; init; }
    public required Card DealerUp { get; init; }
    /// <summary>Only dealt (shown) when the up-card is an Ace.</summary>
    public Card? DealerHole { get; init; }

    public required int TwentyOnePlus3Wager { get; init; }
    public required int LuckyLadiesWager { get; init; }

    public required int TwentyOnePlus3Pays { get; init; }   // the "X" of X to 1; 0 = loses
    public required string TwentyOnePlus3Hand { get; init; } // e.g. "a Flush"
    public required int LuckyLadiesPays { get; init; }
    public required string LuckyLadiesHand { get; init; }

    public int TwentyOnePlus3Payout => TwentyOnePlus3Wager * TwentyOnePlus3Pays;
    public int LuckyLadiesPayout => LuckyLadiesWager * LuckyLadiesPays;
}

public static class SideWagerPayouts
{
    public static readonly int[] WagerAmounts = { 1, 5, 10, 25 };
    private const int Decks = 6;

    /// <summary>
    /// Deals from a freshly shuffled 6-deck shoe until the cards win
    /// 21 + 3, Lucky Ladies or both.
    /// </summary>
    public static SideWagerPayoutRound Deal(Random rng)
    {
        while (true)
        {
            // Four distinct positions in a 312-card shoe (no card is dealt twice)
            int[] picks = PickPositions(rng, 4);
            Card p1 = ShoeCard(picks[0]);
            Card p2 = ShoeCard(picks[1]);
            Card up = ShoeCard(picks[2]);
            Card? hole = up.IsAce() ? ShoeCard(picks[3]) : null;

            (int tcPays, string tcHand) = Score21Plus3(p1, p2, up);
            (int llPays, string llHand) = ScoreLuckyLadies(p1, p2, up, hole);
            if (tcPays == 0 && llPays == 0) continue;

            return new SideWagerPayoutRound
            {
                Player1 = p1,
                Player2 = p2,
                DealerUp = up,
                DealerHole = hole,
                TwentyOnePlus3Wager = WagerAmounts[rng.Next(WagerAmounts.Length)],
                LuckyLadiesWager = WagerAmounts[rng.Next(WagerAmounts.Length)],
                TwentyOnePlus3Pays = tcPays,
                TwentyOnePlus3Hand = tcHand,
                LuckyLadiesPays = llPays,
                LuckyLadiesHand = llHand
            };
        }
    }

    // 21 + 3: Straight Flush 30, Three of a Kind 20, Straight 10, Flush 5
    public static (int Pays, string Hand) Score21Plus3(Card p1, Card p2, Card up) =>
        ThreeCardEvaluator.GetCategory(new[] { p1, p2, up }) switch
        {
            ThreeCardCategory.StraightFlush => (30, "a Straight Flush"),
            ThreeCardCategory.ThreeOfAKind => (20, "Three of a Kind"),
            ThreeCardCategory.Straight => (10, "a Straight"),
            ThreeCardCategory.Flush => (5, "a Flush"),
            _ => (0, "no Flush, Straight or Three of a Kind")
        };

    // Lucky Ladies (the player's two cards must total 20):
    // Q♥ pair + dealer Blackjack 1000, Q♥ pair 125, matched 20 19,
    // suited 20 9, any 20 4
    public static (int Pays, string Hand) ScoreLuckyLadies(Card p1, Card p2, Card up, Card? hole)
    {
        if (!SideWagerRules.QualifiesLuckyLadies(p1, p2))
            return (0, "");

        bool queenHeartsPair = IsQueenOfHearts(p1) && IsQueenOfHearts(p2);
        bool dealerBlackjack = hole is not null && up.IsAce() && hole.BlackjackHardValue() == 10;

        if (queenHeartsPair && dealerBlackjack)
            return (1000, $"Queen of Hearts pair with dealer Blackjack ({Short(up)} up, {Short(hole!)} in the hole)");
        if (queenHeartsPair)
            return (125, "Queen of Hearts pair");
        if (p1.Rank == p2.Rank && p1.Suit == p2.Suit)
            return (19, "Matched 20 (identical cards)");
        if (p1.Suit == p2.Suit)
            return (9, "Suited 20");
        return (4, "Any 20");
    }

    /// <summary>Why line for the result box, e.g. "Q♥ Q♥ A♥ is a Flush: 5 to 1. $25 × 5 = $125."</summary>
    public static string Why21Plus3(SideWagerPayoutRound r)
    {
        string cards = $"{Short(r.Player1)} {Short(r.Player2)} {Short(r.DealerUp)}";
        if (r.TwentyOnePlus3Pays == 0)
            return $"{cards} has {r.TwentyOnePlus3Hand}: the wager loses.";
        return $"{cards} is {r.TwentyOnePlus3Hand}: {r.TwentyOnePlus3Pays} to 1. "
             + $"${r.TwentyOnePlus3Wager} × {r.TwentyOnePlus3Pays} = {Money(r.TwentyOnePlus3Payout)}.";
    }

    public static string WhyLuckyLadies(SideWagerPayoutRound r)
    {
        string cards = $"{Short(r.Player1)} {Short(r.Player2)}";
        if (r.LuckyLadiesPays == 0)
            return $"{cards} totals {TwoCardTotal(r.Player1, r.Player2)} (needs 20): the wager loses.";
        return $"{cards} is {r.LuckyLadiesHand}: {r.LuckyLadiesPays} to 1. "
             + $"${r.LuckyLadiesWager} × {r.LuckyLadiesPays:N0} = {Money(r.LuckyLadiesPayout)}.";
    }

    public static string Money(int dollars) => $"${dollars:N0}";

    /// <summary>"Q♥", "10♠", "A♦"</summary>
    public static string Short(Card c)
    {
        string rank = c.Rank switch
        {
            Rank.Ace => "A",
            Rank.King => "K",
            Rank.Queen => "Q",
            Rank.Jack => "J",
            _ => ((int)c.Rank).ToString()
        };
        string suit = c.Suit switch
        {
            Suit.Clubs => "♣",
            Suit.Diamonds => "♦",
            Suit.Hearts => "♥",
            _ => "♠"
        };
        return rank + suit;
    }

    private static bool IsQueenOfHearts(Card c) => c.Rank == Rank.Queen && c.Suit == Suit.Hearts;

    private static int TwoCardTotal(Card a, Card b)
    {
        int total = a.BlackjackHardValue() + b.BlackjackHardValue();
        if ((a.IsAce() || b.IsAce()) && total + 10 <= 21) total += 10;
        return total;
    }

    // Shoe position 0..311 -> card (each of the 52 cards appears 6 times)
    private static Card ShoeCard(int position)
    {
        int i = position % 52;
        return new Card((Rank)(2 + i % 13), (Suit)(i / 13));
    }

    private static int[] PickPositions(Random rng, int count)
    {
        int[] result = new int[count];
        for (int k = 0; k < count; k++)
        {
            int p;
            do { p = rng.Next(52 * Decks); } while (Array.IndexOf(result, p, 0, k) >= 0);
            result[k] = p;
        }
        return result;
    }
}

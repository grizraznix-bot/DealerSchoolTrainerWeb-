using DealerSchoolTrainerWeb.Core;
using DealerSchoolTrainerWeb.Games.ThreeCardTrainer;

namespace DealerSchoolTrainerWeb.Games.SideWagerTrainer;

/// <summary>
/// One Side Wagers round: the player's two cards, the dealer's up-card
/// (plus the hole card when the house peeks), and a 21 + 3 and a Lucky
/// Ladies wager. Call &amp; Pay rounds win at least one of the two.
/// </summary>
public sealed class SideWagerPayoutRound
{
    public required Card Player1 { get; init; }
    public required Card Player2 { get; init; }
    public required Card DealerUp { get; init; }
    /// <summary>Only dealt when the house peeks: a Queen of Hearts pair
    /// showing and the up-card an Ace or a 10-value card. The peek needs a
    /// supervisor's approval first (the trainee calls one).</summary>
    public Card? DealerHole { get; init; }

    public bool PeekNeeded => DealerHole is not null;

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
    private const int Decks = 6;

    // Practice boosts (natural rates in brackets):
    // Call Only -- half the hands win 21 + 3, Lucky Ladies or both, half
    // lose both (naturally only about 1 in 5 wins either); about 1 in 20
    // is the dealer-peek case, so calling a supervisor gets practised.
    // Call & Pay -- every hand wins at least one wager, and about 1 in 4
    // is one of the big pays: Matched 20, a Queen of Hearts pair (with or
    // without dealer Blackjack), a 21 + 3 Straight Flush or Three of a
    // Kind (together well under 1 in 100).
    private const double CallOnlyWinShare = 0.50;
    private const double BigPayShare = 0.25;
    private const double PeekShareCallOnly = 0.05;

    /// <summary>
    /// Deals from a 6-deck shoe. callAndPay false (Call Only): half the
    /// hands win at least one wager, half lose both. true (Call &amp; Pay):
    /// every hand wins at least one, about 1 in 4 a big pay.
    /// </summary>
    public static SideWagerPayoutRound Deal(Random rng, bool callAndPay = true)
    {
        if (!callAndPay)
        {
            // About 1 in 20: the peek case (Queen of Hearts pair, Ace or
            // 10-value up), so the Call Supervisor step gets practised
            if (rng.NextDouble() < PeekShareCallOnly)
                return QueenOfHeartsPeek(rng);

            bool wantWinner = rng.NextDouble() < CallOnlyWinShare;
            while (true)
            {
                SideWagerPayoutRound r = DealRandom(rng);
                bool wins = r.TwentyOnePlus3Pays > 0 || r.LuckyLadiesPays > 0;
                if (wins == wantWinner) return r;
            }
        }

        if (rng.NextDouble() < BigPayShare)
        {
            return rng.Next(100) switch
            {
                < 30 => Until(rng, r => r.LuckyLadiesPays == 19),            // Matched 20
                < 50 => QueenOfHeartsPair(rng, dealerBlackjack: false),      // 125 to 1
                < 60 => QueenOfHeartsPair(rng, dealerBlackjack: true),       // 1000 to 1
                < 80 => Until(rng, r => r.TwentyOnePlus3Pays == 30),        // Straight Flush
                _ => Until(rng, r => r.TwentyOnePlus3Pays == 20)            // Three of a Kind
            };
        }

        return Until(rng, r => r.TwentyOnePlus3Pays > 0 || r.LuckyLadiesPays > 0);
    }

    // Re-deals until the hand matches
    private static SideWagerPayoutRound Until(Random rng, Func<SideWagerPayoutRound, bool> want)
    {
        while (true)
        {
            SideWagerPayoutRound r = DealRandom(rng);
            if (want(r)) return r;
        }
    }

    // One hand as it comes: two player cards and the up-card from the
    // shoe; a hole card only when the house peeks
    private static SideWagerPayoutRound DealRandom(Random rng)
    {
        int[] picks = PickPositions(rng, 4);
        Card p1 = ShoeCard(picks[0]);
        Card p2 = ShoeCard(picks[1]);
        Card up = ShoeCard(picks[2]);
        Card? hole = Peeks(p1, p2, up) ? ShoeCard(picks[3]) : null;
        return Build(rng, p1, p2, up, hole);
    }

    // A Queen of Hearts pair. dealerBlackjack: the up-card is an Ace or a
    // 10-value card and the hole card makes Blackjack; otherwise any
    // up-card, and a hole card that doesn't (when the house peeks)
    private static SideWagerPayoutRound QueenOfHeartsPair(Random rng, bool dealerBlackjack)
    {
        Card qh = new Card(Rank.Queen, Suit.Hearts);
        while (true)
        {
            Card up = RandomCard(rng);
            Card? hole = null;
            if (Peeks(qh, qh, up))
            {
                hole = RandomCard(rng);
                bool bj = IsBlackjack(up, hole);
                if (bj != dealerBlackjack) continue;
            }
            else if (dealerBlackjack)
            {
                continue;
            }
            return Build(rng, qh, qh, up, hole);
        }
    }

    // A Queen of Hearts pair with an Ace or 10-value card up (the house
    // peeks); the hole card makes Blackjack or not, as it comes
    private static SideWagerPayoutRound QueenOfHeartsPeek(Random rng)
    {
        Card qh = new Card(Rank.Queen, Suit.Hearts);
        while (true)
        {
            Card up = RandomCard(rng);
            if (!Peeks(qh, qh, up)) continue;
            return Build(rng, qh, qh, up, RandomCard(rng));
        }
    }

    private static SideWagerPayoutRound Build(Random rng, Card p1, Card p2, Card up, Card? hole)
    {
        (int tcPays, string tcHand) = Score21Plus3(p1, p2, up);
        (int llPays, string llHand) = ScoreLuckyLadies(p1, p2, up, hole);
        return new SideWagerPayoutRound
        {
            Player1 = p1,
            Player2 = p2,
            DealerUp = up,
            DealerHole = hole,
            TwentyOnePlus3Wager = SideWager(rng),
            LuckyLadiesWager = SideWager(rng),
            TwentyOnePlus3Pays = tcPays,
            TwentyOnePlus3Hand = tcHand,
            LuckyLadiesPays = llPays,
            LuckyLadiesHand = llHand
        };
    }

    /// <summary>The house peeks (with a supervisor's approval) only when a
    /// Queen of Hearts pair shows and the up-card is an Ace or 10-value.</summary>
    public static bool Peeks(Card p1, Card p2, Card up) =>
        IsQueenOfHearts(p1) && IsQueenOfHearts(p2) && (up.IsAce() || up.BlackjackHardValue() == 10);

    // An Ace and a 10-value card, either one up
    private static bool IsBlackjack(Card up, Card hole) =>
        (up.IsAce() && hole.BlackjackHardValue() == 10) || (hole.IsAce() && up.BlackjackHardValue() == 10);

    private static Card RandomCard(Random rng) => ShoeCard(rng.Next(52 * Decks));

    // The amounts seen most on the floor, each wager on its own: $1, 5,
    // 10, 15, 20, 25, 50 -- mostly $1 or $5, then $10, now and then
    // $15-$25, rarely $50. Also the chips in Side Wagers (Win / Lose).
    internal static int SideWager(Random rng) =>
        Weighted(rng, (1, 30), (5, 30), (10, 15), (15, 7), (20, 6), (25, 7), (50, 5));

    // Weighted pick: (value, weight) pairs; weights are relative
    private static int Weighted(Random rng, params (int Value, int Weight)[] table)
    {
        int roll = rng.Next(table.Sum(t => t.Weight));
        foreach (var (value, weight) in table)
        {
            if (roll < weight) return value;
            roll -= weight;
        }
        return table[^1].Value;
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
        bool dealerBlackjack = hole is not null && IsBlackjack(up, hole);

        if (queenHeartsPair && dealerBlackjack)
            return (1000, $"Queen of Hearts pair with dealer Blackjack ({Short(up)} up, {Short(hole!)} in the hole)");
        if (queenHeartsPair && hole is not null)
            return (125, $"Queen of Hearts pair, no dealer Blackjack ({Short(up)} up, {Short(hole)} in the hole)");
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
            return $"{cards}: loses (no Flush, Straight or Three of a Kind).";
        return $"{cards}, {Bare(r.TwentyOnePlus3Hand)}: {r.TwentyOnePlus3Pays} to 1.";
    }

    public static string WhyLuckyLadies(SideWagerPayoutRound r)
    {
        string cards = $"{Short(r.Player1)} {Short(r.Player2)}";
        if (r.LuckyLadiesPays == 0)
            return $"{cards} totals {TwoCardTotal(r.Player1, r.Player2)}: loses (needs 20).";
        return $"{cards}, {r.LuckyLadiesHand}: {r.LuckyLadiesPays:N0} to 1.";
    }

    public static string Money(int dollars) => $"${dollars:N0}";

    // "a Flush" -> "Flush"
    private static string Bare(string hand) => hand.StartsWith("a ") ? hand[2..] : hand;

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

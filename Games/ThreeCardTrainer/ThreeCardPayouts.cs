using DealerSchoolTrainerWeb.Core;
using DealerSchoolTrainerWeb.Games.HoldemTrainer;

namespace DealerSchoolTrainerWeb.Games.ThreeCardTrainer;

/// <summary>
/// What a wager pays: an amount of winnings, a push, or a loss
/// (Amount 0 and not a push).
/// </summary>
public readonly record struct TcpPay(int Amount, bool IsPush)
{
    public bool IsLose => Amount == 0 && !IsPush;

    public static TcpPay Lose => new(0, false);
    public static TcpPay Push => new(0, true);
    public static TcpPay Win(int amount) => new(amount, false);
}

/// <summary>
/// One Three Card Prime payout round: both hands, the five wagers (Ante
/// and Play always equal), and what each one pays with the reason.
/// Wagers are in settling order: Play, Ante, Pair Bonus, Prime, All 6.
/// </summary>
public sealed class TcpPayoutRound
{
    public const int Play = 0, Ante = 1, PairBonus = 2, Prime = 3, AllSix = 4;
    public static readonly string[] Names = { "Play", "Ante", "Pair Bonus", "Prime", "All 6" };

    public required Card[] PlayerCards { get; init; }
    public required Card[] DealerCards { get; init; }
    public required int[] Wagers { get; init; }      // by the index constants above
    public required TcpPay[] Pays { get; init; }
    public required string[] Why { get; init; }

    public required bool DealerQualifies { get; init; }
    public required string PlayerHand { get; init; }  // e.g. "Straight Flush", "Pair of 7s"
    public required string DealerHand { get; init; }  // e.g. "King high"
}

public static class ThreeCardPayouts
{
    // Training boost carried over from the Win/Lose mode: about 1 hand in 4
    // has a winning All 6 (vs about 1 in 14 dealt naturally)
    private const double AllSixBoostRate = 0.19;

    public static TcpPayoutRound Deal(Random rng)
    {
        bool forceAllSix = rng.NextDouble() < AllSixBoostRate;
        Card[] player, dealer;
        do
        {
            Deck deck = new();
            deck.Shuffle();
            player = new[] { deck.Draw(), deck.Draw(), deck.Draw() };
            dealer = new[] { deck.Draw(), deck.Draw(), deck.Draw() };
        }
        while (forceAllSix && ThreeCardEvaluator.ResolveAllSix(player, dealer) != WagerOutcome.Win);

        SortForDisplay(player);
        SortForDisplay(dealer);

        int antePlay = AntePlayWager(rng);
        int[] wagers = { antePlay, antePlay, SideWager(rng), PrimeWager(rng), AllSixWager(rng) };
        return Settle(player, dealer, wagers);
    }

    /// <summary>Works out every wager's pay and the reason for it.</summary>
    public static TcpPayoutRound Settle(Card[] player, Card[] dealer, int[] wagers)
    {
        TcpPay[] pays = new TcpPay[5];
        string[] why = new string[5];

        string playerHand = DescribeThree(player);
        string dealerHand = DescribeThree(dealer);
        bool qualifies = ThreeCardEvaluator.DealerQualifies(dealer);
        (WagerOutcome anteOutcome, WagerOutcome playOutcome) = ThreeCardEvaluator.ResolveAntePlay(player, dealer);

        string versus;
        if (!qualifies)
        {
            versus = $"Dealer doesn't qualify ({dealerHand})";
        }
        else
        {
            int cmp = ThreeCardEvaluator.Compare(player, dealer);
            versus = cmp > 0 ? $"Player's {playerHand} beats dealer's {dealerHand}"
                   : cmp < 0 ? $"Dealer's {dealerHand} beats player's {playerHand}"
                   : $"Tie, {playerHand}";
        }

        // Play: 1 to 1; pushes when the dealer doesn't qualify or on a tie
        int w = wagers[TcpPayoutRound.Play];
        pays[TcpPayoutRound.Play] = playOutcome switch
        {
            WagerOutcome.Win => TcpPay.Win(w),
            WagerOutcome.Push => TcpPay.Push,
            _ => TcpPay.Lose
        };
        why[TcpPayoutRound.Play] = playOutcome switch
        {
            WagerOutcome.Win => $"{versus}: pays even.",
            WagerOutcome.Push => $"{versus}: pushes.",
            _ => $"{versus}."
        };

        // Ante: 1 to 1, plus the Ante Bonus (paid on the player's hand,
        // whether the Ante wins, pushes or loses), as one payout
        w = wagers[TcpPayoutRound.Ante];
        int anteWin = anteOutcome == WagerOutcome.Win ? w : 0;
        (int bonusTo1, string bonusHand) = AnteBonus(player);
        int bonus = w * bonusTo1;
        int anteTotal = anteWin + bonus;

        string antePart = anteOutcome switch
        {
            WagerOutcome.Win => qualifies ? $"{versus}: Ante pays even" : $"{versus}: Ante still pays even",
            WagerOutcome.Push => $"{versus}: Ante pushes",
            _ => $"{versus}: Ante loses"
        };

        if (bonus > 0)
        {
            pays[TcpPayoutRound.Ante] = TcpPay.Win(anteTotal);
            string bonusPart = $"Ante Bonus {bonusTo1} to 1 ({bonusHand})";
            why[TcpPayoutRound.Ante] = anteWin > 0
                ? $"{antePart}, plus {bonusPart}."
                : $"{antePart}, but the {bonusPart} still pays.";
        }
        else
        {
            pays[TcpPayoutRound.Ante] = anteOutcome switch
            {
                WagerOutcome.Win => TcpPay.Win(anteWin),
                WagerOutcome.Push => TcpPay.Push,
                _ => TcpPay.Lose
            };
            why[TcpPayoutRound.Ante] = $"{antePart}; no Ante Bonus.";
        }

        // Pair Bonus: player's 3 cards
        w = wagers[TcpPayoutRound.PairBonus];
        int pbTo1 = ThreeCardEvaluator.GetCategory(player) switch
        {
            ThreeCardCategory.StraightFlush => 40,
            ThreeCardCategory.ThreeOfAKind => 30,
            ThreeCardCategory.Straight => 6,
            ThreeCardCategory.Flush => 3,
            ThreeCardCategory.Pair => 1,
            _ => 0
        };
        string cards3 = string.Join(" ", player.Select(SideWagerTrainer.SideWagerPayouts.Short));
        pays[TcpPayoutRound.PairBonus] = pbTo1 > 0 ? TcpPay.Win(w * pbTo1) : TcpPay.Lose;
        why[TcpPayoutRound.PairBonus] = pbTo1 > 0
            ? $"{cards3}, {playerHand}: {pbTo1} to 1."
            : $"{cards3}, {playerHand}: loses (needs a Pair or better).";

        // Prime: all 6 cards one color 4 to 1; else the player's 3 one color 3 to 1
        w = wagers[TcpPayoutRound.Prime];
        bool playerOne = OneColor(player);
        bool allOne = playerOne && OneColor(player.Concat(dealer).ToArray());
        string color = IsRed(player[0]) ? "red" : "black";
        if (allOne)
        {
            pays[TcpPayoutRound.Prime] = TcpPay.Win(w * 4);
            why[TcpPayoutRound.Prime] = $"All 6 cards {color}: 4 to 1.";
        }
        else if (playerOne)
        {
            pays[TcpPayoutRound.Prime] = TcpPay.Win(w * 3);
            why[TcpPayoutRound.Prime] = $"Player's 3 cards {color}: 3 to 1.";
        }
        else
        {
            pays[TcpPayoutRound.Prime] = TcpPay.Lose;
            why[TcpPayoutRound.Prime] = "Player's cards mixed colors: loses.";
        }

        // All 6: best 5 of all 6 cards
        w = wagers[TcpPayoutRound.AllSix];
        List<Card> six = player.Concat(dealer).ToList();
        HandRank best = HandEvaluator.BestHandRank(six);
        int a6To1 = best switch
        {
            HandRank.RoyalFlush => 1000,
            HandRank.StraightFlush => 200,
            HandRank.FourOfAKind => 50,
            HandRank.FullHouse => 25,
            HandRank.Flush => 20,
            HandRank.Straight => 10,
            HandRank.ThreeOfAKind => 5,
            _ => 0
        };
        string sixHand = HandEvaluator.DescribeHand(six);
        pays[TcpPayoutRound.AllSix] = a6To1 > 0 ? TcpPay.Win(w * a6To1) : TcpPay.Lose;
        why[TcpPayoutRound.AllSix] = a6To1 > 0
            ? $"All 6 cards make {WithArticle(HandEvaluator.HandRankName(best))}: {a6To1:N0} to 1."
            : $"All 6 cards make {WithArticle(HandEvaluator.HandRankName(best))}: loses (needs Three of a Kind or better).";

        return new TcpPayoutRound
        {
            PlayerCards = player,
            DealerCards = dealer,
            Wagers = wagers,
            Pays = pays,
            Why = why,
            DealerQualifies = qualifies,
            PlayerHand = Capitalize(playerHand),
            DealerHand = Capitalize(dealerHand)
        };
    }

    // ---- Real-world betting patterns ----------------------------------
    // (also used for the chips in Three Card Prime's Win / Lose / Push mode)

    // The amounts seen most on the floor (also the chips in the Win / Lose
    // / Push mode).
    // Ante/Play: $5, 10, 15, 20, 25, 50, 75, 100, then $25 steps to $300 --
    // mostly $5-$15, then $20-$25, then $50-$100, rarely $125-$300
    internal static int AntePlayWager(Random rng) =>
        Weighted(rng, (5, 17), (10, 17), (15, 16), (20, 13), (25, 12), (50, 8), (75, 6), (100, 6),
                      (125, 1), (150, 1), (175, 1), (200, 1), (225, 1), (250, 1), (275, 1), (300, 1));

    // Pair Bonus: $5, 10, 15, 20, 25, 50, 75, 100 -- mostly $5-$10,
    // sometimes $15-$25, rarely $50-$100
    internal static int SideWager(Random rng) =>
        Weighted(rng, (5, 35), (10, 35), (15, 9), (20, 8), (25, 8), (50, 2), (75, 1), (100, 2));

    // Prime: $5, 10, 15, 20, 25, 50, 100 -- same pattern, no $75
    internal static int PrimeWager(Random rng) =>
        Weighted(rng, (5, 35), (10, 35), (15, 9), (20, 8), (25, 8), (50, 3), (100, 2));

    // All 6: $1 or $5
    internal static int AllSixWager(Random rng) => rng.Next(2) == 0 ? 1 : 5;

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

    // ---- Helpers ----------------------------------------------------------

    private static (int To1, string Hand) AnteBonus(Card[] player) =>
        ThreeCardEvaluator.GetCategory(player) switch
        {
            ThreeCardCategory.StraightFlush => (5, "a Straight Flush"),
            ThreeCardCategory.ThreeOfAKind => (4, "Three of a Kind"),
            ThreeCardCategory.Straight => (1, "a Straight"),
            _ => (0, "")
        };

    public static string Money(int dollars) => $"${dollars:N0}";

    private static bool IsRed(Card c) => c.Suit is Suit.Hearts or Suit.Diamonds;

    private static bool OneColor(Card[] cards) => cards.All(IsRed) || !cards.Any(IsRed);

    /// <summary>e.g. "Straight Flush", "Three 7s", "Pair of 7s", "King high"</summary>
    public static string DescribeThree(IReadOnlyList<Card> cards)
    {
        switch (ThreeCardEvaluator.GetCategory(cards))
        {
            case ThreeCardCategory.StraightFlush: return "Straight Flush";
            case ThreeCardCategory.ThreeOfAKind: return $"Three {RankPlural(cards[0].Rank)}";
            case ThreeCardCategory.Straight: return "Straight";
            case ThreeCardCategory.Flush: return "Flush";
            case ThreeCardCategory.Pair:
                Rank pair = cards.GroupBy(c => c.Rank).First(g => g.Count() == 2).Key;
                return $"Pair of {RankPlural(pair)}";
            default:
                return $"{RankName(cards.Max(c => c.Rank))} high";
        }
    }

    // "a Straight Flush", "a Pair of 7s", "Three 7s", "King high"
    private static string WithArticle(string hand) =>
        hand.StartsWith("Three") || hand.EndsWith("high") ? hand : "a " + hand;

    private static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private static string RankName(Rank r) => r switch
    {
        Rank.Ace => "Ace",
        Rank.King => "King",
        Rank.Queen => "Queen",
        Rank.Jack => "Jack",
        _ => ((int)r).ToString()
    };

    private static string RankPlural(Rank r) => r == Rank.Six ? "6s" : RankName(r) + "s";

    // Pair or trips grouped ahead of the kicker, then high to low (same as
    // the Win/Lose mode)
    private static void SortForDisplay(Card[] cards)
    {
        Dictionary<Rank, int> counts = cards.GroupBy(c => c.Rank).ToDictionary(g => g.Key, g => g.Count());
        Array.Sort(cards, (a, b) =>
        {
            int ca = counts[a.Rank], cb = counts[b.Rank];
            if (ca != cb) return cb.CompareTo(ca);
            return ((int)b.Rank).CompareTo((int)a.Rank);
        });
    }
}

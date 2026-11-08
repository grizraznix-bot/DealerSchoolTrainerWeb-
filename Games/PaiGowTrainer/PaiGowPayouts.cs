namespace DealerSchoolTrainerWeb.Games.PaiGowTrainer;

/// <summary>What a wager pays: an amount of winnings, a push, or a loss.</summary>
public readonly record struct PgPay(int Amount, bool IsPush)
{
    public bool IsLose => Amount == 0 && !IsPush;

    public static PgPay Lose => new(0, false);
    public static PgPay Push => new(0, true);
    public static PgPay Win(int amount) => new(amount, false);
}

/// <summary>
/// One Pai Gow payout round: both hands already set per the House Way
/// (no fouls), the three wagers, and what each one pays with the reason.
/// Wagers in settling order: Pai Gow Insurance, Main Wager, Emperor's
/// Treasure.
/// </summary>
public sealed class PgPayoutRound
{
    public const int Insurance = 0, Main = 1, Emperors = 2;
    public static readonly string[] Names = { "Pai Gow Insurance", "Main Wager", "Emperor's Treasure" };
    public static readonly string[] ShortNames = { "Insurance", "Main Wager", "Emperor's Treasure" };

    public required List<PaiGowCard> DealerLow { get; init; }
    public required List<PaiGowCard> DealerHigh { get; init; }
    public required List<PaiGowCard> PlayerLow { get; init; }
    public required List<PaiGowCard> PlayerHigh { get; init; }
    public required string DealerLowLabel { get; init; }
    public required string DealerHighLabel { get; init; }
    public required string PlayerLowLabel { get; init; }
    public required string PlayerHighLabel { get; init; }
    public required bool DealerAceHighPaiGow { get; init; }

    public required int[] Wagers { get; init; }
    public required PgPay[] Pays { get; init; }
    public required string[] Why { get; init; }

    /// <summary>Envy Bonus tip for the feedback (empty when none applies).</summary>
    public required string EnvyTip { get; init; }
}

public static class PaiGowPayouts
{
    // Natural odds of the Joker landing in one of the two hands (7 of
    // 53 cards per hand), same as the other Pai Gow modes' default
    private const double JokerProbability = 2 * 7.0 / 53.0;

    // Training boost: about 1 hand in 7 gives the player Four of a Kind
    // or better for Emperor's Treasure (naturally well under 1 in 100)
    private const double BigHandBoostRate = 0.15;

    /// <summary>
    /// jokerProbability: chance the Joker lands in one of the two hands
    /// (null = the natural 14 of 53, from the Joker Frequency setting).
    /// </summary>
    public static PgPayoutRound Deal(Random rng, double? jokerProbability = null)
    {
        bool forceBigHand = rng.NextDouble() < BigHandBoostRate;
        PaiGowSolver player = new();
        PaiGowSolver dealer = new();
        List<PaiGowCard> playerCards, dealerCards;

        int tries = 0;
        do
        {
            PaiGowDeck deck = new();
            (playerCards, dealerCards) = deck.DealTwoSevenCardHandsWithJokerBias(jokerProbability ?? JokerProbability);
            player.SetCards(playerCards);
            tries++;
        }
        while (forceBigHand && tries < 5000 && EmperorsTo1(player).To1 < 25);

        dealer.SetCards(dealerCards);

        int[] wagers = { SideWager(rng), MainWager(rng), SideWager(rng) };
        return Settle(player, playerCards, dealer, dealerCards, wagers);
    }

    public static PgPayoutRound Settle(PaiGowSolver player, List<PaiGowCard> playerCards,
                                       PaiGowSolver dealer, List<PaiGowCard> dealerCards, int[] wagers)
    {
        // Both hands set per the House Way
        (int pLow1, int pLow2, int[] pHigh) = player.GetCorrectHand();
        (int dLow1, int dLow2, int[] dHigh) = dealer.GetCorrectHand();

        PgPay[] pays = new PgPay[3];
        string[] why = new string[3];

        // ---- Main Wager: 1 to 1, no commission --------------------------
        int w = wagers[PgPayoutRound.Main];
        bool aceHighPaiGow = dealer.IsAceHighPaiGow(dHigh);
        if (aceHighPaiGow)
        {
            pays[PgPayoutRound.Main] = PgPay.Push;
            why[PgPayoutRound.Main] = "Dealer has an Ace-high Pai Gow: all hands push.";
        }
        else
        {
            double pl = player.ScoreLowHand(pLow1, pLow2), ph = player.ScoreHighHand(pHigh);
            double dl = dealer.ScoreLowHand(dLow1, dLow2), dh = dealer.ScoreHighHand(dHigh);
            bool winsHigh = ph > dh, winsLow = pl > dl;



            if (winsHigh && winsLow)
            {
                pays[PgPayoutRound.Main] = PgPay.Win(w);
                why[PgPayoutRound.Main] = "Player wins High and Low: pays even (no commission).";
            }
            else if (winsHigh || winsLow)
            {
                pays[PgPayoutRound.Main] = PgPay.Push;
                why[PgPayoutRound.Main] = $"Player wins the {(winsHigh ? "High" : "Low")}, dealer the {(winsHigh ? "Low" : "High")}: pushes.";
            }
            else
            {
                pays[PgPayoutRound.Main] = PgPay.Lose;
                string copy = ph == dh || pl == dl ? " (a copy goes to the dealer)" : "";
                why[PgPayoutRound.Main] = $"Dealer wins High and Low{copy}.";
            }
        }

        // ---- Pai Gow Insurance: player's 7 cards are a Pai Gow ----------
        w = wagers[PgPayoutRound.Insurance];
        if (player.QualifiesInsurance())
        {
            bool hasJoker = playerCards.Any(c => c.IsJoker);
            int top = hasJoker ? 14 : playerCards.Max(c => c.RankOrder);
            (int to1, string name) = top switch
            {
                14 => (3, "A high"),
                13 => (5, "K high"),
                12 => (6, "Q high"),
                11 => (15, "J high"),
                10 => (25, "10 high"),
                _ => (100, "9 high")
            };
            string jokerNote = hasJoker ? " (the Joker counts as an Ace)" : "";
            pays[PgPayoutRound.Insurance] = PgPay.Win(w * to1);
            why[PgPayoutRound.Insurance] = $"{name} Pai Gow{jokerNote}: {to1} to 1.";
        }
        else
        {
            pays[PgPayoutRound.Insurance] = PgPay.Lose;
            why[PgPayoutRound.Insurance] = $"Not a Pai Gow ({player.BestSevenCardCategoryName()}): loses.";
        }

        // ---- Emperor's Treasure: player's 7 cards -----------------------
        w = wagers[PgPayoutRound.Emperors];
        (int etTo1, string etName, int envy) = EmperorsTo1(player);
        if (etTo1 > 0)
        {
            pays[PgPayoutRound.Emperors] = PgPay.Win(w * etTo1);
            why[PgPayoutRound.Emperors] = $"{etName}: {etTo1:N0} to 1.";
        }
        else
        {
            pays[PgPayoutRound.Emperors] = PgPay.Lose;
            why[PgPayoutRound.Emperors] = $"{etName}: loses (needs Three of a Kind or better).";
        }

        string envyTip = envy > 0
            ? $"At a full table, every other player with an Emperor's Treasure wager would also be paid a {Money(envy)} Envy Bonus for this {etName}."
            : "";

        return new PgPayoutRound
        {
            DealerLow = SortForDisplay(new() { dealerCards[dLow1 - 1], dealerCards[dLow2 - 1] }),
            DealerHigh = SortForDisplay(Enumerable.Range(1, 5).Select(i => dealerCards[dHigh[i] - 1]).ToList()),
            PlayerLow = SortForDisplay(new() { playerCards[pLow1 - 1], playerCards[pLow2 - 1] }),
            PlayerHigh = SortForDisplay(Enumerable.Range(1, 5).Select(i => playerCards[pHigh[i] - 1]).ToList()),
            DealerLowLabel = dealer.LowHandLabel(dLow1, dLow2),
            DealerHighLabel = dealer.HighHandDescription(dHigh),
            PlayerLowLabel = player.LowHandLabel(pLow1, pLow2),
            PlayerHighLabel = player.HighHandDescription(pHigh),
            DealerAceHighPaiGow = aceHighPaiGow,
            Wagers = wagers,
            Pays = pays,
            Why = why,
            EnvyTip = envyTip
        };
    }

    /// <summary>Emperor's Treasure pay (X to 1, 0 = loses), hand name and Envy Bonus.</summary>
    public static (int To1, string Name, int Envy) EmperorsTo1(PaiGowSolver s)
    {
        if (s.IsSevenCardStraightFlushNoJoker()) return (2500, "7-Card Straight Flush with no Joker", 1000);
        if (s.IsRoyalFlushPlusAceKingSuited()) return (1000, "Royal Flush + Ace & King suited", 750);
        if (s.IsSevenCardStraightFlushWithJoker()) return (750, "7-Card Straight Flush with the Joker", 250);

        string best = s.BestSevenCardCategoryName();
        return best switch
        {
            "Five Aces" => (250, best, 100),
            "Royal Flush" => (125, best, 50),
            "Straight Flush" => (50, best, 20),
            "Four of a Kind" => (25, best, 5),
            "Full House" => (5, best, 0),
            "Flush" => (4, best, 0),
            "Three of a Kind" => (3, best, 0),
            "Straight" => (2, best, 0),
            _ => (0, best, 0)
        };
    }

    // ---- Real-world betting patterns ------------------------------------

    // The amounts seen most on the floor (also the chips in Settling Bets).
    // Main Wager: $10, 15, 20, 25, 50, 100, 200, 300 -- mostly $10-$25,
    // then $50, then $100, rarely $200 / $300
    internal static int MainWager(Random rng) =>
        Weighted(rng, (10, 14), (15, 14), (20, 14), (25, 13), (50, 25), (100, 15), (200, 3), (300, 2));

    // Pai Gow Insurance / Emperor's Treasure: $1, 5, 10, 15, 20, 25, 30 --
    // mostly $1 or $5, then $10, rarely $15-$30
    internal static int SideWager(Random rng) =>
        Weighted(rng, (1, 32), (5, 33), (10, 18), (15, 5), (20, 4), (25, 5), (30, 3));

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

    public static string Money(int dollars) => $"${dollars:N0}";


    private static int SortValue(PaiGowCard c) => c.IsJoker ? 14 : c.RankOrder;

    // Pairs/trips grouped first, then high to low (same as Settling Bets)
    private static List<PaiGowCard> SortForDisplay(List<PaiGowCard> cards)
    {
        Dictionary<int, int> counts = new();
        foreach (PaiGowCard c in cards) counts[SortValue(c)] = counts.GetValueOrDefault(SortValue(c)) + 1;
        return cards.OrderByDescending(c => counts[SortValue(c)]).ThenByDescending(SortValue).ToList();
    }
}

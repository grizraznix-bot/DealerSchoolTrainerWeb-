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
    public static readonly string[] ShortNames = { "Insurance", "Main Wager", "Emperor's" };

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

    public static PgPayoutRound Deal(Random rng)
    {
        bool forceBigHand = rng.NextDouble() < BigHandBoostRate;
        PaiGowSolver player = new();
        PaiGowSolver dealer = new();
        List<PaiGowCard> playerCards, dealerCards;

        int tries = 0;
        do
        {
            PaiGowDeck deck = new();
            (playerCards, dealerCards) = deck.DealTwoSevenCardHandsWithJokerBias(JokerProbability);
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
            why[PgPayoutRound.Main] = "The dealer has an Ace-high Pai Gow, so all hands push.";
        }
        else
        {
            double pl = player.ScoreLowHand(pLow1, pLow2), ph = player.ScoreHighHand(pHigh);
            double dl = dealer.ScoreLowHand(dLow1, dLow2), dh = dealer.ScoreHighHand(dHigh);
            bool winsHigh = ph > dh, winsLow = pl > dl;

            string high = $"High: player's {player.HighHandDescription(pHigh)} {Versus(winsHigh, ph == dh)} the dealer's {dealer.HighHandDescription(dHigh)}";
            string low = $"Low: player's {LowText(playerCards[pLow1 - 1], playerCards[pLow2 - 1], player.LowHandLabel(pLow1, pLow2))} {Versus(winsLow, pl == dl)} the dealer's {LowText(dealerCards[dLow1 - 1], dealerCards[dLow2 - 1], dealer.LowHandLabel(dLow1, dLow2))}";

            if (winsHigh && winsLow)
            {
                pays[PgPayoutRound.Main] = PgPay.Win(w);
                why[PgPayoutRound.Main] = $"{high}. {low}. The player wins both hands: 1 to 1, no commission. {Money(w)} × 1 = {Money(w)}.";
            }
            else if (winsHigh || winsLow)
            {
                pays[PgPayoutRound.Main] = PgPay.Push;
                why[PgPayoutRound.Main] = $"{high}. {low}. One hand each: the Main Wager pushes.";
            }
            else
            {
                pays[PgPayoutRound.Main] = PgPay.Lose;
                string copy = ph == dh || pl == dl ? " (a copy goes to the dealer)" : "";
                why[PgPayoutRound.Main] = $"{high}. {low}. The dealer wins both hands{copy}: the Main Wager loses.";
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
                14 => (3, "Ace high"),
                13 => (5, "King high"),
                12 => (6, "Queen high"),
                11 => (15, "Jack high"),
                10 => (25, "10 high"),
                _ => (100, "9 high")
            };
            string jokerNote = hasJoker ? " (the Joker counts as an Ace)" : "";
            pays[PgPayoutRound.Insurance] = PgPay.Win(w * to1);
            why[PgPayoutRound.Insurance] = $"The player's 7 cards are {(top == 14 ? "an" : "a")} {name} Pai Gow{jokerNote}: {to1} to 1. {Money(w)} × {to1} = {Money(w * to1)}.";
        }
        else
        {
            pays[PgPayoutRound.Insurance] = PgPay.Lose;
            why[PgPayoutRound.Insurance] = $"The player's 7 cards aren't a Pai Gow ({player.BestSevenCardCategoryName()}): the wager loses.";
        }

        // ---- Emperor's Treasure: player's 7 cards -----------------------
        w = wagers[PgPayoutRound.Emperors];
        (int etTo1, string etName, int envy) = EmperorsTo1(player);
        if (etTo1 > 0)
        {
            pays[PgPayoutRound.Emperors] = PgPay.Win(w * etTo1);
            why[PgPayoutRound.Emperors] = $"The player's 7 cards make {WithArticle(etName)}: {etTo1:N0} to 1. {Money(w)} × {etTo1:N0} = {Money(w * etTo1)}.";
        }
        else
        {
            pays[PgPayoutRound.Emperors] = PgPay.Lose;
            why[PgPayoutRound.Emperors] = $"The player's 7 cards make {WithArticle(etName)} (needs Three of a Kind or better): the wager loses.";
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

    // Main Wager $10-$300 in $5 steps: mostly $10-$25, then $30-$50
    // (mostly $50), then $55-$100 (mostly $75/$100), rarely $105-$300
    // (mostly $150/$200/$300)
    internal static int MainWager(Random rng)
    {
        double r = rng.NextDouble();
        if (r < 0.55) return Pick(rng, 10, 15, 20, 25);
        if (r < 0.80) return rng.NextDouble() < 0.6 ? 50 : Pick(rng, 30, 35, 40, 45);
        if (r < 0.95) return rng.NextDouble() < 0.6 ? Pick(rng, 75, 100) : FivesBetween(rng, 55, 100);
        return rng.NextDouble() < 0.6 ? Pick(rng, 150, 200, 300) : FivesBetween(rng, 105, 300);
    }

    // Insurance / Emperor's Treasure $1-$30 ($1-$5, then $5 steps): mostly
    // $1 or $5, sometimes $2-$4 or $10, rarely $15-$30
    internal static int SideWager(Random rng)
    {
        double r = rng.NextDouble();
        if (r < 0.65) return Pick(rng, 1, 5);
        if (r < 0.93) return Pick(rng, 2, 3, 4, 10);
        return Pick(rng, 15, 20, 25, 30);
    }

    private static int Pick(Random rng, params int[] values) => values[rng.Next(values.Length)];

    private static int FivesBetween(Random rng, int low, int high) => low + 5 * rng.Next((high - low) / 5 + 1);

    // ---- Helpers ----------------------------------------------------------

    public static string Money(int dollars) => $"${dollars:N0}";

    private static string Versus(bool playerWins, bool tie) =>
        playerWins ? "beats" : tie ? "ties (copy, dealer wins)" : "loses to";

    private static string WithArticle(string hand) =>
        hand.StartsWith("Three") || hand.StartsWith("Four") || hand.StartsWith("Five") || hand.StartsWith("Two")
            || hand.StartsWith("7-Card") || hand.StartsWith("High")
            ? hand
            : "a " + hand;

    // A Low hand that's just high cards reads as its ranks, e.g. "K-Q"
    // (so K-Q vs K-J shows why it wins); a pair keeps its name
    private static string LowText(PaiGowCard a, PaiGowCard b, string label)
    {
        if (!label.EndsWith("high")) return label;
        PaiGowCard[] two = new[] { a, b }.OrderByDescending(SortValue).ToArray();
        return $"{RankText(two[0])}-{RankText(two[1])}";
    }

    private static string RankText(PaiGowCard c) => c.IsJoker ? "A" : c.Rank == "T" ? "10" : c.Rank;

    private static int SortValue(PaiGowCard c) => c.IsJoker ? 14 : c.RankOrder;

    // Pairs/trips grouped first, then high to low (same as Settling Bets)
    private static List<PaiGowCard> SortForDisplay(List<PaiGowCard> cards)
    {
        Dictionary<int, int> counts = new();
        foreach (PaiGowCard c in cards) counts[SortValue(c)] = counts.GetValueOrDefault(SortValue(c)) + 1;
        return cards.OrderByDescending(c => counts[SortValue(c)]).ThenByDescending(SortValue).ToList();
    }
}

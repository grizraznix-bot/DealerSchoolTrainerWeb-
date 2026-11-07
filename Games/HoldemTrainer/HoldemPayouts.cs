using DealerSchoolTrainerWeb.Core;

namespace DealerSchoolTrainerWeb.Games.HoldemTrainer;

/// <summary>What a wager pays, in cents (Odds pays 3 to 2 on a Flush), or a push / loss.</summary>
public readonly record struct HdPay(int Cents, bool IsPush)
{
    public bool IsLose => Cents == 0 && !IsPush;

    public static HdPay Lose => new(0, false);
    public static HdPay Push => new(0, true);
    public static HdPay Win(int cents) => new(cents, false);
}

/// <summary>
/// One Heads-Up Hold'em payout round: the dealer's and player's hole
/// cards, the board, and the five wagers (Odds always equals the Ante;
/// the Raise is 3x / 2x / 1x the Ante by when it was made). Wagers in
/// settling order: Raise, Ante, Odds, Trips Plus, Pocket Bonus.
/// </summary>
public sealed class HdPayoutRound
{
    public const int Raise = 0, Ante = 1, Odds = 2, Trips = 3, Pocket = 4;
    public static readonly string[] Names = { "Raise", "Ante", "Odds", "Trips Plus", "Pocket Bonus" };

    public required Card[] DealerCards { get; init; }
    public required Card[] PlayerCards { get; init; }
    public required Card[] Board { get; init; }
    public required string DealerHand { get; init; }  // e.g. "Three of a Kind"
    public required string PlayerHand { get; init; }
    public required bool DealerOpens { get; init; }

    public required int[] Wagers { get; init; }        // whole dollars
    public required string RaiseTiming { get; init; }  // e.g. "3x pre-flop"
    public required HdPay[] Pays { get; init; }
    public required string[] Why { get; init; }
}

public static class HoldemPayouts
{
    // Training boost: about 1 hand in 5 gives the player a Straight or
    // better (so Odds pays more often); about a third of those lose to
    // the dealer, for Bad Beat practice
    private const double BigHandRate = 0.20;
    private const double BadBeatShare = 0.30;

    public static HdPayoutRound Deal(Random rng)
    {
        bool forceBig = rng.NextDouble() < BigHandRate;
        bool forceBadBeat = forceBig && rng.NextDouble() < BadBeatShare;

        Card[] dealer, player, board;
        int tries = 0;
        while (true)
        {
            Deck deck = new();
            deck.Shuffle();
            dealer = new[] { deck.Draw(), deck.Draw() };
            player = new[] { deck.Draw(), deck.Draw() };
            board = new[] { deck.Draw(), deck.Draw(), deck.Draw(), deck.Draw(), deck.Draw() };
            tries++;
            if (!forceBig || tries > 20000) break;

            HandRank pr = HandEvaluator.BestHandRank(player.Concat(board).ToList());
            if (pr < HandRank.Straight) continue;
            if (!forceBadBeat) break;
            if (Compare(player, dealer, board) < 0) break;
        }

        int ante = AnteWager(rng);
        (int mult, string timing) = RaiseTiming(rng);
        int[] wagers = { ante * mult, ante, ante, SideWager(rng), SideWager(rng) };
        return Settle(dealer, player, board, wagers, timing);
    }

    public static HdPayoutRound Settle(Card[] dealer, Card[] player, Card[] board, int[] wagers, string raiseTiming)
    {
        List<Card> player7 = player.Concat(board).ToList();
        List<Card> dealer7 = dealer.Concat(board).ToList();
        HandRank pr = HandEvaluator.BestHandRank(player7);
        HandRank dr = HandEvaluator.BestHandRank(dealer7);
        bool opens = dr >= HandRank.Pair;
        int cmp = Compare(player, dealer, board);

        string playerDesc = HandEvaluator.DescribeHand(player7);
        string dealerDesc = HandEvaluator.DescribeHand(dealer7);
        string versus = cmp > 0 ? $"The player's {playerDesc} beats the dealer's {dealerDesc}"
                      : cmp < 0 ? $"The dealer's {dealerDesc} beats the player's {playerDesc}"
                      : $"The player and dealer tie ({playerDesc})";

        HdPay[] pays = new HdPay[5];
        string[] why = new string[5];

        // ---- Raise: 1 to 1; a tie pushes -----------------------------------
        int w = wagers[HdPayoutRound.Raise];
        if (cmp == 0) { pays[0] = HdPay.Push; why[0] = $"{versus}: the Raise pushes."; }
        else if (cmp > 0) { pays[0] = HdPay.Win(w * 100); why[0] = $"{versus}: 1 to 1. {Money(w)} × 1 = {Money(w)}."; }
        else { pays[0] = HdPay.Lose; why[0] = $"{versus}: the Raise loses."; }

        // ---- Ante: 1 to 1 when the dealer opens; pushes when the dealer
        // doesn't open (pair or better needed) and on a tie ---------------
        w = wagers[HdPayoutRound.Ante];
        string openText = opens ? "The dealer opens (pair or better)" : $"The dealer doesn't open ({HandEvaluator.HandRankName(dr)}, needs a pair or better)";
        if (cmp == 0) { pays[1] = HdPay.Push; why[1] = $"{versus}: the Ante pushes."; }
        else if (!opens) { pays[1] = HdPay.Push; why[1] = $"{openText}: the Ante pushes."; }
        else if (cmp > 0) { pays[1] = HdPay.Win(w * 100); why[1] = $"{openText} and the player wins: 1 to 1. {Money(w)} × 1 = {Money(w)}."; }
        else { pays[1] = HdPay.Lose; why[1] = $"{openText} and the player loses: the Ante loses."; }

        // ---- Odds: pays only when the player wins with a Straight or
        // better (less pushes); a loss with a Straight or better pays the
        // Bad Beat table; a tie pushes ---------------------------------------
        w = wagers[HdPayoutRound.Odds];
        string handName = HandEvaluator.HandRankName(pr);
        if (cmp == 0)
        {
            pays[2] = HdPay.Push; why[2] = $"{versus}: the Odds pushes.";
        }
        else if (cmp > 0)
        {
            (int num, int den) = pr switch
            {
                HandRank.RoyalFlush => (500, 1),
                HandRank.StraightFlush => (50, 1),
                HandRank.FourOfAKind => (10, 1),
                HandRank.FullHouse => (3, 1),
                HandRank.Flush => (3, 2),
                HandRank.Straight => (1, 1),
                _ => (0, 1)
            };
            if (num == 0)
            {
                pays[2] = HdPay.Push;
                why[2] = $"The player wins with {WithArticle(handName)} (the Odds pays on a Straight or better): the Odds pushes.";
            }
            else
            {
                int cents = w * 100 * num / den;
                pays[2] = HdPay.Win(cents);
                string math = den == 1 ? $"{Money(w)} × {num} = {MoneyCents(cents)}" : $"{Money(w)} ÷ {den} × {num} = {MoneyCents(cents)}";
                why[2] = $"The player wins with {WithArticle(handName)}: {num} to {den}. {math}.";
            }
        }
        else
        {
            int bad = pr switch
            {
                HandRank.StraightFlush => 500,
                HandRank.FourOfAKind => 25,
                HandRank.FullHouse => 6,
                HandRank.Flush => 5,
                HandRank.Straight => 4,
                _ => 0
            };
            if (bad > 0)
            {
                pays[2] = HdPay.Win(w * 100 * bad);
                why[2] = $"Bad Beat: the player loses holding {WithArticle(handName)}: {bad} to 1. {Money(w)} × {bad} = {Money(w * bad)}.";
            }
            else
            {
                pays[2] = HdPay.Lose;
                why[2] = $"The player loses with {WithArticle(handName)} (no Bad Beat below a Straight): the Odds loses.";
            }
        }

        // ---- Trips Plus: player's best 5 of 7 -----------------------------
        w = wagers[HdPayoutRound.Trips];
        int trips = pr switch
        {
            HandRank.RoyalFlush => 100,
            HandRank.StraightFlush => 40,
            HandRank.FourOfAKind => 30,
            HandRank.FullHouse => 8,
            HandRank.Flush => 7,
            HandRank.Straight => 4,
            HandRank.ThreeOfAKind => 3,
            _ => 0
        };
        string best5 = string.Join(" ", HandEvaluator.BestFiveCards(player7).OrderByDescending(c => (int)c.Rank).Select(Short));
        pays[3] = trips > 0 ? HdPay.Win(w * 100 * trips) : HdPay.Lose;
        why[3] = trips > 0
            ? $"Best 5 of the player's 7 cards: {best5}, {WithArticle(handName)}: {trips} to 1. {Money(w)} × {trips} = {Money(w * trips)}."
            : $"Best 5 of the player's 7 cards: {playerDesc} (needs Three of a Kind or better): the wager loses.";

        // ---- Pocket Bonus: player's 2 hole cards --------------------------
        w = wagers[HdPayoutRound.Pocket];
        (int pocket, string pocketName) = PocketBonus(player);
        string two = $"{Short(player[0])} {Short(player[1])}";
        pays[4] = pocket > 0 ? HdPay.Win(w * 100 * pocket) : HdPay.Lose;
        why[4] = pocket > 0
            ? $"{two} is {pocketName}: {pocket} to 1. {Money(w)} × {pocket} = {Money(w * pocket)}."
            : $"{two} isn't a pair or Ace-Face (A-K, A-Q, A-J): the wager loses.";

        return new HdPayoutRound
        {
            DealerCards = dealer,
            PlayerCards = player,
            Board = board,
            DealerHand = HandEvaluator.HandRankName(dr),
            PlayerHand = handName,
            DealerOpens = opens,
            Wagers = wagers,
            RaiseTiming = raiseTiming,
            Pays = pays,
            Why = why
        };
    }

    /// <summary>Player vs dealer on the best 5 of 7: 1 player wins, -1 dealer wins, 0 tie.</summary>
    public static int Compare(Card[] player, Card[] dealer, Card[] board)
    {
        long p = HandEvaluator.HandScore(HandEvaluator.BestFiveCards(player.Concat(board).ToList()));
        long d = HandEvaluator.HandScore(HandEvaluator.BestFiveCards(dealer.Concat(board).ToList()));
        return p.CompareTo(d);
    }

    private static (int To1, string Name) PocketBonus(Card[] hole)
    {
        Card a = hole[0], b = hole[1];
        if (a.Rank == b.Rank)
            return a.Rank == Rank.Ace ? (25, "Pocket Aces") : (5, "a Pair");

        bool hasAce = a.Rank == Rank.Ace || b.Rank == Rank.Ace;
        Rank other = a.Rank == Rank.Ace ? b.Rank : a.Rank;
        if (hasAce && other is Rank.King or Rank.Queen or Rank.Jack)
            return a.Suit == b.Suit ? (20, "Ace-Face suited") : (10, "Ace-Face");
        return (0, "");
    }

    // ---- Real-world betting patterns ------------------------------------

    // The amounts seen most on the floor.
    // Ante (and Odds): $5, 10, 15, 20, 25, 50 -- mostly $5-$10, then
    // $15-$25, then $50
    private static int AnteWager(Random rng) =>
        Weighted(rng, (5, 23), (10, 22), (15, 12), (20, 12), (25, 11), (50, 20));

    // Trips Plus / Pocket Bonus: $5, 10, 15, 20, 25, 50, 75, 100 -- mostly
    // $5-$10, sometimes $15-$25, rarely $50-$100
    private static int SideWager(Random rng) =>
        Weighted(rng, (5, 35), (10, 35), (15, 9), (20, 8), (25, 8), (50, 2), (75, 1), (100, 2));

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

    // Raise: 3x before the flop, 2x after the flop, 1x after the river
    private static (int Mult, string Timing) RaiseTiming(Random rng)
    {
        double r = rng.NextDouble();
        if (r < 0.45) return (3, "3x pre-flop");
        if (r < 0.80) return (2, "2x after the flop");
        return (1, "1x after the river");
    }

    // ---- Helpers ----------------------------------------------------------

    public static string Money(int dollars) => $"${dollars:N0}";

    /// <summary>"$15" or "$7.50"</summary>
    public static string MoneyCents(int cents) =>
        cents % 100 == 0 ? $"${cents / 100:N0}" : $"${cents / 100.0:N2}";

    private static string WithArticle(string hand) =>
        hand.StartsWith("Three") || hand.StartsWith("Four") || hand.StartsWith("Two") || hand == "High Card"
            ? hand
            : "a " + hand;

    private static string Short(Card c)
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
}

using DealerSchoolTrainerWeb.Core;
using DealerSchoolTrainerWeb.Games.HoldemTrainer;
using DealerSchoolTrainerWeb.Games.PaiGowTrainer;
using DealerSchoolTrainerWeb.Games.ThreeCardTrainer;

namespace DealerSchoolTrainerWeb.Games.ProgressiveTrainer;

// Progressives: the table's progressive touchscreen routine, one game at
// a time. Each game has two progressive wagers (the touchscreen's two chip
// rows): $5 and $1 for Blackjack, Three Card Prime and Hold'em, Emperor's
// Treasure $1 and Insurance $1 for Pai Gow. Pay tables are the house's
// base tables (no Dynamic Pays multipliers). A metered pay (a % of the
// meter) needs a supervisor; nothing is entered for it.

public enum ProgGame { Blackjack, ThreeCard, PaiGow, Holdem }

/// <summary>One pay table line: a dollar amount, or Meter = "100%" / "50%" / "25%".</summary>
public sealed record ProgRow(string Name, int Pay, string? Meter = null)
{
    public bool IsMetered => Meter is not null;
    public string PayText => Meter ?? $"${Pay:N0}";
}

/// <summary>One of the game's two progressive wagers.</summary>
public sealed record ProgWager(
    string Header,     // touchscreen pay table title, e.g. "21+3 $5", "HUH $1", "ET $1"
    string ChipTop,    // on the touchscreen chip, e.g. "$5"
    string ChipSub,    // under it, e.g. "Jackpot", "Emperor"
    string Tag,        // the wager tag on the felt, e.g. "$5", "ET $1"
    string HeaderClass,// "pr-h5" (orange) or "pr-h1" (blue), like the real screen
    ProgRow[] Rows);

/// <summary>One dealt hand: the cards shown and, per wager, the pay table line it hits (-1 = none).</summary>
public sealed class ProgHand
{
    public List<(string Code, string Name)> Dealer { get; init; } = new(); // Blackjack: the up card
    public List<(string Code, string Name)> Board { get; init; } = new();  // Hold'em: the 5 community cards
    public List<(string Code, string Name)> Player { get; init; } = new();
    public required int Seat { get; init; }          // 1-6
    public required bool[] Placed { get; init; }     // which of the 2 wagers the player made
    public required int[] Row { get; init; }         // pay table line hit per wager, -1 = none
    public required string HandName { get; init; }   // e.g. "Three of a Kind", "Two Pair"

    /// <summary>The wagers that need paying: placed, and the hand hits their table.</summary>
    public IEnumerable<int> Winners => Enumerable.Range(0, 2).Where(w => Placed[w] && Row[w] >= 0);
}

public sealed class ProgGameDef
{
    public required ProgGame Game { get; init; }
    public required string Key { get; init; }        // ?open= and the summary key
    public required string Name { get; init; }       // "Blackjack"
    public required string Title { get; init; }      // "Blackjack Progressive"
    public required string Description { get; init; }
    public required string FeltLabel { get; init; }  // over the player's cards
    public required ProgWager[] Wagers { get; init; }
}

public static class ProgressiveGames
{
    // Practice boost: about half the hands qualify (naturally far fewer,
    // e.g. about 1 in 36 for Hold'em's Full House or better), so the
    // payout routine gets practised and END GAME still has to be decided
    private const double QualifyRate = 0.5;

    public static readonly ProgGameDef[] All =
    {
        new()
        {
            Game = ProgGame.Blackjack, Key = "bj", Name = "Blackjack", Title = "Blackjack Progressive",
            Description = "21+3 Progressive Jackpot: the player's 2 cards and the dealer's up card.",
            FeltLabel = "DEALER",
            Wagers = new[]
            {
                new ProgWager("21+3 $5", "$5", "Jackpot", "$5", "pr-h5", new ProgRow[]
                {
                    new("3 of a Kind Aces (Suited)", 0, "100%"), new("3 of a Kind (Suited)", 500),
                    new("Straight Flush", 100), new("3 of a Kind (Offsuit)", 50), new("Straight", 10)
                }),
                new ProgWager("21+3 $1", "$1", "Jackpot", "$1", "pr-h1", new ProgRow[]
                {
                    new("3 of a Kind Aces (Suited)", 0, "100%"), new("3 of a Kind (Suited)", 100),
                    new("Straight Flush", 20), new("3 of a Kind (Offsuit)", 10), new("Straight", 2)
                })
            }
        },
        new()
        {
            Game = ProgGame.ThreeCard, Key = "tc", Name = "Three Card Prime", Title = "Three Card Progressive",
            Description = "Mini Royal Progressive: the player's 3 cards.",
            FeltLabel = "PLAYER",
            Wagers = new[]
            {
                new ProgWager("3CP $5", "$5", "Jackpot", "$5", "pr-h5", new ProgRow[]
                {
                    new("Mini-Royal (Diamonds)", 0, "100%"), new("Mini-Royal (Other)", 1000),
                    new("Straight Flush", 100), new("Three of a Kind", 50), new("Straight", 10)
                }),
                new ProgWager("3CP $1", "$1", "Jackpot", "$1", "pr-h1", new ProgRow[]
                {
                    new("Mini-Royal (Spades)", 0, "100%"), new("Mini-Royal (Other)", 200),
                    new("Straight Flush", 20), new("Three of a Kind", 10), new("Straight", 2)
                })
            }
        },
        new()
        {
            Game = ProgGame.PaiGow, Key = "pg", Name = "Pai Gow", Title = "Pai Gow Progressive",
            Description = "Emperor's Treasure and Pai Gow Insurance progressives: the player's 7 cards.",
            FeltLabel = "PLAYER",
            Wagers = new[]
            {
                new ProgWager("ET $1", "$1", "Emperor", "ET $1", "pr-h5", new ProgRow[]
                {
                    new("7-Card SF (no Joker)", 0, "100%"), new("Royal + A-K Suited", 0, "50%"),
                    new("7-Card Straight Flush", 0, "25%"), new("Five Aces", 1000), new("Royal Flush", 175),
                    new("Straight Flush", 150), new("Four of a Kind", 75), new("Full House", 5)
                }),
                new ProgWager("INS $1", "$1", "Insurance", "Ins $1", "pr-h1", new ProgRow[]
                {
                    new("9-High Same Color", 0, "100%"), new("9-High Pai Gow", 250),
                    new("10-High Pai Gow", 125), new("Jack-High Pai Gow", 30), new("Queen-High Pai Gow", 7)
                })
            }
        },
        new()
        {
            Game = ProgGame.Holdem, Key = "hd", Name = "Heads-Up Hold'em", Title = "Hold'em Progressive",
            Description = "Hold'em progressive: the player's best 5 of 7 cards, or a Royal Flush on the board.",
            FeltLabel = "BOARD",
            Wagers = new[]
            {
                new ProgWager("HUH $5", "$5", "Jackpot", "$5", "pr-h5", new ProgRow[]
                {
                    new("Royal Flush", 0, "100%"), new("Community Royal", 1000), new("Straight Flush", 400),
                    new("Four-of-a-Kind", 200), new("Full House", 20)
                }),
                new ProgWager("HUH $1", "$1", "Jackpot", "$1", "pr-h1", new ProgRow[]
                {
                    new("Royal Flush", 0, "100%"), new("Community Royal", 200), new("Straight Flush", 80),
                    new("Four-of-a-Kind", 40), new("Full House", 4)
                })
            }
        }
    };

    public static ProgGameDef? ByKey(string? key) => All.FirstOrDefault(g => g.Key == key);

    // ======================================================================
    // Dealing
    // ======================================================================

    public static ProgHand Deal(ProgGameDef def, Random rng)
    {
        bool qualify = rng.NextDouble() < QualifyRate;
        for (int attempt = 0; attempt < 50; attempt++)
        {
            ProgHand? h = def.Game switch
            {
                ProgGame.Blackjack => DealBlackjack(rng, qualify),
                ProgGame.ThreeCard => DealThreeCard(rng, qualify),
                ProgGame.PaiGow => DealPaiGow(rng, qualify),
                _ => DealHoldem(rng, qualify)
            };
            if (h is not null) return h;
        }
        // Fallback: whatever comes (never expected)
        return def.Game switch
        {
            ProgGame.Blackjack => DealBlackjack(rng, false)!,
            ProgGame.ThreeCard => DealThreeCard(rng, false)!,
            ProgGame.PaiGow => DealPaiGow(rng, false)!,
            _ => DealHoldem(rng, false)!
        };
    }

    // Wagers made: both about 40% of hands, otherwise one or the other.
    // mustHave = a wager that has to be among them (the one the hand wins).
    private static bool[] PickWagers(Random rng, int mustHave = -1)
    {
        double roll = rng.NextDouble();
        bool[] placed = roll < 0.4 ? new[] { true, true } : roll < 0.7 ? new[] { true, false } : new[] { false, true };
        if (mustHave >= 0) placed[mustHave] = true;
        return placed;
    }

    private static int Weighted(Random rng, params int[] weights)
    {
        int roll = rng.Next(weights.Sum());
        for (int i = 0; i < weights.Length; i++)
        {
            if (roll < weights[i]) return i;
            roll -= weights[i];
        }
        return weights.Length - 1;
    }

    private static readonly Suit[] Suits = { Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades };

    private static Card C(int rank, Suit s) => new((Rank)(rank == 1 ? 14 : rank), s);
    private static Suit AnySuit(Random rng) => Suits[rng.Next(4)];
    private static (string, string) Show(Card c) => (c.Code, c.Name);

    private static void Shuffle<T>(IList<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ---- Blackjack: 21+3 on the player's 2 cards + the dealer's up card,
    // dealt from a 6-deck shoe (so suited three of a kind can happen) ------

    /// <summary>Pay table line (same lines on both wagers), -1 = no pay.</summary>
    public static int BlackjackRow(IReadOnlyList<Card> c)
    {
        bool trips = c[0].Rank == c[1].Rank && c[1].Rank == c[2].Rank;
        bool suited = c[0].Suit == c[1].Suit && c[1].Suit == c[2].Suit;
        if (trips && suited) return c[0].Rank == Rank.Ace ? 0 : 1;
        if (trips) return 3;
        int[] r = c.Select(x => (int)x.Rank).OrderBy(x => x).ToArray();
        bool straight = (r[1] == r[0] + 1 && r[2] == r[1] + 1) || (r[0] == 2 && r[1] == 3 && r[2] == 14);
        if (straight && suited) return 2;
        if (straight) return 4;
        return -1;
    }

    private static string BlackjackName(IReadOnlyList<Card> c)
    {
        int row = BlackjackRow(c);
        if (row >= 0) return All[0].Wagers[0].Rows[row].Name;
        bool suited = c[0].Suit == c[1].Suit && c[1].Suit == c[2].Suit;
        bool pair = c[0].Rank == c[1].Rank || c[1].Rank == c[2].Rank || c[0].Rank == c[2].Rank;
        return suited ? "Flush" : pair ? "Pair" : "High Card";
    }

    private static ProgHand? DealBlackjack(Random rng, bool qualify)
    {
        List<Card> cards;
        int target = -1;
        if (qualify)
        {
            target = Weighted(rng, 6, 12, 16, 30, 36);
            Suit s = AnySuit(rng);
            int r = rng.Next(2, 15);
            int start = rng.Next(1, 13); // A-2-3 up to Q-K-A
            cards = target switch
            {
                0 => new() { C(14, s), C(14, s), C(14, s) },
                1 => new() { C(r == 14 ? 13 : r, s), C(r == 14 ? 13 : r, s), C(r == 14 ? 13 : r, s) },
                3 => new() { C(r, AnySuit(rng)), C(r, AnySuit(rng)), C(r, AnySuit(rng)) },
                2 => new() { C(start, s), C(start + 1, s), C(start + 2, s) },
                _ => new() { C(start, AnySuit(rng)), C(start + 1, AnySuit(rng)), C(start + 2, AnySuit(rng)) }
            };
        }
        else
        {
            cards = new() { C(rng.Next(2, 15), AnySuit(rng)), C(rng.Next(2, 15), AnySuit(rng)), C(rng.Next(2, 15), AnySuit(rng)) };
        }

        int row = BlackjackRow(cards);
        if (row != target) return null; // e.g. "offsuit" trips that came out suited: deal again

        Shuffle(cards, rng);
        int seat = rng.Next(1, 7);
        return new ProgHand
        {
            Dealer = new() { Show(cards[2]) },
            Player = new() { Show(cards[0]), Show(cards[1]) },
            Seat = seat,
            Placed = PickWagers(rng),
            Row = new[] { row, row },
            HandName = BlackjackName(cards)
        };
    }

    // ---- Three Card Prime: Mini Royal on the player's 3 cards -------------

    /// <summary>Pay table line for wager 0 ($5) or 1 ($1), -1 = no pay.</summary>
    public static int ThreeCardRow(IReadOnlyList<Card> c, int wager)
    {
        ThreeCardCategory cat = ThreeCardEvaluator.GetCategory(c);
        if (cat == ThreeCardCategory.StraightFlush)
        {
            bool mini = c.Any(x => x.Rank == Rank.Ace) && c.Any(x => x.Rank == Rank.King) && c.Any(x => x.Rank == Rank.Queen);
            if (mini)
            {
                // The jackpot suit differs: Diamonds on the $5, Spades on the $1
                Suit jackpot = wager == 0 ? Suit.Diamonds : Suit.Spades;
                return c[0].Suit == jackpot ? 0 : 1;
            }
            return 2;
        }
        return cat switch
        {
            ThreeCardCategory.ThreeOfAKind => 3,
            ThreeCardCategory.Straight => 4,
            _ => -1
        };
    }

    private static ProgHand? DealThreeCard(Random rng, bool qualify)
    {
        Deck deck = new();
        deck.Shuffle();
        List<Card> cards;
        if (qualify)
        {
            int target = Weighted(rng, 14, 16, 30, 40); // Mini-Royal, SF, Trips, Straight
            // Mini-Royal: half the time in a jackpot suit (Diamonds / Spades)
            Suit s = target == 0 && rng.Next(2) == 0 ? (rng.Next(2) == 0 ? Suit.Diamonds : Suit.Spades) : AnySuit(rng);
            int start = target == 0 ? 12 : rng.Next(1, 12); // SF/Straight: A-2-3 up to J-Q-K
            int r = rng.Next(2, 15);
            List<Suit> three = Suits.OrderBy(_ => rng.Next()).Take(3).ToList();
            cards = target switch
            {
                0 or 1 => new() { C(start, s), C(start + 1, s), C(start + 2, s) },
                2 => new() { C(r, three[0]), C(r, three[1]), C(r, three[2]) },
                _ => new() { C(start, AnySuit(rng)), C(start + 1, AnySuit(rng)), C(start + 2, AnySuit(rng)) }
            };
            if (target == 3 && ThreeCardEvaluator.GetCategory(cards) != ThreeCardCategory.Straight) return null;
        }
        else
        {
            cards = new() { deck.Draw(), deck.Draw(), deck.Draw() };
            if (ThreeCardRow(cards, 0) >= 0) return null;
        }

        Shuffle(cards, rng);
        int[] rows = { ThreeCardRow(cards, 0), ThreeCardRow(cards, 1) };
        return new ProgHand
        {
            Player = cards.Select(Show).ToList(),
            Seat = rng.Next(1, 7),
            Placed = PickWagers(rng),
            Row = rows,
            HandName = rows[0] >= 0 ? (rows[0] <= 1 ? "Mini-Royal" : All[1].Wagers[0].Rows[rows[0]].Name)
                     : ThreeCardEvaluator.GetCategory(cards) switch
                     {
                         ThreeCardCategory.Flush => "Flush",
                         ThreeCardCategory.Pair => "Pair",
                         _ => "High Card"
                     }
        };
    }

    // ---- Hold'em: the player's best 5 of 7; a Royal on the board alone is
    // a Community Royal (its own hand, not the player's Royal Flush) -------

    /// <summary>Pay table line (same lines on both wagers), -1 = no pay.</summary>
    public static int HoldemRow(IReadOnlyList<Card> hole, IReadOnlyList<Card> board)
    {
        if (HandEvaluator.BestHandRank(board) == HandRank.RoyalFlush) return 1;
        HandRank best = HandEvaluator.BestHandRank(hole.Concat(board).ToList());
        return best switch
        {
            HandRank.RoyalFlush => 0,
            HandRank.StraightFlush => 2,
            HandRank.FourOfAKind => 3,
            HandRank.FullHouse => 4,
            _ => -1
        };
    }

    private static ProgHand? DealHoldem(Random rng, bool qualify)
    {
        Deck deck = new();
        deck.Shuffle();
        List<Card> pool = new();
        int target = -1;
        if (qualify)
        {
            target = Weighted(rng, 6, 6, 12, 26, 50); // Royal, Community Royal, SF, Quads, Full House
            Suit s = AnySuit(rng);
            int a = rng.Next(2, 15), b;
            do b = rng.Next(2, 15); while (b == a);
            List<Suit> ss = Suits.OrderBy(_ => rng.Next()).ToList();
            pool = target switch
            {
                0 or 1 => new() { C(10, s), C(11, s), C(12, s), C(13, s), C(14, s) },
                2 => Enumerable.Range(rng.Next(1, 10), 5).Select(r => C(r, s)).ToList(), // A-5 up to 9-K
                3 => ss.Select(x => C(a, x)).ToList(),
                _ => new() { C(a, ss[0]), C(a, ss[1]), C(a, ss[2]), C(b, ss[0]), C(b, ss[3]) }
            };
        }

        // Fill to 7 from the rest of the deck
        List<Card> rest = new();
        while (deck.RemainingCount > 0)
        {
            Card c = deck.Draw();
            if (!pool.Any(p => p.Rank == c.Rank && p.Suit == c.Suit)) rest.Add(c);
        }
        List<Card> seven = pool.Concat(rest.Take(7 - pool.Count)).ToList();

        List<Card> hole, board;
        if (target == 1)
        {
            board = seven.Take(5).ToList();
            hole = seven.Skip(5).ToList();
        }
        else
        {
            Shuffle(seven, rng);
            hole = seven.Take(2).ToList();
            board = seven.Skip(2).ToList();
        }
        Shuffle(board, rng);

        int row = HoldemRow(hole, board);
        if (row != target) return null;

        HandRank best = HandEvaluator.BestHandRank(hole.Concat(board).ToList());
        return new ProgHand
        {
            Board = board.Select(Show).ToList(),
            Player = hole.Select(Show).ToList(),
            Seat = rng.Next(1, 7),
            Placed = PickWagers(rng),
            Row = new[] { row, row },
            HandName = row >= 0 ? All[3].Wagers[0].Rows[row].Name : HandEvaluator.HandRankName(best)
        };
    }

    // ---- Pai Gow: the player's 7 cards (53-card deck with the Joker) -----

    private const string PgRanks = "23456789TJQKA";
    private static readonly string[] PgSuits = { "S", "H", "D", "C" };

    private static PaiGowCard Pg(int rank, string suit) =>
        new(PgRanks[(rank == 1 ? 14 : rank) - 2].ToString(), suit);

    private static bool SameCard(PaiGowCard a, PaiGowCard b) => a.Rank == b.Rank && a.Suit == b.Suit;

    /// <summary>Emperor's Treasure progressive line, -1 = no pay.</summary>
    public static int EmperorsRow(List<PaiGowCard> cards)
    {
        PaiGowSolver s = new();
        s.SetCards(cards);
        if (s.IsSevenCardStraightFlushNoJoker()) return 0;
        if (s.IsRoyalFlushPlusAceKingSuited()) return 1;
        if (s.IsSevenCardStraightFlushWithJoker()) return 2;
        return s.BestSevenCardCategoryName() switch
        {
            "Five Aces" => 3,
            "Royal Flush" => 4,
            "Straight Flush" => 5,
            "Four of a Kind" => 6,
            "Full House" => 7,
            _ => -1
        };
    }

    /// <summary>Insurance progressive line: a Pai Gow (no pair, straight or flush) Queen high or lower, -1 = no pay.</summary>
    public static int InsuranceRow(List<PaiGowCard> cards)
    {
        PaiGowSolver s = new();
        s.SetCards(cards);
        if (!s.QualifiesInsurance()) return -1;
        if (cards.Any(c => c.IsJoker)) return -1; // the Joker counts as an Ace: Ace high
        int top = cards.Max(c => c.RankOrder);
        bool oneColor = cards.All(c => c.Suit is "H" or "D") || cards.All(c => c.Suit is "S" or "C");
        return top switch
        {
            9 => oneColor ? 0 : 1,
            10 => 2,
            11 => 3,
            12 => 4,
            _ => -1
        };
    }

    private static ProgHand? DealPaiGow(Random rng, bool qualify)
    {
        List<PaiGowCard> deck = new();
        foreach (string su in PgSuits)
            foreach (char r in PgRanks) deck.Add(new PaiGowCard(r.ToString(), su));
        deck.Add(new PaiGowCard("JOKER", ""));
        Shuffle(deck, rng);

        List<PaiGowCard> pool = new();
        int wager = -1, target = -1;
        if (qualify)
        {
            wager = rng.NextDouble() < 0.55 ? 0 : 1;
            string s = PgSuits[rng.Next(4)];
            string t = PgSuits.Where(x => x != s).ElementAt(rng.Next(3));
            if (wager == 0)
            {
                target = Weighted(rng, 2, 2, 2, 3, 5, 6, 30, 50);
                int start = rng.Next(1, 9); // 7-card runs: A-7 up to 8-A
                int gap = rng.Next(1, 6);   // the card the Joker stands in for (not an end card)
                int a = rng.Next(2, 15), b;
                do b = rng.Next(2, 15); while (b == a);
                List<string> ss = PgSuits.OrderBy(_ => rng.Next()).ToList();
                pool = target switch
                {
                    0 => Enumerable.Range(start, 7).Select(r => Pg(r, s)).ToList(),
                    1 => new() { Pg(10, s), Pg(11, s), Pg(12, s), Pg(13, s), Pg(14, s), Pg(14, t), Pg(13, t) },
                    2 => Enumerable.Range(start, 7).Where((_, i) => i != gap).Select(r => Pg(r, s))
                                   .Append(new PaiGowCard("JOKER", "")).ToList(),
                    3 => PgSuits.Select(x => Pg(14, x)).Append(new PaiGowCard("JOKER", "")).ToList(),
                    4 => new() { Pg(10, s), Pg(11, s), Pg(12, s), Pg(13, s), Pg(14, s) },
                    5 => Enumerable.Range(rng.Next(1, 10), 5).Select(r => Pg(r, s)).ToList(),
                    6 => ss.Select(x => Pg(a, x)).ToList(),
                    _ => new() { Pg(a, ss[0]), Pg(a, ss[1]), Pg(a, ss[2]), Pg(b, ss[0]), Pg(b, ss[3]) }
                };
            }
            else
            {
                // A 7-card Pai Gow topped by 9, 10, J or Q, no Joker
                target = Weighted(rng, 8, 12, 20, 28, 32);
                int top = target switch { 0 or 1 => 9, 2 => 10, 3 => 11, _ => 12 };
                List<int> lower = Enumerable.Range(2, top - 2).OrderBy(_ => rng.Next()).Take(6).ToList();
                List<int> ranks = lower.Append(top).ToList();
                string[] colorSuits = rng.Next(2) == 0 ? new[] { "S", "C" } : new[] { "H", "D" };
                pool = ranks.Select((r, i) => Pg(r, target == 0 ? colorSuits[i % 2] : PgSuits[rng.Next(4)])).ToList();
                if (target == 1 && InsuranceRow(pool) == 0) return null; // came out one color: deal again
            }
        }

        List<PaiGowCard> seven = pool.ToList();
        foreach (PaiGowCard c in deck)
        {
            if (seven.Count == 7) break;
            if (!seven.Any(p => SameCard(p, c))) seven.Add(c);
        }
        Shuffle(seven, rng);

        int[] rows = { EmperorsRow(seven), InsuranceRow(seven) };
        if (qualify ? rows[wager] != target : rows[0] >= 0 || rows[1] >= 0) return null;

        PaiGowSolver solver = new();
        solver.SetCards(seven);
        // Pairs/trips grouped, then high to low, like the other Pai Gow modes
        List<PaiGowCard> shown = seven
            .OrderByDescending(c => seven.Count(x => !x.IsJoker && x.Rank == c.Rank && !c.IsJoker))
            .ThenByDescending(c => c.IsJoker ? 15 : c.RankOrder)
            .ToList();

        return new ProgHand
        {
            Player = shown.Select(c => (c.ImageCode, c.Name)).ToList(),
            Seat = rng.Next(1, 7),
            Placed = PickWagers(rng, wager),
            Row = rows,
            HandName = rows[0] >= 0 ? All[2].Wagers[0].Rows[rows[0]].Name
                     : rows[1] >= 0 ? All[2].Wagers[1].Rows[rows[1]].Name
                     : solver.BestSevenCardCategoryName() is "High Card" ? "Ace or King high Pai Gow"
                     : solver.BestSevenCardCategoryName()
        };
    }
}

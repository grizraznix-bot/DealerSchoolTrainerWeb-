namespace DealerSchoolTrainerWeb.Games.PaiGowTrainer;

/// <summary>
/// Hand Setting's Advanced difficulty: deals 7-card hands that need one
/// of the House Way's special rules -- the hands that come up rarely in
/// plain random deals but must be set correctly at the table. Each kind
/// is built on purpose (its key cards placed, the rest filled at random)
/// and then checked with the solver, so the hand really is that kind.
/// If a hand also falls under another rule, that's fine: it's still a
/// hand that takes thought, and the solver decides the correct split.
/// </summary>
public static class PaiGowTrickyHands
{
    public enum Kind
    {
        TripsWithStraightOrFlush,
        AceTripsWithStraightOrFlush,
        StraightAndFlush,
        JokerStraightOrFlush,
        TwoPairWithStraightOrFlush,
        TwoPairWithAce,
        FullHouseExtraPair,
        FullHouseTwosAceKing,
        QuadsSevenToTen,
        TwoTrips
    }

    private static readonly string[] Suits = { "S", "H", "D", "C" };
    private static readonly Kind[] AllKinds = Enum.GetValues<Kind>();

    /// <summary>A tricky hand of a random kind, its 7 cards in random order.</summary>
    public static List<PaiGowCard> Deal(Random rng, out Kind kind)
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            kind = AllKinds[rng.Next(AllKinds.Length)];
            List<PaiGowCard>? cards = TryBuild(kind, rng);
            if (cards is not null && Matches(kind, cards))
            {
                // Random order, like a real deal
                return cards.OrderBy(_ => rng.Next()).ToList();
            }
        }

        // Not expected to happen; a plain deal keeps the game going
        kind = Kind.TwoPairWithAce;
        PaiGowDeck deck = new();
        return Enumerable.Range(0, 7).Select(_ => deck.Draw()).ToList();
    }

    /// <summary>True when the 7 cards really are this kind (checked with the solver).</summary>
    public static bool Matches(Kind kind, IReadOnlyList<PaiGowCard> cards)
    {
        PaiGowSolver solver = new();
        solver.SetCards(cards);
        var t = solver.Traits();
        bool special = t.Straight || t.Flush;

        return kind switch
        {
            Kind.TripsWithStraightOrFlush => t.Trips == 1 && t.QuadRank == 0 && !t.FullHouse && special,
            Kind.AceTripsWithStraightOrFlush => t.Trips == 1 && TripRankIs(cards, "A") && !t.FullHouse && special,
            Kind.StraightAndFlush => t.Straight && t.Flush,
            Kind.JokerStraightOrFlush => t.Joker && special,
            Kind.TwoPairWithStraightOrFlush => t.Pairs == 2 && t.Trips == 0 && special,
            Kind.TwoPairWithAce => t.Pairs == 2 && t.Trips == 0 && t.QuadRank == 0 && HasSingleAce(cards),
            Kind.FullHouseExtraPair => t.FullHouse && t.Trips == 1 && t.Pairs == 2,
            Kind.FullHouseTwosAceKing => t.FullHouse && t.Trips == 1 && t.Pairs == 1 && CountRank(cards, "2") == 2
                                         && CountRank(cards, "A") == 1 && CountRank(cards, "K") == 1,
            Kind.QuadsSevenToTen => t.QuadRank >= 7 && t.QuadRank <= 10,
            Kind.TwoTrips => t.Trips == 2,
            _ => false
        };
    }

    // ------------------------------------------------------------------
    // Builders. Each places the cards that make the hand its kind, then
    // fills to 7 from what's left (no Joker unless the kind needs it).
    // ------------------------------------------------------------------

    private static List<PaiGowCard>? TryBuild(Kind kind, Random rng)
    {
        Pool pool = new(rng);

        switch (kind)
        {
            case Kind.TripsWithStraightOrFlush:
            case Kind.AceTripsWithStraightOrFlush:
            {
                string trip = kind == Kind.AceTripsWithStraightOrFlush ? "A" : RandomRank(rng, "A");
                List<PaiGowCard> hand = pool.TakeRank(trip, 3);
                if (rng.Next(2) == 0)
                {
                    // Flush: one of the trips' suits, 4 more of that suit
                    string suit = hand[rng.Next(3)].Suit;
                    hand.AddRange(pool.TakeSuit(suit, 4, excludeRanks: new[] { trip }));
                }
                else
                {
                    // Straight through the trips' rank
                    hand.AddRange(pool.TakeStraightThrough(trip, rng));
                }
                return hand.Count == 7 ? hand : null;
            }

            case Kind.StraightAndFlush:
            {
                int start = rng.Next(1, 11); // 1 = A-2-3-4-5 ... 10 = 10-J-Q-K-A
                string suit = Suits[rng.Next(4)];
                List<string> ranks = Enumerable.Range(start, 5).Select(RankName).ToList();
                // 3 of the straight in the flush suit, 2 in other suits
                List<int> inSuit = Enumerable.Range(0, 5).OrderBy(_ => rng.Next()).Take(3).ToList();
                List<PaiGowCard> hand = new();
                for (int i = 0; i < 5; i++)
                {
                    PaiGowCard? c = inSuit.Contains(i) ? pool.Take(ranks[i], suit) : pool.TakeOtherSuit(ranks[i], suit, rng);
                    if (c is null) return null;
                    hand.Add(c);
                }
                hand.AddRange(pool.TakeSuit(suit, 2, excludeRanks: ranks.ToArray()));
                return hand.Count == 7 ? hand : null;
            }

            case Kind.JokerStraightOrFlush:
            {
                List<PaiGowCard> hand = new() { pool.TakeJoker()! };
                if (rng.Next(2) == 0)
                {
                    string suit = Suits[rng.Next(4)];
                    hand.AddRange(pool.TakeSuit(suit, 4, excludeRanks: Array.Empty<string>()));
                }
                else
                {
                    // 4 of 5 straight ranks; the Joker fills the gap
                    int start = rng.Next(1, 11);
                    List<string> ranks = Enumerable.Range(start, 5).Select(RankName).ToList();
                    ranks.RemoveAt(rng.Next(5));
                    foreach (string r in ranks)
                    {
                        PaiGowCard? c = pool.TakeAnySuit(r, rng);
                        if (c is null) return null;
                        hand.Add(c);
                    }
                }
                hand.AddRange(pool.TakeRandom(7 - hand.Count));
                return hand.Count == 7 ? hand : null;
            }

            case Kind.TwoPairWithStraightOrFlush:
            {
                // A straight with two of its ranks paired
                int start = rng.Next(1, 11);
                List<string> ranks = Enumerable.Range(start, 5).Select(RankName).ToList();
                List<PaiGowCard> hand = new();
                foreach (string r in ranks)
                {
                    PaiGowCard? c = pool.TakeAnySuit(r, rng);
                    if (c is null) return null;
                    hand.Add(c);
                }
                foreach (string r in ranks.OrderBy(_ => rng.Next()).Take(2))
                {
                    PaiGowCard? c = pool.TakeAnySuit(r, rng);
                    if (c is null) return null;
                    hand.Add(c);
                }
                return hand;
            }

            case Kind.TwoPairWithAce:
            {
                List<string> pairRanks = OtherRanks(rng, 2, "A");
                List<PaiGowCard> hand = new();
                foreach (string r in pairRanks) hand.AddRange(pool.TakeRank(r, 2));
                hand.Add(pool.TakeAnySuit("A", rng)!);
                hand.AddRange(pool.TakeRandomExcluding(2, pairRanks.Append("A").ToArray()));
                return hand.Count == 7 ? hand : null;
            }

            case Kind.FullHouseExtraPair:
            {
                List<string> r = OtherRanks(rng, 3, null);
                List<PaiGowCard> hand = pool.TakeRank(r[0], 3);
                hand.AddRange(pool.TakeRank(r[1], 2));
                hand.AddRange(pool.TakeRank(r[2], 2));
                return hand;
            }

            case Kind.FullHouseTwosAceKing:
            {
                string trip = RandomRank(rng, "2", "A", "K");
                List<PaiGowCard> hand = pool.TakeRank(trip, 3);
                hand.AddRange(pool.TakeRank("2", 2));
                hand.Add(pool.TakeAnySuit("A", rng)!);
                hand.Add(pool.TakeAnySuit("K", rng)!);
                return hand;
            }

            case Kind.QuadsSevenToTen:
            {
                string quad = new[] { "7", "8", "9", "T" }[rng.Next(4)];
                List<PaiGowCard> hand = pool.TakeRank(quad, 4);
                // Half the time an Ace to play in front (keeps them together)
                if (rng.Next(2) == 0) hand.Add(pool.TakeAnySuit("A", rng)!);
                hand.AddRange(pool.TakeRandomExcluding(7 - hand.Count, new[] { quad, "A" }));
                return hand.Count == 7 ? hand : null;
            }

            case Kind.TwoTrips:
            {
                List<string> r = OtherRanks(rng, 2, null);
                List<PaiGowCard> hand = pool.TakeRank(r[0], 3);
                hand.AddRange(pool.TakeRank(r[1], 3));
                hand.AddRange(pool.TakeRandomExcluding(1, r.ToArray()));
                return hand.Count == 7 ? hand : null;
            }
        }

        return null;
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static readonly string[] Ranks = { "2", "3", "4", "5", "6", "7", "8", "9", "T", "J", "Q", "K", "A" };

    // 1 or 14 = A, 2..13 = 2..K
    private static string RankName(int n) => n == 1 || n == 14 ? "A" : Ranks[n - 2];

    private static string RandomRank(Random rng, params string[] exclude)
    {
        string[] choices = Ranks.Where(r => !exclude.Contains(r)).ToArray();
        return choices[rng.Next(choices.Length)];
    }

    private static List<string> OtherRanks(Random rng, int count, string? exclude) =>
        Ranks.Where(r => r != exclude).OrderBy(_ => rng.Next()).Take(count).ToList();

    private static int CountRank(IReadOnlyList<PaiGowCard> cards, string rank) => cards.Count(c => c.Rank == rank);

    private static bool TripRankIs(IReadOnlyList<PaiGowCard> cards, string rank) => CountRank(cards, rank) == 3;

    private static bool HasSingleAce(IReadOnlyList<PaiGowCard> cards) =>
        CountRank(cards, "A") == 1 && !cards.Any(c => c.IsJoker);

    /// <summary>The cards not yet used in the hand being built (one 53-card deck).</summary>
    private sealed class Pool
    {
        private readonly List<PaiGowCard> _left;
        private readonly Random _rng;

        public Pool(Random rng)
        {
            _rng = rng;
            _left = Ranks.SelectMany(r => Suits.Select(s => new PaiGowCard(r, s))).ToList();
            _left.Add(new PaiGowCard("JOKER", ""));
        }

        private PaiGowCard? Remove(Func<PaiGowCard, bool> match)
        {
            List<PaiGowCard> options = _left.Where(match).ToList();
            if (options.Count == 0) return null;
            PaiGowCard c = options[_rng.Next(options.Count)];
            _left.Remove(c);
            return c;
        }

        public PaiGowCard? Take(string rank, string suit) => Remove(c => c.Rank == rank && c.Suit == suit);

        public PaiGowCard? TakeAnySuit(string rank, Random rng) => Remove(c => c.Rank == rank);

        public PaiGowCard? TakeOtherSuit(string rank, string suit, Random rng) => Remove(c => c.Rank == rank && c.Suit != suit);

        public PaiGowCard? TakeJoker() => Remove(c => c.IsJoker);

        public List<PaiGowCard> TakeRank(string rank, int count)
        {
            List<PaiGowCard> taken = new();
            for (int i = 0; i < count; i++)
            {
                PaiGowCard? c = Remove(x => x.Rank == rank);
                if (c is not null) taken.Add(c);
            }
            return taken;
        }

        // count cards of one suit, all different ranks, none in excludeRanks
        public List<PaiGowCard> TakeSuit(string suit, int count, string[] excludeRanks)
        {
            List<PaiGowCard> taken = new();
            for (int i = 0; i < count; i++)
            {
                PaiGowCard? c = Remove(x => x.Suit == suit && !x.IsJoker
                                            && !excludeRanks.Contains(x.Rank)
                                            && !taken.Any(t => t.Rank == x.Rank));
                if (c is not null) taken.Add(c);
            }
            return taken;
        }

        // 4 more cards making a straight with the given rank in it
        public List<PaiGowCard> TakeStraightThrough(string rank, Random rng)
        {
            int n = rank == "A" ? (rng.Next(2) == 0 ? 1 : 14) : Array.IndexOf(Ranks, rank) + 2;
            int lowest = Math.Max(1, n - 4), highest = Math.Min(10, n);
            int start = rng.Next(lowest, highest + 1);
            List<PaiGowCard> taken = new();
            for (int v = start; v < start + 5; v++)
            {
                if (v == n || (n == 14 && v == 1) || (n == 1 && v == 14)) continue;
                PaiGowCard? c = Remove(x => x.Rank == RankName(v));
                if (c is not null) taken.Add(c);
            }
            return taken;
        }

        public List<PaiGowCard> TakeRandom(int count)
        {
            List<PaiGowCard> taken = new();
            for (int i = 0; i < count; i++)
            {
                PaiGowCard? c = Remove(x => !x.IsJoker);
                if (c is not null) taken.Add(c);
            }
            return taken;
        }

        public List<PaiGowCard> TakeRandomExcluding(int count, string[] excludeRanks)
        {
            List<PaiGowCard> taken = new();
            for (int i = 0; i < count; i++)
            {
                PaiGowCard? c = Remove(x => !x.IsJoker && !excludeRanks.Contains(x.Rank) && !taken.Any(t => t.Rank == x.Rank));
                if (c is not null) taken.Add(c);
            }
            return taken;
        }
    }
}

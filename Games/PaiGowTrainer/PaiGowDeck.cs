namespace DealerSchoolTrainerWeb.Games.PaiGowTrainer;

/// <summary>Direct translation of clsPaiGowDeck.cls.</summary>
public sealed class PaiGowDeck
{
    private const int DeckSize = 53;

    private readonly List<PaiGowCard> _cards = new(DeckSize);
    private int _nextIndex;
    private static readonly Random Rng = new();

    public PaiGowDeck()
    {
        BuildDeck();
        Shuffle();
    }

    private void BuildDeck()
    {
        _cards.Clear();

        string[] ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "T", "J", "Q", "K" };
        string[] suits = { "S", "H", "D", "C" };

        foreach (string s in suits)
        {
            foreach (string r in ranks)
            {
                _cards.Add(new PaiGowCard(r, s));
            }
        }

        _cards.Add(new PaiGowCard("JOKER", ""));
    }

    public void Shuffle()
    {
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = Rng.Next(i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }

        _nextIndex = 0;
    }

    public PaiGowCard Draw()
    {
        if (_nextIndex >= DeckSize)
            Shuffle();

        return _cards[_nextIndex++];
    }

    public int CardsRemaining => DeckSize - _nextIndex;

    /// <summary>
    /// Deals 7 cards from this shuffled deck, with the single real
    /// Joker's inclusion probability set explicitly — a weighted coin
    /// flip decides once, up front, whether the Joker is forced into
    /// this hand or forced out, rather than leaving it to the natural
    /// ~13.2% odds (7/53) of a plain shuffle-and-draw-7. There's still
    /// exactly one physical Joker in the deck; this doesn't add
    /// fictional extras, it just controls how often THIS one shows up
    /// in a dealt hand, so the trainer can dial up practice reps
    /// against Joker scenarios without breaking deck realism.
    /// Call this INSTEAD OF Draw() — it consumes the whole deck for
    /// one 7-card hand rather than drawing incrementally.
    /// </summary>
    public List<PaiGowCard> DealSevenCardsWithJokerBias(double jokerInclusionProbability)
    {
        PaiGowCard joker = _cards.First(c => c.IsJoker);
        List<PaiGowCard> nonJokerCards = _cards.Where(c => !c.IsJoker).ToList();

        // _cards is already shuffled (constructor calls Shuffle()), so
        // nonJokerCards preserves that shuffled order — no need to
        // reshuffle just because the Joker got filtered out of it.
        bool includeJoker = Rng.NextDouble() < jokerInclusionProbability;

        List<PaiGowCard> hand = new(7);
        if (includeJoker)
        {
            hand.Add(joker);
            hand.AddRange(nonJokerCards.Take(6));
        }
        else
        {
            hand.AddRange(nonJokerCards.Take(7));
        }

        return hand;
    }

    /// <summary>
    /// Deals TWO non-overlapping 7-card hands (player + dealer) from
    /// this SAME shuffled 53-card deck. Calling DealSevenCardsWithJokerBias
    /// twice on two separate deck instances can't guarantee the same
    /// card doesn't appear in both hands, since each instance believes
    /// it has the entire deck to itself -- this correctly models a
    /// single real deck shared between both hands, which a genuine
    /// pai gow table always is. The one real Joker's inclusion
    /// probability is set explicitly, same idea as the single-hand
    /// version; if included, a coin flip decides which of the two
    /// hands receives it, since there's only one Joker to give to
    /// either. Call this instead of DealSevenCardsWithJokerBias
    /// whenever two simultaneous hands need to come from one deck.
    /// </summary>
    public (List<PaiGowCard> Hand1, List<PaiGowCard> Hand2) DealTwoSevenCardHandsWithJokerBias(double jokerInclusionProbability)
    {
        PaiGowCard joker = _cards.First(c => c.IsJoker);
        List<PaiGowCard> nonJokerCards = _cards.Where(c => !c.IsJoker).ToList();

        bool includeJoker = Rng.NextDouble() < jokerInclusionProbability;
        bool jokerGoesToHand1 = Rng.NextDouble() < 0.5;

        List<PaiGowCard> hand1 = new(7);
        List<PaiGowCard> hand2 = new(7);
        int cursor = 0;

        if (includeJoker && jokerGoesToHand1)
        {
            hand1.Add(joker);
            hand1.AddRange(nonJokerCards.GetRange(cursor, 6)); cursor += 6;
            hand2.AddRange(nonJokerCards.GetRange(cursor, 7));
        }
        else if (includeJoker)
        {
            hand1.AddRange(nonJokerCards.GetRange(cursor, 7)); cursor += 7;
            hand2.Add(joker);
            hand2.AddRange(nonJokerCards.GetRange(cursor, 6));
        }
        else
        {
            hand1.AddRange(nonJokerCards.GetRange(cursor, 7)); cursor += 7;
            hand2.AddRange(nonJokerCards.GetRange(cursor, 7));
        }

        return (hand1, hand2);
    }
}

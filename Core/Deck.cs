namespace DealerSchoolTrainerWeb.Core;

/// <summary>
/// Standard 52-card deck. Shared across every trainer that needs a
/// random deal (Blackjack Trainer, Side Wagers, Pai Gow, Hold'em,
/// Three Card Prime). The Payout Trainer form does not draw from
/// this — its two displayed cards are a fixed decorative pair — but
/// the shared deck is established now per the up-front architecture
/// decision rather than being built per-form later.
/// </summary>
public sealed class Deck
{
    private readonly List<Card> _cards = new(52);
    private static readonly Random Rng = new();

    public Deck()
    {
        Reset();
    }

    public void Reset()
    {
        _cards.Clear();
        foreach (Suit suit in Enum.GetValues<Suit>())
        {
            foreach (Rank rank in Enum.GetValues<Rank>())
            {
                _cards.Add(new Card(rank, suit));
            }
        }
    }

    public void Shuffle()
    {
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = Rng.Next(i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
    }

    public Card Draw()
    {
        if (_cards.Count == 0)
            throw new InvalidOperationException("Deck is empty.");

        Card card = _cards[^1];
        _cards.RemoveAt(_cards.Count - 1);
        return card;
    }

    public int RemainingCount => _cards.Count;
}

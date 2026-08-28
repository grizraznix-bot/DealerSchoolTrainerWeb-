using DealerSchoolTrainerWeb.Core;

namespace DealerSchoolTrainerWeb.Games.FlashCardTrainer;

/// <summary>
/// Direct translation of clsBJTDeck.cls. Deliberately separate from
/// the shared Core.Deck: this one auto-reshuffles when it runs out
/// (a perpetual shoe) and supports stripping all ten-value cards for
/// counting practice — behavior the other trainers don't need.
/// </summary>
public sealed class BlackjackShoe
{
    private List<Card> _cards = new();
    private int _nextIndex;
    private static readonly Random Rng = new();

    public BlackjackShoe()
    {
        BuildDeck();
        Shuffle();
    }

    private void BuildDeck()
    {
        _cards = new List<Card>(52);
        foreach (Suit suit in Enum.GetValues<Suit>())
        {
            foreach (Rank rank in Enum.GetValues<Rank>())
            {
                _cards.Add(new Card(rank, suit));
            }
        }
    }

    /// <summary>
    /// Removes all ten-value cards (10, J, Q, K) for counting-practice
    /// mode. Call right after construction, before any Draw calls.
    /// </summary>
    public void RemoveTenValueCards()
    {
        _cards.RemoveAll(c => c.Rank is Rank.Ten or Rank.Jack or Rank.Queen or Rank.King);
        Shuffle();
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

    public Card Draw()
    {
        if (_nextIndex >= _cards.Count)
            Shuffle();

        return _cards[_nextIndex++];
    }
}

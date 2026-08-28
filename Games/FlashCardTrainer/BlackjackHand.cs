using DealerSchoolTrainerWeb.Core;

namespace DealerSchoolTrainerWeb.Games.FlashCardTrainer;

/// <summary>Direct translation of clsBJTHand.cls.</summary>
public sealed class BlackjackHand
{
    private readonly List<Card> _cards = new();

    public void AddCard(Card card) => _cards.Add(card);

    public int Count => _cards.Count;

    /// <summary>0-based, unlike the VBA class's 1-based indexer.</summary>
    public Card CardAt(int index) => _cards[index];

    public int Total
    {
        get
        {
            int total = 0, aces = 0;
            foreach (Card c in _cards)
            {
                total += c.BlackjackHardValue();
                if (c.IsAce()) aces++;
            }

            while (aces > 0 && total + 10 <= 21)
            {
                total += 10;
                aces--;
            }

            return total;
        }
    }

    public int HardTotal => _cards.Sum(c => c.BlackjackHardValue());

    public bool IsSoft
    {
        get
        {
            int total = 0, aces = 0;
            bool softUsed = false;

            foreach (Card c in _cards)
            {
                total += c.BlackjackHardValue();
                if (c.IsAce()) aces++;
            }

            while (aces > 0 && total + 10 <= 21)
            {
                total += 10;
                aces--;
                softUsed = true;
            }

            return softUsed;
        }
    }

    public bool IsBust => Total > 21;

    public bool IsBlackjack => Count == 2 && Total == 21;

    /// <param name="hitSoft17">True = dealer hits a soft 17 (H17 house rule).</param>
    public bool MustHit(bool hitSoft17)
    {
        int t = Total;
        if (t < 17) return true;
        if (t == 17 && IsSoft && hitSoft17) return true;
        return false;
    }

    public bool HasAce => _cards.Any(c => c.IsAce());
}

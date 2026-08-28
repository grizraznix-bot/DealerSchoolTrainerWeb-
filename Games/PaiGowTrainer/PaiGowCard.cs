namespace DealerSchoolTrainerWeb.Games.PaiGowTrainer;

/// <summary>
/// Direct translation of clsPaiGowCard.cls. Deliberately separate
/// from the shared Core.Card: Pai Gow's 53-card deck includes a
/// Joker, which has no rank or suit in the normal sense, so this
/// keeps the same string-based rank/suit representation the VBA
/// class used rather than forcing the Joker into the shared enum.
/// </summary>
public sealed class PaiGowCard
{
    /// <summary>"A", "2"-"9", "T", "J", "Q", "K", or "JOKER".</summary>
    public string Rank { get; }

    /// <summary>"S", "H", "D", "C". The Joker has no suit and is "".</summary>
    public string Suit { get; }

    public PaiGowCard(string rank, string suit)
    {
        Rank = rank.Trim().ToUpperInvariant();
        Suit = suit.Trim().ToUpperInvariant();
    }

    public bool IsJoker => Rank == "JOKER";

    /// <summary>Image code used by CardImageProvider — e.g. "AS", "KH", "7D", "JOKER".</summary>
    public string ImageCode => IsJoker ? "JOKER" : Rank + Suit;

    /// <summary>Numerical rank order for sorting. Joker = 0 (handled separately), Ace = 14.</summary>
    public int RankOrder => Rank switch
    {
        "JOKER" => 0,
        "T" => 10,
        "J" => 11,
        "Q" => 12,
        "K" => 13,
        "A" => 14,
        _ => int.TryParse(Rank, out int n) ? n : 0
    };

    /// <summary>Short text representation, e.g. "A♠", "K♥", "10♦", "JOKER".</summary>
    public string ShortLabel
    {
        get
        {
            if (IsJoker) return "JOKER";

            string r = Rank == "T" ? "10" : Rank;
            string s = Suit switch
            {
                "S" => "\u2660",
                "H" => "\u2665",
                "D" => "\u2666",
                "C" => "\u2663",
                _ => ""
            };
            return r + s;
        }
    }
}

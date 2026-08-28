namespace DealerSchoolTrainerWeb.Core;

public enum Suit
{
    Clubs,
    Diamonds,
    Hearts,
    Spades
}

public enum Rank
{
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13,
    Ace = 14
}

/// <summary>
/// Shared card model used by every trainer. Deliberately the single
/// Card/Deck implementation for the whole app (replacing the isolated
/// per-form Card classes the VBA project used — clsCard, clsBJTCard,
/// clsPaiGowCard, etc. — now that C#'s compile-time type checking
/// removes the reason those were kept separate).
/// </summary>
public sealed class Card
{
    public Rank Rank { get; }
    public Suit Suit { get; }

    public Card(Rank rank, Suit suit)
    {
        Rank = rank;
        Suit = suit;
    }

    /// <summary>
    /// Two-character code matching the asset filenames under
    /// Assets/Cards (e.g. "AS", "TD", "9H") — same convention the
    /// original workbook's CardImages sheet used.
    /// </summary>
    public string Code => RankChar() + SuitChar();

    private string RankChar() => Rank switch
    {
        Rank.Ten => "T",
        Rank.Jack => "J",
        Rank.Queen => "Q",
        Rank.King => "K",
        Rank.Ace => "A",
        _ => ((int)Rank).ToString()
    };

    private string SuitChar() => Suit switch
    {
        Suit.Clubs => "C",
        Suit.Diamonds => "D",
        Suit.Hearts => "H",
        Suit.Spades => "S",
        _ => throw new ArgumentOutOfRangeException(nameof(Suit), Suit, "Unrecognized suit.")
    };

    public override string ToString() => Code;
}

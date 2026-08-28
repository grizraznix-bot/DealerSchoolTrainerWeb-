namespace DealerSchoolTrainerWeb.Core;

/// <summary>
/// Blackjack-flavored helpers on the shared Card type. Kept separate
/// from Card itself because different trainers want different rank
/// semantics (e.g. Hold'em treats Ace as high only) — these extension
/// methods are opt-in per game rather than baked into the base type.
/// </summary>
public static class CardExtensions
{
    /// <summary>Ace=1, T/J/Q/K=10, everything else its face value.</summary>
    public static int BlackjackHardValue(this Card card) => card.Rank switch
    {
        Rank.Ace => 1,
        Rank.Ten or Rank.Jack or Rank.Queen or Rank.King => 10,
        _ => (int)card.Rank
    };

    public static bool IsAce(this Card card) => card.Rank == Rank.Ace;

    /// <summary>
    /// Ace-LOW rank order used for three-card straight checks in the
    /// Blackjack side wagers (21+3): A=1, 2-9 natural, T=10, J=11,
    /// Q=12, K=13. Matches clsBJSWCard.RankOrder from the workbook.
    /// </summary>
    public static int AceLowRankOrder(this Card card) => card.Rank == Rank.Ace ? 1 : (int)card.Rank;
}

namespace DealerSchoolTrainerWeb.Games.RouletteTrainer;

/// <summary>
/// The Roulette close-up shared by the Roulette trainer and Roulette in
/// Challenge Mode: the 12 betting spots that win on 17, where each
/// spot's chip sits on the board, and dealing a random set of wagers.
/// Drawn by Shared/RouletteBoard.razor.
/// </summary>
public static class RouletteLayout
{
    // One betting spot that wins on 17. X/Y are where its chip sits, in
    // cells from the top-left of the grid (17 is the middle of row 2,
    // col 1; whole numbers are lines, .5 is the middle of a cell). The
    // street and six-lines sit on the outside line at the 16 end of the
    // rows (dozens side). Order keeps the breakdown in a steady order.
    public sealed record Spot(string Name, int Pays, double X, double Y, int Order);

    public sealed record PlacedWager(Spot Spot, int Chips);

    public static readonly Spot[] Spots =
    {
        new("Straight up 17", 35, 1.5, 2.5, 0),
        new("Split 14/17", 17, 1.5, 2.0, 1),
        new("Split 16/17", 17, 1.0, 2.5, 2),
        new("Split 17/18", 17, 2.0, 2.5, 3),
        new("Split 17/20", 17, 1.5, 3.0, 4),
        new("Street 16/17/18", 11, 0.0, 2.5, 5),
        new("Corner 13/14/16/17", 8, 1.0, 2.0, 6),
        new("Corner 14/15/17/18", 8, 2.0, 2.0, 7),
        new("Corner 16/17/19/20", 8, 1.0, 3.0, 8),
        new("Corner 17/18/20/21", 8, 2.0, 3.0, 9),
        new("Six-line 13-18", 5, 0.0, 2.0, 10),
        new("Six-line 16-21", 5, 0.0, 3.0, 11)
    };

    // Winning number, and the first number in the close-up (top-left):
    // rows 10-12 (partly shown), 13-15, 16-18, 19-21, 22-24 (partly shown)
    public const int WinningNumber = 17;
    public const int FirstNumber = 10;

    // Cell size in px, lines included -- must match .rl-grid in app.css.
    // EdgeRowH: the partly shown top and bottom rows (10-12, 22-24).
    public const double CellW = 94;
    public const double CellH = 54;
    public const double EdgeRowH = 24;

    public static readonly HashSet<int> RedNumbers = new() { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };

    /// <summary>
    /// <paramref name="count"/> different spots at random, each with a stack
    /// of minChips..maxChips chips, listed in board order.
    /// </summary>
    public static List<PlacedWager> Deal(int count, int minChips, int maxChips, Random rng)
    {
        count = Math.Clamp(count, 1, Spots.Length);
        return Spots
            .OrderBy(_ => rng.Next())
            .Take(count)
            .OrderBy(s => s.Order)
            .Select(s => new PlacedWager(s, rng.Next(minChips, maxChips + 1)))
            .ToList();
    }

    /// <summary>Total payout in chips (winnings only).</summary>
    public static int Total(IEnumerable<PlacedWager> wagers) => wagers.Sum(w => w.Chips * w.Spot.Pays);
}

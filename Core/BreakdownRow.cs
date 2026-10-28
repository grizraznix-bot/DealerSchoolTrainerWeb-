namespace DealerSchoolTrainerWeb.Core;

/// <summary>
/// One line of the after-Submit feedback box (Shared/ResultBreakdown):
/// the wager or question, what the user picked, the right answer, and
/// a short reason. Used by every game that settles several wagers.
/// </summary>
public sealed record BreakdownRow(string Name, string You, string Answer, bool Ok, string Why)
{
    /// <summary>
    /// Result line above the box, worded the same in every game:
    /// "CORRECT — all 5 right" / "INCORRECT — 2 of 5 wrong" (just
    /// CORRECT / INCORRECT when there's only one line).
    /// </summary>
    public static string Summary(IReadOnlyCollection<BreakdownRow> rows)
    {
        int wrong = rows.Count(r => !r.Ok);
        if (rows.Count <= 1) return wrong == 0 ? "CORRECT" : "INCORRECT";
        return wrong == 0
            ? $"CORRECT — all {rows.Count} right"
            : $"INCORRECT — {wrong} of {rows.Count} wrong";
    }
}

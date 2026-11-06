namespace DealerSchoolTrainerWeb.Core;

/// <summary>
/// What happened in one practice session, for the end-of-session summary
/// (Shared/SessionSummary): how each wager or hand type went ("parts")
/// and the most recent misses. Rounds played and correct come from each
/// game's own score, so this only holds the detail behind them.
/// A new log is made each time a mode starts.
/// </summary>
public sealed class SessionLog
{
    public const int MaxMisses = 5;

    public DateTime Started { get; } = DateTime.Now;

    // Parts in the order first seen (the game's settling order)
    private readonly List<string> _partOrder = new();
    private readonly Dictionary<string, (int Right, int Total)> _parts = new();
    private readonly List<SessionMiss> _misses = new();

    public int MissCount { get; private set; }

    /// <summary>One answer for a wager or hand type, e.g. ("All 6", false).</summary>
    public void Part(string name, bool ok)
    {
        if (!_parts.TryGetValue(name, out var p))
        {
            p = (0, 0);
            _partOrder.Add(name);
        }
        _parts[name] = (p.Right + (ok ? 1 : 0), p.Total + 1);
    }

    /// <summary>A missed answer: keeps the latest few for review.</summary>
    public void Miss(string title, string why, string you, string answer)
    {
        MissCount++;
        _misses.Add(new SessionMiss(title, why, you, answer));
        if (_misses.Count > MaxMisses) _misses.RemoveAt(0);
    }

    /// <summary>
    /// A scored round's feedback rows: each row is a part (named by
    /// partName, default the row's name; null = don't count it as a part)
    /// and each wrong row a miss.
    /// </summary>
    public void AddRows(IEnumerable<BreakdownRow> rows, Func<BreakdownRow, string?>? partName = null)
    {
        foreach (BreakdownRow r in rows)
        {
            string? part = partName is null ? r.Name : partName(r);
            if (part is not null) Part(part, r.Ok);
            if (!r.Ok) Miss(r.Name, r.Why, r.You, r.Answer);
        }
    }

    /// <summary>Parts with no misses, in settling order.</summary>
    public IEnumerable<string> StrongParts =>
        _partOrder.Where(n => _parts[n].Right == _parts[n].Total);

    /// <summary>Parts with misses: name and how many.</summary>
    public IEnumerable<(string Name, int Misses)> WeakParts =>
        _partOrder.Where(n => _parts[n].Right < _parts[n].Total)
                  .Select(n => (n, _parts[n].Total - _parts[n].Right));

    public int PartCount => _partOrder.Count;

    /// <summary>Latest misses, newest first.</summary>
    public IEnumerable<SessionMiss> RecentMisses => Enumerable.Reverse(_misses);
}

public sealed record SessionMiss(string Title, string Why, string You, string Answer);

namespace DealerSchoolTrainerWeb.Core;

/// <summary>
/// Right-in-a-row count for the score line (Shared/GameStatus), worked out
/// from each game's own score as it changes: a scored round that was right
/// adds one, a miss starts over, and a score reset (new session) clears it.
/// Also the short every-10-hands line.
/// </summary>
public sealed class PracticeStreak
{
    private int _lastCorrect;
    private int _lastPlayed;

    public int Count { get; private set; }

    /// <summary>The streak badge shows from this many in a row.</summary>
    public const int ShowFrom = 3;

    public bool Show => Count >= ShowFrom;

    /// <summary>Call with the current score whenever it may have changed.</summary>
    public void Update(int correct, int played)
    {
        if (played == 0 || played < _lastPlayed)
        {
            Count = 0;                                  // new session
        }
        else if (played > _lastPlayed)
        {
            int rounds = played - _lastPlayed;
            int right = correct - _lastCorrect;
            Count = right >= rounds ? Count + rounds : 0;
        }
        _lastPlayed = played;
        _lastCorrect = correct;
    }

    /// <summary>The every-10-hands line, or "" on other hands.</summary>
    public static string Cheer(int correct, int played)
    {
        if (played <= 0 || played % 10 != 0) return "";
        double rate = (double)correct / played;
        if (rate >= 0.9) return $"{played} hands done, {correct} right. Sharp work.";
        if (rate >= 0.7) return $"{played} hands done, {correct} right so far. Steady work.";
        return $"{played} hands done. Keep going: every hand builds the habit.";
    }
}

namespace DealerSchoolTrainerWeb.Games.PayoutTrainer;

public enum PayoutDifficulty
{
    Beginner,
    Intermediate,
    Advanced
}

public enum PayoutRatio
{
    ThreeToTwo,
    SixToFive
}

/// <summary>
/// Direct translation of modPayoutWager.bas. All money is handled in
/// integer cents throughout, same as the original, to avoid float
/// comparison risk against the user's typed currency answer.
/// </summary>
public static class PayoutWager
{
    private static readonly Random Rng = new();

    /// <summary>
    /// Generates one random wager in cents for the given ratio and
    /// difficulty. NOTE: this matches the workbook's actual behavior,
    /// not its (stale) code comment — the comment says "70% chance $5,
    /// 25% chance $1, 5% chance $0.50" but the real weights coded in
    /// GenerateWager are 40% $5.00, 40% $1.00, 20% $0.50. Flag to Jesse
    /// if the comment's odds were the intended ones instead.
    /// </summary>
    public static long GenerateWagerCents(PayoutRatio ratio, PayoutDifficulty difficulty)
    {
        long minCents, maxCents, stepCents;

        if (ratio == PayoutRatio.ThreeToTwo)
        {
            int minVal, maxVal;
            switch (difficulty)
            {
                case PayoutDifficulty.Beginner:
                    minVal = 5; maxVal = 50;
                    break;
                case PayoutDifficulty.Intermediate:
                    minVal = 10; maxVal = 100;
                    break;
                default:
                    minVal = 50; maxVal = 500;
                    break;
            }

            minCents = minVal * 100L;
            maxCents = maxVal * 100L;

            double roll = Rng.NextDouble();
            if (roll < 0.2) stepCents = 50;        // $0.50 - 20% chance
            else if (roll < 0.6) stepCents = 100;  // $1.00 - 40% chance
            else stepCents = 500;                  // $5.00 - 40% chance
        }
        else
        {
            // RATIO_6TO5 - unchanged, always $5 steps
            switch (difficulty)
            {
                case PayoutDifficulty.Beginner:
                    minCents = 500; maxCents = 5000;
                    break;
                case PayoutDifficulty.Intermediate:
                    minCents = 500; maxCents = 10000;
                    break;
                default:
                    minCents = 2500; maxCents = 50000;
                    break;
            }

            stepCents = 500;
        }

        long stepCount = (maxCents - minCents) / stepCents;
        return minCents + (Rng.Next((int)stepCount + 1) * stepCents);
    }

    /// <summary>
    /// Integer-cents payout. 3:2 house rule for an odd $0.50 wager:
    /// the whole-dollar portion pays true 3:2, and the leftover $0.50
    /// pays as its own even-money (1:1) unit rather than being
    /// multiplied by 3/2 — e.g. $5.50 pays $7.50 (on the $5) + $0.50
    /// (on the $0.50) = $8.00, not $8.25. 6:5 never produces a $0.50
    /// wager (always $5 steps), so this split never applies there.
    /// </summary>
    public static long CalculatePayoutCents(long wagerCents, PayoutRatio ratio)
    {
        if (ratio == PayoutRatio.ThreeToTwo)
        {
            long wholeDollarCents = (wagerCents / 100) * 100;
            long remainderCents = wagerCents % 100;

            return remainderCents == 50
                ? (wholeDollarCents * 3 / 2) + 50
                : wagerCents * 3 / 2;
        }

        return wagerCents * 6 / 5;
    }

    /// <summary>
    /// Parses a typed dollar amount ("5", "5.5", "5.50") into cents.
    /// Matches ParseCents in the workbook exactly, including its
    /// truncation of anything past two decimal digits.
    /// </summary>
    public static long ParseCents(string raw)
    {
        raw = raw.Trim();
        int dotPos = raw.IndexOf('.');

        string dollarPart, centPart;

        if (dotPos < 0)
        {
            dollarPart = raw;
            centPart = "00";
        }
        else
        {
            dollarPart = raw[..dotPos];
            centPart = raw[(dotPos + 1)..];

            centPart = centPart.Length switch
            {
                0 => "00",
                1 => centPart + "0",
                _ => centPart[..2]
            };
        }

        if (dollarPart.Length == 0)
            dollarPart = "0";

        return long.Parse(dollarPart) * 100 + long.Parse(centPart);
    }

    /// <summary>Always renders with a literal "$" regardless of OS locale, matching the original.</summary>
    public static string FormatCents(long cents) => "$" + (cents / 100.0).ToString("0.00");
}

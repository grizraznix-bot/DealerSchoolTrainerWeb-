namespace DealerSchoolTrainerWeb.Core;

/// <summary>
/// The quick way a dealer works out a payout in their head, shown under
/// a missed payout in the payout modes, e.g. "$15 × 100 = $1,500, ÷ 4 =
/// $375" for 25 to 1. One standard shortcut per pay, the same in every
/// game; pays without a handy trick just show the multiplication.
/// Amounts are in cents so 3 to 2 / $7.50 payouts work too.
/// </summary>
public static class PayoutShortcut
{
    /// <summary>The shortcut line, or "" when there's nothing to show (no win, or odd amounts).</summary>
    public static string For(long wagerCents, long payoutCents)
    {
        if (wagerCents <= 0 || payoutCents <= 0) return "";

        string W = M(wagerCents), P = M(payoutCents);

        // 3 to 2 (1.5 to 1): the bet plus half
        if (payoutCents * 2 == wagerCents * 3) return $"{W} + half ({M(wagerCents / 2)}) = {P}";
        // 6 to 5: the bet plus a fifth
        if (payoutCents * 5 == wagerCents * 6) return $"{W} + a fifth ({M(wagerCents / 5)}) = {P}";

        if (payoutCents % wagerCents != 0) return "";
        long m = payoutCents / wagerCents;

        return m switch
        {
            1 => $"Pays even: {P}",
            2 => $"Double {W} = {P}",
            4 => $"Double {W} = {M(wagerCents * 2)}, double again = {P}",
            5 => $"{W} × 10 = {M(wagerCents * 10)}, half = {P}",
            6 => $"{W} × 5 = {M(wagerCents * 5)}, + one more {W} = {P}",
            7 => $"{W} × 5 = {M(wagerCents * 5)}, + {W} × 2 = {P}",
            8 => $"Double {W} three times = {P}",
            9 => $"{W} × 10 = {M(wagerCents * 10)}, − one {W} = {P}",
            15 => $"{W} × 10 = {M(wagerCents * 10)}, + half of that = {P}",
            19 => $"{W} × 20 = {M(wagerCents * 20)}, − one {W} = {P}",
            25 => $"{W} × 100 = {M(wagerCents * 100)}, ÷ 4 = {P}",
            50 => $"{W} × 100 = {M(wagerCents * 100)}, half = {P}",
            75 => $"{W} × 100 = {M(wagerCents * 100)}, − a quarter = {P}",
            125 => $"{W} × 100 = {M(wagerCents * 100)}, + a quarter = {P}",
            250 => $"{W} × 1,000 = {M(wagerCents * 1000)}, ÷ 4 = {P}",
            500 => $"{W} × 1,000 = {M(wagerCents * 1000)}, half = {P}",
            750 => $"{W} × 1,000 = {M(wagerCents * 1000)}, − a quarter = {P}",
            2500 => $"{W} × 10,000 = {M(wagerCents * 10000)}, ÷ 4 = {P}",
            // 10, 20, 30, 40, 100, 200, 1,000: multiply by the front digit,
            // then add the zeros
            _ when IsDigitThenZeros(m, out long digit, out string zeros) && zeros.Length > 0
                => digit == 1
                    ? $"{W}, {AddZeros(zeros.Length)} = {P}"
                    : $"{W} × {digit} = {M(wagerCents * digit)}, {AddZeros(zeros.Length)} = {P}",
            _ => $"{W} × {m:N0} = {P}"
        };
    }

    private static string AddZeros(int n) => n switch
    {
        1 => "add a 0",
        2 => "add two 0's",
        3 => "add three 0's",
        _ => $"add {n} 0's"
    };

    // 30 -> 3 and "0"; 200 -> 2 and "00"; 7 -> false
    private static bool IsDigitThenZeros(long m, out long digit, out string zeros)
    {
        string s = m.ToString();
        digit = s[0] - '0';
        zeros = s[1..];
        return zeros.Length > 0 && zeros.All(c => c == '0');
    }

    private static string M(long cents) =>
        cents % 100 == 0 ? $"${cents / 100:N0}" : $"${cents / 100.0:N2}";
}

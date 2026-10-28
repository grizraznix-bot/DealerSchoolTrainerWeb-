namespace DealerSchoolTrainerWeb.Games.RouletteTrainer;

/// <summary>
/// Roulette Math Tricks: several different ways to work out each
/// payout in your head, so a dealer who freezes on one way has another
/// to fall back on. Each way turns any chip count into numbered,
/// plain-language steps (shown on the Math Tricks page and in Roulette's
/// Help). Every way's answer is checked against chips × pays.
/// </summary>
public static class RouletteMath
{
    /// <summary>One way of working out a payout.</summary>
    /// <param name="Name">Short name, e.g. "Half the chips, times 70".</param>
    /// <param name="Idea">Why it works, in one line.</param>
    /// <param name="Steps">Builds the steps for a chip count; null when this way doesn't suit that count (WhenNot says why).</param>
    /// <param name="WhenNot">Shown instead of steps when Steps returns null.</param>
    /// <param name="WatchOut">The most common mistake with this way.</param>
    public sealed record Way(string Name, string Idea, Func<int, List<string>?> Steps, string WhenNot, string WatchOut);

    /// <summary>One bet type: its pay, the ways to work it out, and quick example chip counts.</summary>
    public sealed record BetTricks(string Bet, string ShortName, int Pays, int[] Examples, IReadOnlyList<Way> Ways);

    public static string N(long n) => n.ToString("N0");

    private static List<string> L(params string[] steps) => steps.ToList();

    public static readonly IReadOnlyList<BetTricks> All = new[]
    {
        // ---------------------------------------------------------- 35
        new BetTricks("Straight up", "Straight", 35, new[] { 2, 4, 7, 9, 13 }, new[]
        {
            new Way("Half the chips, times 70",
                "35 is half of 70, and 70 is just 7 with a zero on the end.",
                n =>
                {
                    int h = n / 2;
                    if (n % 2 == 0)
                    {
                        return L($"Cut {n} in half: {h}.",
                                 $"Multiply by 7: {h} × 7 = {N(h * 7)}.",
                                 $"Put a zero on the end: {N(h * 70)}.");
                    }
                    return h == 0
                        ? L("Cut 1 in half: 0 and a half.",
                            "The half is always worth 35.",
                            "So the answer is 35.")
                        : L($"Cut {n} in half: {h} and a half.",
                            $"Do the whole part first: {h} × 7 = {N(h * 7)}, put a zero on the end: {N(h * 70)}.",
                            "The half is always worth 35.",
                            $"Add them: {N(h * 70)} + 35 = {N(n * 35)}.");
                },
                "", "With an odd number of chips, forgetting to add the extra 35 for the half."),

            new Way("Three dozen, take one away",
                "35 is one less than 36, and 36 is three dozen.",
                n => L($"{n} dozen: {n} × 12 = {N(n * 12)}.",
                       $"Three of those: {N(n * 12)} × 3 = {N(n * 36)}.",
                       $"Take away the chip count once: {N(n * 36)} − {n} = {N(n * 35)}."),
                "", "Taking away 1 instead of taking away the number of chips."),

            new Way("Thirties plus fives",
                "35 is 30 + 5. Do each part on its own, then add.",
                n => L($"The 30s: {n} × 3 = {N(n * 3)}, put a zero on the end: {N(n * 30)}.",
                       $"The 5s: put a zero on {n} ({N(n * 10)}) and cut it in half: {N(n * 5)}.",
                       $"Add them: {N(n * 30)} + {N(n * 5)} = {N(n * 35)}."),
                "", "Forgetting the zero on the 30s part."),
        }),

        // ---------------------------------------------------------- 17
        new BetTricks("Split", "Split", 17, new[] { 3, 6, 8, 9, 12 }, new[]
        {
            new Way("Tens plus sevens",
                "17 is 10 + 7. Work out the 10s and the 7s, then add.",
                n => L($"The 10s: put a zero on the end of {n}: {N(n * 10)}.",
                       $"The 7s: {n} × 7 = {N(n * 7)}.",
                       $"Add them: {N(n * 10)} + {N(n * 7)} = {N(n * 17)}."),
                "", "Adding the chip count instead of 7 times the chip count."),

            new Way("Twenty minus three",
                "17 is 20 − 3. Easier when the 7s times table feels hard.",
                n => L($"Double {n} ({N(n * 2)}) and put a zero on the end: {N(n * 20)}.",
                       $"The 3s: {n} × 3 = {N(n * 3)}.",
                       $"Take the 3s away: {N(n * 20)} − {N(n * 3)} = {N(n * 17)}."),
                "", "Adding the 3s instead of taking them away."),

            new Way("Double four times, add one more",
                "Doubling four times is × 16; one more stack makes × 17. Only doubling and one add.",
                n => L($"Double: {n} → {N(n * 2)}.",
                       $"Double: {N(n * 2)} → {N(n * 4)}.",
                       $"Double: {N(n * 4)} → {N(n * 8)}.",
                       $"Double: {N(n * 8)} → {N(n * 16)}.",
                       $"Add one more {n}: {N(n * 16)} + {n} = {N(n * 17)}."),
                "", "Losing count of the doubles. Say them out loud: one, two, three, four."),
        }),

        // ---------------------------------------------------------- 11
        new BetTricks("Street", "Street", 11, new[] { 4, 7, 23, 52, 58 }, new[]
        {
            new Way("Put the digit sum in the middle",
                "For a two-digit number: split the digits apart, add them, and the sum goes in between.",
                n =>
                {
                    if (n < 10 || n > 99) return null;
                    int a = n / 10, b = n % 10, sum = a + b;
                    if (sum <= 9)
                    {
                        return L($"Split {n} apart: {a} _ {b}.",
                                 $"Add the two digits: {a} + {b} = {sum}.",
                                 $"Put {sum} in the gap: {a}{sum}{b}.");
                    }
                    return L($"Split {n} apart: {a} _ {b}.",
                             $"Add the two digits: {a} + {b} = {sum}. That's 10 or more, so it needs a carry.",
                             $"Put only the {sum % 10} in the gap: {a} {sum % 10} {b}.",
                             $"Carry the 1 onto the first digit: {a} + 1 = {a + 1}.",
                             $"Answer: {N(n * 11)}.");
                },
                "This way works for 10 to 99 chips. Use another way for this count.",
                "Forgetting the carry when the two digits add up to 10 or more."),

            new Way("Times 10, add it once more",
                "11 is 10 + 1. Works for any number of chips.",
                n => L($"Put a zero on the end of {n}: {N(n * 10)}.",
                       $"Add {n} once more: {N(n * 10)} + {n} = {N(n * 11)}."),
                "", "Adding 1 instead of adding the chip count."),

            new Way("Twelves, take one away",
                "11 is 12 − 1. Handy if you know your dozens.",
                n => L($"{n} dozen: {n} × 12 = {N(n * 12)} (that's {N(n * 10)} + {N(n * 2)}).",
                       $"Take away the chip count once: {N(n * 12)} − {n} = {N(n * 11)}."),
                "", "Taking away 1 instead of the chip count."),
        }),

        // ---------------------------------------------------------- 8
        new BetTricks("Corner", "Corner", 8, new[] { 3, 5, 6, 9, 13 }, new[]
        {
            new Way("Double, double, double",
                "8 is 2 × 2 × 2: three doubles.",
                n => L($"Double once: {n} → {N(n * 2)}.",
                       $"Double twice: {N(n * 2)} → {N(n * 4)}.",
                       $"Double three times: {N(n * 4)} → {N(n * 8)}."),
                "", "Stopping after two doubles (that's only × 4)."),

            new Way("Tens minus twos",
                "8 is 10 − 2.",
                n => L($"Put a zero on the end of {n}: {N(n * 10)}.",
                       $"Double {n}: {N(n * 2)}.",
                       $"Take it away: {N(n * 10)} − {N(n * 2)} = {N(n * 8)}."),
                "", "Taking away 2 instead of double the chip count."),

            new Way("Fives plus threes",
                "8 is 5 + 3.",
                n => L($"The 5s: put a zero on {n} ({N(n * 10)}) and cut it in half: {N(n * 5)}.",
                       $"The 3s: {n} × 3 = {N(n * 3)}.",
                       $"Add them: {N(n * 5)} + {N(n * 3)} = {N(n * 8)}."),
                "", "Mixing up which part is the 5s and which is the 3s."),
        }),

        // ---------------------------------------------------------- 5
        new BetTricks("Six-line", "Six-line", 5, new[] { 2, 5, 9, 14, 17 }, new[]
        {
            new Way("Times 10, then half",
                "5 is half of 10.",
                n => L($"Put a zero on the end of {n}: {N(n * 10)}.",
                       $"Cut it in half: {N(n * 5)}."),
                "", "Forgetting to cut it in half (that's × 10)."),

            new Way("Half first, then times 10",
                "Same idea the other way round; easiest with an even number of chips.",
                n =>
                {
                    int h = n / 2;
                    if (n % 2 == 0)
                    {
                        return L($"Cut {n} in half: {h}.",
                                 $"Put a zero on the end: {N(h * 10)}.");
                    }
                    return L($"Cut {n} in half: {h} and a half.",
                             $"The whole part with a zero on the end: {N(h * 10)}.",
                             "The half is worth 5.",
                             $"Add them: {N(h * 10)} + 5 = {N(n * 5)}.");
                },
                "", "With an odd number of chips, forgetting the extra 5 for the half."),

            new Way("Count by fives",
                "For small stacks: count 5, 10, 15, 20... once for each chip.",
                n => n > 12 ? null
                    : L($"Count by 5s, once per chip ({n} chips): {string.Join(", ", Enumerable.Range(1, n).Select(i => N(i * 5)))}.",
                        $"The last number is the answer: {N(n * 5)}."),
                "Counting works best up to 12 chips. Use another way for this count.",
                "Counting one number too many or too few. Touch each chip as you count."),
        }),
    };
}

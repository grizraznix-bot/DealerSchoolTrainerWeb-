namespace DealerSchoolTrainerWeb.Games.PaiGowTrainer;

/// <summary>Result of grading a submitted hand against the House Way. Returned rather than shown directly (unlike the VBA original's inline MsgBox calls), so the form owns all UI and this stays a pure logic class.</summary>
public sealed class PaiGowSubmitResult
{
    /// <summary>Set (with everything else default) when the submission itself was invalid — e.g. duplicate card, missing Low card.</summary>
    public string? ValidationError { get; init; }

    public bool IsFoul { get; init; }
    public bool IsCorrect { get; init; }

    public string UserLowLabel { get; init; } = "";
    public string UserHighDescription { get; init; } = "";
    public string CorrectLowLabel { get; init; } = "";
    public string CorrectHighDescription { get; init; } = "";
}

/// <summary>
/// Pai Gow Poker House Way Solver. Direct translation of
/// clsPaiGowSolver.cls. The Joker may act as an Ace, and may
/// complete a Straight, Flush, Straight Flush, or Royal Flush.
///
/// Note: clsPaiGowHand.cls existed in the workbook but was never
/// actually instantiated anywhere (dead code superseded by this
/// class's own EvaluateFive/ScoreConcreteHand) — not ported.
/// </summary>
public sealed class PaiGowSolver
{
    private readonly PaiGowCard?[] _cards = new PaiGowCard?[8]; // 1-7 used

    public void SetCards(IReadOnlyList<PaiGowCard> cards)
    {
        for (int i = 1; i <= 7; i++)
            _cards[i] = cards[i - 1];
    }

    /// <summary>Returns the correct House Way Low/High split for the current 7 cards (1-based card indexes).</summary>
    public (int Low1, int Low2, int[] High) GetCorrectHand()
    {
        int low1 = 0, low2 = 0;
        int[] high = new int[6]; // 1-5 used

        if (IsFiveAces())
        {
            ApplyFiveAcesRule(ref low1, ref low2, high);
            return (low1, low2, high);
        }

        if (FourKindRank() > 0)
        {
            ApplyFourKindRule(ref low1, ref low2, high);
            return (low1, low2, high);
        }

        if (HasFullHouse())
        {
            ApplyFullHouseRule(ref low1, ref low2, high);
            return (low1, low2, high);
        }

        int pairCount = ExactPairCount();
        int tripCountValue = TripCount();

        if (tripCountValue >= 2)
        {
            ApplyTwoTripsRule(ref low1, ref low2, high);
            return (low1, low2, high);
        }

        if (pairCount >= 3)
        {
            ApplyThreePairRule(ref low1, ref low2, high);
            return (low1, low2, high);
        }

        if (HasSpecialHighHand())
        {
            // Two Pair may be split rather than playing the special hand,
            // depending upon the House Way rule.
            if (pairCount == 2 && ShouldSplitTwoPair())
            {
                ApplyTwoPairRule(ref low1, ref low2, high);
                return (low1, low2, high);
            }

            // Non-Ace trips remain together.
            if (tripCountValue == 1 && TripRank() != 14)
            {
                ApplyTripsRule(ref low1, ref low2, high);
                return (low1, low2, high);
            }

            // Three Aces + special hand: special hand takes precedence.
            ApplySpecialHandRule(ref low1, ref low2, high);
            return (low1, low2, high);
        }

        if (pairCount == 2)
        {
            ApplyTwoPairRule(ref low1, ref low2, high);
            return (low1, low2, high);
        }

        if (tripCountValue == 1)
        {
            ApplyTripsRule(ref low1, ref low2, high);
            return (low1, low2, high);
        }

        if (pairCount == 1)
        {
            ApplyOnePairRule(ref low1, ref low2, high);
            return (low1, low2, high);
        }

        ApplyNoPairRule(ref low1, ref low2, high);
        return (low1, low2, high);
    }

    /// <summary>
    /// Grades a user's submitted Low/High split against the House Way.
    /// userLowIndexes and userHighIndexes are 1-based indexes into the
    /// 7 cards passed to SetCards (the exact card instances the user
    /// placed — not re-derived by image code, which sidesteps any
    /// ambiguity from duplicate ranks in the 7-card deal).
    /// </summary>
    public PaiGowSubmitResult SubmitHand(int userLow1, int userLow2, int[] userHigh)
    {
        if (userLow1 == 0 || userLow2 == 0)
        {
            return new PaiGowSubmitResult { ValidationError = "Two cards must be placed in the Low Hand." };
        }

        if (userLow1 == userLow2)
        {
            return new PaiGowSubmitResult { ValidationError = "The same card cannot occupy both Low Hand positions." };
        }

        (int correctLow1, int correctLow2, int[] correctHigh) = GetCorrectHand();

        double userLowScore = TwoCardScore(userLow1, userLow2);
        double userHighScore = FiveCardScoreFromIndexes(userHigh);

        if (userHighScore < userLowScore)
        {
            return new PaiGowSubmitResult
            {
                IsFoul = true,
                UserLowLabel = DescribeLowHand(userLow1, userLow2),
                UserHighDescription = DescribeHighHand(userHigh)
            };
        }

        bool isCorrect = HandMatches(userLow1, userLow2, userHigh, correctLow1, correctLow2, correctHigh);

        return new PaiGowSubmitResult
        {
            IsCorrect = isCorrect,
            UserLowLabel = TwoCardRankLabel(userLow1, userLow2),
            UserHighDescription = DescribeHighHand(userHigh),
            CorrectLowLabel = TwoCardRankLabel(correctLow1, correctLow2),
            CorrectHighDescription = DescribeHighHand(correctHigh)
        };
    }

    // ============================================================
    // TWO-CARD RANK LABEL / DISPLAY
    // ============================================================

    // ============================================================
    // PUBLIC EVALUATION HELPERS FOR PAI GOW - SETTLING BETS
    // ============================================================
    // Purely additive wrappers around existing private scoring/
    // detection logic below -- nothing above this point is modified,
    // so Pai Gow - Hand Setting's existing behavior is unaffected.
    // These let a caller evaluate an ARBITRARY Low/High split (not
    // just the single House-Way-correct one from GetCorrectHand),
    // needed to generate varied, non-House-Way-bound hands and to
    // grade the Insurance/Main Wager/Emperor's Treasure side bets.

    /// <summary>Raw strength score for an arbitrary 2-card Low selection. Comparable across different PaiGowSolver instances (i.e. player vs dealer), since the scoring formula itself doesn't depend on which solver instance computed it.</summary>
    public double ScoreLowHand(int card1, int card2) => TwoCardScore(card1, card2);

    /// <summary>Raw strength score for an arbitrary 5-card High selection (1-based indexes, slots 1-5 used in a size-6 array matching GetCorrectHand's own convention).</summary>
    public double ScoreHighHand(int[] fiveCardIndexes) => FiveCardScoreFromIndexes(fiveCardIndexes);

    /// <summary>True if this Low/High split is legal (High outranks or matches Low) -- false means it's a foul.</summary>
    public bool IsLegalSplit(int low1, int low2, int[] high) => ScoreHighHand(high) >= ScoreLowHand(low1, low2);

    /// <summary>
    /// Insurance wager: wins if the BEST possible 5-card hand from any
    /// 5 of the combined 7 cards is High Card -- i.e. nothing better
    /// is achievable at all, not even by using a completely different
    /// 5 of the 7 than whatever the actual Low/High split happens to
    /// use. Corrected from an earlier version that only checked for
    /// pairs/trips/quads/full-house and missed Straight and Flush
    /// entirely (both of which also outrank High Card and would have
    /// wrongly still qualified).
    /// </summary>
    public bool QualifiesInsurance() => BestSevenCardCategory() == 0;

    /// <summary>
    /// Emperor's Treasure wager: wins if the BEST possible 5-card hand
    /// from any 5 of the combined 7 cards is Three of a Kind or
    /// better (category 3+), per standard poker hand ranking. Corrected
    /// from an earlier version that only checked for trips/quads/full-
    /// house/five-aces and missed Straight, Flush, and Straight Flush
    /// (categories 4, 5, 8) -- all of which also rank above Three of a
    /// Kind and would have wrongly NOT qualified, e.g. a genuine 7-card
    /// straight flush using cards outside whatever the specific Low/
    /// High split happens to be.
    /// </summary>
    public bool QualifiesEmperorsTreasure() => BestSevenCardCategory() >= 3;

    /// <summary>Name of the best possible 5-card category achievable from any 5 of the combined 7 cards (e.g. "Straight Flush", "Three of a Kind", "High Card") -- for showing WHY Emperor's Treasure/Insurance won, not just that it did.</summary>
    public string BestSevenCardCategoryName() => BestSevenCardCategory() switch
    {
        10 => "Five Aces",
        9 => "Royal Flush",
        8 => "Straight Flush",
        7 => "Four of a Kind",
        6 => "Full House",
        5 => "Flush",
        4 => "Straight",
        3 => "Three of a Kind",
        2 => "Two Pair",
        1 => "Pair",
        _ => "High Card"
    };

    // Enumerates all C(7,5)=21 possible 5-card subsets of the 7 dealt
    // cards and returns the highest category found among them -- the
    // true best hand achievable from the 7 cards as a whole, entirely
    // independent of whatever specific Low/High split is actually in
    // play. 21 evaluations once per Submit is computationally trivial.
    private int BestSevenCardCategory()
    {
        int best = 0;
        for (int a = 1; a <= 7; a++)
        for (int b = a + 1; b <= 7; b++)
        for (int c = b + 1; c <= 7; c++)
        for (int d = c + 1; d <= 7; d++)
        for (int e = d + 1; e <= 7; e++)
        {
            EvaluateFive(a, b, c, d, e, out int category, out _);
            if (category > best) best = category;
        }
        return best;
    }

    /// <summary>Detailed Low hand label (e.g. "Pair of Kings", "Ace high") -- same description Hand Setting shows for a submitted or correct Low hand.</summary>
    public string LowHandLabel(int card1, int card2) => DescribeLowHand(card1, card2);

    /// <summary>Detailed High hand description (e.g. "Two Pair — Aces and Kings", "Flush — King high") -- same description Hand Setting shows for a submitted or correct High hand.</summary>
    public string HighHandDescription(int[] indexes) => DescribeHighHand(indexes);

    /// <summary>
    /// "Ace High Pai Gow": true if this specific 5-card High hand (as
    /// actually arranged, not "best of any 5 of 7") has no pair at all
    /// and its highest card is an Ace (a Joker acting as one counts) --
    /// the worst possible non-foul hand a dealer can hold. Some
    /// casinos push every wager when the dealer's High hand qualifies
    /// as this.
    /// </summary>
    public bool IsAceHighPaiGow(int[] fiveCardIndexes)
    {
        EvaluateFive(fiveCardIndexes[1], fiveCardIndexes[2], fiveCardIndexes[3], fiveCardIndexes[4], fiveCardIndexes[5], out int category, out _);
        if (category != 0) return false;

        int topRank = 0;
        for (int i = 1; i <= 5; i++)
        {
            int rank = HouseRank(fiveCardIndexes[i]);
            if (rank > topRank) topRank = rank;
        }
        return topRank == 14;
    }

    /// <summary>
    /// "7 Card Straight Flush" (no Joker): all 7 dealt cards are real
    /// (the Joker isn't among them at all), share one suit, and their
    /// ranks form exactly 7 consecutive values (including the Ace-low
    /// wheel extension, e.g. A-2-3-4-5-6-7).
    /// </summary>
    public bool IsSevenCardStraightFlushNoJoker()
    {
        if (HasJoker()) return false;

        string suit = _cards[1]!.Suit;
        for (int i = 2; i <= 7; i++)
        {
            if (_cards[i]!.Suit != suit) return false;
        }

        List<int> ranks = new();
        for (int i = 1; i <= 7; i++) ranks.Add(_cards[i]!.RankOrder);
        ranks.Sort();

        if (FormsConsecutiveRun(ranks)) return true;

        List<int> wheelRanks = ranks.Select(r => r == 14 ? 1 : r).OrderBy(r => r).ToList();
        return FormsConsecutiveRun(wheelRanks);
    }

    /// <summary>
    /// "7 Card Straight Flush" (with Joker): the Joker IS among the 7
    /// dealt cards, the 6 real cards share one suit, and their ranks
    /// span exactly 7 consecutive values with the Joker filling the one
    /// missing rank (including the Ace-low wheel extension).
    /// </summary>
    public bool IsSevenCardStraightFlushWithJoker()
    {
        if (!HasJoker()) return false;

        List<PaiGowCard> realCards = new();
        for (int i = 1; i <= 7; i++)
        {
            if (!_cards[i]!.IsJoker) realCards.Add(_cards[i]!);
        }

        if (realCards.Count != 6) return false;

        string suit = realCards[0].Suit;
        if (realCards.Any(c => c.Suit != suit)) return false;

        List<int> ranks = realCards.Select(c => c.RankOrder).OrderBy(r => r).ToList();
        if (FormsRunWithOneJokerGap(ranks)) return true;

        List<int> wheelRanks = ranks.Select(r => r == 14 ? 1 : r).OrderBy(r => r).ToList();
        return FormsRunWithOneJokerGap(wheelRanks);
    }

    /// <summary>
    /// A Royal Flush (T-J-Q-K-A of one suit, the Joker may complete
    /// one missing card) among the 7 cards, PLUS at least one of the
    /// two leftover cards being an Ace or King of a DIFFERENT suit
    /// than the Royal Flush itself.
    /// </summary>
    public bool IsRoyalFlushWithExtraAceOrKing()
    {
        string[] suits = { "S", "H", "D", "C" };
        HashSet<string> royalRanks = new() { "T", "J", "Q", "K", "A" };

        foreach (string suit in suits)
        {
            List<int> matchingIndexes = new();
            HashSet<string> foundRanks = new();
            int jokerIndex = 0;
            bool hasJoker = false;

            for (int i = 1; i <= 7; i++)
            {
                PaiGowCard c = _cards[i]!;
                if (c.IsJoker) { hasJoker = true; jokerIndex = i; continue; }
                if (c.Suit == suit && royalRanks.Contains(c.Rank))
                {
                    matchingIndexes.Add(i);
                    foundRanks.Add(c.Rank);
                }
            }

            List<int> usedIndexes = new(matchingIndexes);
            bool complete = foundRanks.Count == 5 || (foundRanks.Count == 4 && hasJoker);
            if (!complete) continue;
            if (foundRanks.Count == 4) usedIndexes.Add(jokerIndex);

            for (int i = 1; i <= 7; i++)
            {
                if (usedIndexes.Contains(i)) continue;
                PaiGowCard c = _cards[i]!;
                if (!c.IsJoker && (c.Rank == "A" || c.Rank == "K") && c.Suit != suit)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Emperor's Treasure's category name for feedback -- checks the
    /// three specialty hands first (most specific/rare to least),
    /// falling back to the standard best-of-7 category name otherwise.
    /// All specialty hands here are strict supersets of an already-
    /// qualifying standard category (e.g. a 7-card straight flush
    /// necessarily contains a plain straight flush), so this only
    /// affects the DISPLAYED name, not whether the wager qualifies at
    /// all -- QualifiesEmperorsTreasure() doesn't need to change.
    /// </summary>
    public string EmperorsTreasureCategoryName()
    {
        if (IsSevenCardStraightFlushNoJoker()) return "7 Card Straight Flush (No Joker)";
        if (IsRoyalFlushWithExtraAceOrKing()) return "Royal Flush with Additional Ace/King Suited";
        if (IsSevenCardStraightFlushWithJoker()) return "7 Card Straight Flush (with Joker)";
        return BestSevenCardCategoryName();
    }

    private static bool FormsConsecutiveRun(List<int> sortedRanks)
    {
        for (int i = 1; i < sortedRanks.Count; i++)
        {
            if (sortedRanks[i] != sortedRanks[i - 1] + 1) return false;
        }
        return true;
    }

    private static bool FormsRunWithOneJokerGap(List<int> sortedRanks)
    {
        if (sortedRanks.Count != 6) return false;
        if (sortedRanks.Distinct().Count() != 6) return false;

        int span = sortedRanks[^1] - sortedRanks[0];
        // 5: six already-consecutive ranks (Joker extends the run by
        // one card at either end). 6: one single gap within a 7-wide
        // span (Joker fills that gap) -- six distinct ranks fitting a
        // 7-wide span with no duplicates has exactly one gap by
        // construction.
        return span == 5 || span == 6;
    }

    private string TwoCardRankLabel(int card1, int card2) => $"{DisplayRank(card1)}, {DisplayRank(card2)}";

    private string DisplayRank(int cardIndex) => _cards[cardIndex]!.Rank == "T" ? "10" : _cards[cardIndex]!.Rank;

    // ============================================================
    // HOUSE WAY RULES
    // ============================================================

    private void ApplyFiveAcesRule(ref int low1, ref int low2, int[] high)
    {
        if (ExactPairRankExists(13))
        {
            GetTwoCardsOfRank(13, out low1, out low2);
            GetAllExceptTwo(low1, low2, high);
            return;
        }

        int[] aceIndexes = GetAceIndexes();
        low1 = aceIndexes[1];
        low2 = aceIndexes[2];
        GetAllExceptTwo(low1, low2, high);
    }

    private void ApplyFourKindRule(ref int low1, ref int low2, int[] high)
    {
        int quadRank = FourKindRank();
        (int q1, int q2, int q3, int q4) = GetFourCardsOfRank(quadRank);

        // FOUR ACES
        if (quadRank == 14)
        {
            int otherPair = HighestExactPairExcluding(14);

            if (otherPair > 0)
            {
                if (otherPair <= 6)
                {
                    low1 = q1;
                    low2 = q2;
                }
                else
                {
                    GetTwoCardsOfRank(otherPair, out low1, out low2);
                }

                GetAllExceptTwo(low1, low2, high);
                return;
            }

            low1 = q1;
            low2 = q2;
            GetAllExceptTwo(low1, low2, high);
            return;
        }

        // TWO THROUGH SIX - keep four together.
        if (quadRank <= 6)
        {
            SetHighestTwoRemaining(q1, q2, q3, q4, out low1, out low2);
            GetAllExceptTwo(low1, low2, high);
            return;
        }

        // SEVEN THROUGH TEN
        if (quadRank <= 10)
        {
            if (HighestRemainingRankFromQuad(q1, q2, q3, q4) >= 11)
            {
                SetHighestTwoRemaining(q1, q2, q3, q4, out low1, out low2);
            }
            else
            {
                low1 = q1;
                low2 = q2;
            }

            GetAllExceptTwo(low1, low2, high);
            return;
        }

        // JACKS THROUGH KINGS - split the four of a kind.
        low1 = q1;
        low2 = q2;
        GetAllExceptTwo(low1, low2, high);
    }

    private void ApplyFullHouseRule(ref int low1, ref int low2, int[] high)
    {
        int tripRankValue = FullHouseTripRank();
        int pair1 = HighestExactPairExcluding(tripRankValue);
        int pair2 = SecondExactPairExcluding(tripRankValue, pair1);

        // FULL HOUSE + EXTRA PAIR
        if (pair2 > 0)
        {
            int selectedPair = pair1 > pair2 ? pair1 : pair2;
            GetTwoCardsOfRank(selectedPair, out low1, out low2);
            GetAllExceptTwo(low1, low2, high);
            return;
        }

        // PAIR OF TWOS + A-K
        if (pair1 == 2 && HasRank(14) && HasRank(13))
        {
            GetOneCardOfRank(14, out low1);
            GetOneCardOfRank(13, out low2);
            GetAllExceptTwo(low1, low2, high);
            return;
        }

        // NORMAL FULL HOUSE
        GetTwoCardsOfRank(pair1, out low1, out low2);
        GetAllExceptTwo(low1, low2, high);
    }

    private void ApplyTwoTripsRule(ref int low1, ref int low2, int[] high)
    {
        int highTrip = HighestTripRank();
        GetTwoCardsOfRank(highTrip, out low1, out low2);
        GetAllExceptTwo(low1, low2, high);
    }

    private void ApplyThreePairRule(ref int low1, ref int low2, int[] high)
    {
        int highPair = HighestExactPair();
        GetTwoCardsOfRank(highPair, out low1, out low2);
        GetAllExceptTwo(low1, low2, high);
    }

    /// <summary>If split: lower pair goes Low. If kept: highest two singletons go Low.</summary>
    private void ApplyTwoPairRule(ref int low1, ref int low2, int[] high)
    {
        int highPair = HighestExactPair();
        int lowPair = LowestExactPair();

        if (ShouldSplitTwoPair())
        {
            GetTwoCardsOfRank(lowPair, out low1, out low2);
            GetAllExceptTwo(low1, low2, high);
            return;
        }

        SetHighestTwoNonPairCards(highPair, lowPair, out low1, out low2);
        GetAllExceptTwo(low1, low2, high);
    }

    private void ApplyTripsRule(ref int low1, ref int low2, int[] high)
    {
        int trip = TripRank();

        if (trip == 14)
        {
            // House rule: pair of Aces plays in HIGH. The remaining single
            // Ace pairs with the best available non-Ace kicker in LOW - the
            // pair itself stays in High, it does not go to Low with the lone Ace.
            GetOneCardOfRank(14, out low1);
            GetHighestExcludingRank(14, out low2);
            GetAllExceptTwo(low1, low2, high);
            return;
        }

        SetHighestTwoExcludingRank(trip, out low1, out low2);
        GetAllExceptTwo(low1, low2, high);
    }

    /// <summary>Pair remains High. Two highest remaining cards become Low.</summary>
    private void ApplyOnePairRule(ref int low1, ref int low2, int[] high)
    {
        int pairRank = HighestExactPair();
        SetHighestTwoExcludingRank(pairRank, out low1, out low2);
        GetAllExceptTwo(low1, low2, high);
    }

    /// <summary>Highest card remains High. Second and third highest become Low.</summary>
    private void ApplyNoPairRule(ref int low1, ref int low2, int[] high)
    {
        int[] idx = new int[8];
        for (int i = 1; i <= 7; i++) idx[i] = i;

        for (int i = 1; i <= 6; i++)
        {
            for (int j = i + 1; j <= 7; j++)
            {
                if (HouseRank(idx[j]) > HouseRank(idx[i]))
                {
                    (idx[i], idx[j]) = (idx[j], idx[i]);
                }
            }
        }

        low1 = idx[2];
        low2 = idx[3];

        int n = 0;
        for (int i = 1; i <= 7; i++)
        {
            if (i != low1 && i != low2)
            {
                n++;
                high[n] = i;
            }
        }
    }

    /// <summary>
    /// Enumerates every possible five-card special hand (Straight,
    /// Flush, Straight Flush, Royal Flush). The candidate producing
    /// the strongest Low Hand is selected; a Low tie is broken by the
    /// stronger High Hand.
    /// </summary>
    private void ApplySpecialHandRule(ref int low1, ref int low2, int[] high)
    {
        int[] candidateHigh = new int[6];
        int[] bestHigh = new int[6];

        double bestLowScore = -1;
        double bestHighScore = -1;
        int bestCategory = -1;

        for (int i = 1; i <= 3; i++)
        {
            for (int j = i + 1; j <= 4; j++)
            {
                for (int k = j + 1; k <= 5; k++)
                {
                    for (int l = k + 1; l <= 6; l++)
                    {
                        for (int m = l + 1; m <= 7; m++)
                        {
                            candidateHigh[1] = i;
                            candidateHigh[2] = j;
                            candidateHigh[3] = k;
                            candidateHigh[4] = l;
                            candidateHigh[5] = m;

                            int category = FiveCardCategoryFromIndexes(candidateHigh);

                            if (IsSpecialCategory(category))
                            {
                                (int candLow1, int candLow2) = FindRemainingTwo(candidateHigh);
                                double candLowScore = TwoCardScore(candLow1, candLow2);
                                double candHighScore = FiveCardScoreFromIndexes(candidateHigh);

                                if (candLowScore > bestLowScore)
                                {
                                    bestLowScore = candLowScore;
                                    bestHighScore = candHighScore;
                                    bestCategory = category;
                                    Array.Copy(candidateHigh, bestHigh, 6);
                                    low1 = candLow1;
                                    low2 = candLow2;
                                }
                                else if (candLowScore == bestLowScore && candHighScore > bestHighScore)
                                {
                                    bestHighScore = candHighScore;
                                    bestCategory = category;
                                    Array.Copy(candidateHigh, bestHigh, 6);
                                    low1 = candLow1;
                                    low2 = candLow2;
                                }
                            }
                        }
                    }
                }
            }
        }

        if (bestCategory < 0)
        {
            ApplyNoPairRule(ref low1, ref low2, high);
            return;
        }

        Array.Copy(bestHigh, high, 6);
    }

    private bool ShouldSplitTwoPair()
    {
        int p1 = HighestExactPair();
        int p2 = LowestExactPair();

        if (p1 == 0 || p2 == 0) return false;

        // Pair of Aces + another pair: always split.
        if (p1 == 14 || p2 == 14) return true;

        // Ace singleton: Jacks-or-better + Sevens-or-better.
        if (HasAceNotPartOfPair(p1, p2))
            return p1 >= 11 && p2 >= 7;

        // No Ace: Sevens or better means split.
        return p1 >= 7 || p2 >= 7;
    }

    private bool HasSpecialHighHand()
    {
        int[] idx = new int[6];

        for (int i = 1; i <= 3; i++)
        {
            for (int j = i + 1; j <= 4; j++)
            {
                for (int k = j + 1; k <= 5; k++)
                {
                    for (int l = k + 1; l <= 6; l++)
                    {
                        for (int m = l + 1; m <= 7; m++)
                        {
                            idx[1] = i; idx[2] = j; idx[3] = k; idx[4] = l; idx[5] = m;
                            if (IsSpecialCategory(FiveCardCategoryFromIndexes(idx)))
                                return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>9 = Royal Flush, 8 = Straight Flush, 5 = Flush, 4 = Straight.</summary>
    private static bool IsSpecialCategory(int category) => category is 4 or 5 or 8 or 9;

    private (int Low1, int Low2) FindRemainingTwo(int[] high)
    {
        int low1 = 0, low2 = 0, n = 0;

        for (int i = 1; i <= 7; i++)
        {
            bool found = false;
            for (int j = 1; j <= 5; j++)
            {
                if (high[j] == i) { found = true; break; }
            }

            if (!found)
            {
                n++;
                if (n == 1) low1 = i;
                else { low2 = i; break; }
            }
        }

        return (low1, low2);
    }

    private void GetAllExceptTwo(int low1, int low2, int[] high)
    {
        int n = 0;
        for (int i = 1; i <= 7; i++)
        {
            if (i != low1 && i != low2)
            {
                n++;
                high[n] = i;
            }
        }
    }

    private void SetHighestTwoRemaining(int q1, int q2, int q3, int q4, out int low1, out int low2)
    {
        low1 = 0; low2 = 0;
        int best1 = -1, best2 = -1;

        for (int i = 1; i <= 7; i++)
        {
            if (i != q1 && i != q2 && i != q3 && i != q4)
            {
                int r = HouseRank(i);
                if (r > best1)
                {
                    best2 = best1; low2 = low1;
                    best1 = r; low1 = i;
                }
                else if (r > best2)
                {
                    best2 = r; low2 = i;
                }
            }
        }
    }

    private void SetHighestTwoNonPairCards(int pair1, int pair2, out int low1, out int low2)
    {
        low1 = 0; low2 = 0;
        int best1 = -1, best2 = -1;

        for (int i = 1; i <= 7; i++)
        {
            if (HouseRank(i) != pair1 && HouseRank(i) != pair2)
            {
                int r = HouseRank(i);
                if (r > best1)
                {
                    best2 = best1; low2 = low1;
                    best1 = r; low1 = i;
                }
                else if (r > best2)
                {
                    best2 = r; low2 = i;
                }
            }
        }
    }

    private void SetHighestTwoExcludingRank(int excludedRank, out int low1, out int low2)
    {
        low1 = 0; low2 = 0;
        int best1 = -1, best2 = -1;

        for (int i = 1; i <= 7; i++)
        {
            if (HouseRank(i) != excludedRank)
            {
                int r = HouseRank(i);
                if (r > best1)
                {
                    best2 = best1; low2 = low1;
                    best1 = r; low1 = i;
                }
                else if (r > best2)
                {
                    best2 = r; low2 = i;
                }
            }
        }
    }

    private void GetHighestExcludingRank(int excludedRank, out int resultIndex)
    {
        resultIndex = 0;
        int best = -1;

        for (int i = 1; i <= 7; i++)
        {
            if (HouseRank(i) != excludedRank && HouseRank(i) > best)
            {
                best = HouseRank(i);
                resultIndex = i;
            }
        }
    }

    private int HighestRemainingRankFromQuad(int q1, int q2, int q3, int q4)
    {
        int result = 0;
        for (int i = 1; i <= 7; i++)
        {
            if (i != q1 && i != q2 && i != q3 && i != q4)
            {
                int r = HouseRank(i);
                if (r > result) result = r;
            }
        }
        return result;
    }

    private void GetTwoCardsOfRank(int rank, out int card1, out int card2)
    {
        card1 = 0; card2 = 0;
        int found = 0;

        for (int i = 1; i <= 7; i++)
        {
            if (HouseRank(i) == rank)
            {
                found++;
                if (found == 1) card1 = i;
                else { card2 = i; return; }
            }
        }
    }

    private void GetOneCardOfRank(int rank, out int resultIndex)
    {
        resultIndex = 0;
        for (int i = 1; i <= 7; i++)
        {
            if (HouseRank(i) == rank) { resultIndex = i; return; }
        }
    }

    private (int C1, int C2, int C3, int C4) GetFourCardsOfRank(int rank)
    {
        int c1 = 0, c2 = 0, c3 = 0, c4 = 0, n = 0;

        for (int i = 1; i <= 7; i++)
        {
            if (HouseRank(i) == rank)
            {
                n++;
                switch (n)
                {
                    case 1: c1 = i; break;
                    case 2: c2 = i; break;
                    case 3: c3 = i; break;
                    case 4: c4 = i; break;
                }
                if (n == 4) break;
            }
        }

        return (c1, c2, c3, c4);
    }

    private int[] GetAceIndexes()
    {
        int[] indexes = new int[6];
        int n = 0;

        for (int i = 1; i <= 7; i++)
        {
            if (HouseRank(i) == 14)
            {
                n++;
                if (n <= 5) indexes[n] = i;
            }
        }

        return indexes;
    }

    // ============================================================
    // RANK/CATEGORY QUERIES OVER THE CURRENT 7 CARDS
    // ============================================================

    private int FourKindRank()
    {
        for (int r = 14; r >= 2; r--)
        {
            if (CountRank(r) >= 4) return r;
        }
        return 0;
    }

    private bool IsFiveAces() => HasJoker() && NaturalRankCount(14) == 4;

    private bool HasFullHouse()
    {
        int tripRankValue = TripRank();
        if (tripRankValue == 0) return false;

        for (int r = 14; r >= 2; r--)
        {
            if (r != tripRankValue && CountRank(r) >= 2) return true;
        }
        return false;
    }

    private int FullHouseTripRank() => TripRank();

    private int ExactPairCount()
    {
        int count = 0;
        for (int r = 2; r <= 14; r++)
        {
            if (CountRank(r) == 2) count++;
        }
        return count;
    }

    private int HighestExactPair()
    {
        for (int r = 14; r >= 2; r--)
        {
            if (CountRank(r) == 2) return r;
        }
        return 0;
    }

    private int LowestExactPair()
    {
        for (int r = 2; r <= 14; r++)
        {
            if (CountRank(r) == 2) return r;
        }
        return 0;
    }

    private bool ExactPairRankExists(int rank) => CountRank(rank) == 2;

    private int HighestExactPairExcluding(int excludedRank)
    {
        for (int r = 14; r >= 2; r--)
        {
            if (r != excludedRank && CountRank(r) == 2) return r;
        }
        return 0;
    }

    private int SecondExactPairExcluding(int excludedRank, int firstRank)
    {
        for (int r = 14; r >= 2; r--)
        {
            if (r != excludedRank && r != firstRank && CountRank(r) == 2) return r;
        }
        return 0;
    }

    private int TripCount()
    {
        int count = 0;
        for (int r = 2; r <= 14; r++)
        {
            if (CountRank(r) >= 3) count++;
        }
        return count;
    }

    private int TripRank()
    {
        for (int r = 14; r >= 2; r--)
        {
            if (CountRank(r) >= 3) return r;
        }
        return 0;
    }

    private int HighestTripRank() => TripRank();

    /// <summary>Joker counts as Ace for rank-based House Way decisions.</summary>
    private int CountRank(int rank)
    {
        int count = 0;
        for (int i = 1; i <= 7; i++)
        {
            PaiGowCard card = _cards[i]!;
            if (IsJokerCard(card))
            {
                if (rank == 14) count++;
            }
            else if (RankValue(card) == rank)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>Joker excluded.</summary>
    private int NaturalRankCount(int rank)
    {
        int count = 0;
        for (int i = 1; i <= 7; i++)
        {
            PaiGowCard card = _cards[i]!;
            if (!IsJokerCard(card) && RankValue(card) == rank) count++;
        }
        return count;
    }

    private bool HasRank(int rank) => CountRank(rank) > 0;

    private bool HasJoker()
    {
        for (int i = 1; i <= 7; i++)
        {
            if (IsJokerCard(_cards[i]!)) return true;
        }
        return false;
    }

    private bool HasAceNotPartOfPair(int pair1, int pair2)
    {
        if (pair1 == 14 || pair2 == 14) return false;

        for (int i = 1; i <= 7; i++)
        {
            if (HouseRank(i) == 14) return true;
        }
        return false;
    }

    /// <summary>Joker behaves as Ace for ordinary rank decisions.</summary>
    private int HouseRank(int cardIndex) => IsJokerCard(_cards[cardIndex]!) ? 14 : RankValue(_cards[cardIndex]!);

    private static int RankValue(PaiGowCard card) => card.Rank switch
    {
        "A" => 14,
        "K" => 13,
        "Q" => 12,
        "J" => 11,
        "T" or "10" => 10,
        _ => int.TryParse(card.Rank, out int n) ? n : 0
    };

    private static bool IsJokerCard(PaiGowCard card)
    {
        string r = card.Rank;
        string c = card.ImageCode;
        return r is "JOKER" or "JK" or "JOK" || c is "JOKER" or "JK" or "JOK";
    }

    // ============================================================
    // FIVE-CARD / TWO-CARD SCORING
    // ============================================================

    private double FiveCardScoreFromIndexes(int[] indexes) => EvaluateFive(indexes[1], indexes[2], indexes[3], indexes[4], indexes[5], out _, out _);

    private int FiveCardCategoryFromIndexes(int[] indexes)
    {
        EvaluateFive(indexes[1], indexes[2], indexes[3], indexes[4], indexes[5], out int category, out _);
        return category;
    }

    /// <summary>
    /// Categories: 10=Five Aces, 9=Royal Flush, 8=Straight Flush,
    /// 7=Four of a Kind, 6=Full House, 5=Flush, 4=Straight,
    /// 3=Three of a Kind, 2=Two Pair, 1=Pair, 0=High Card.
    /// </summary>
    private double EvaluateFive(int i1, int i2, int i3, int i4, int i5, out int bestCategory, out double bestTieScore)
    {
        int[] indexes = { 0, i1, i2, i3, i4, i5 };
        int[] ranks = new int[6];
        string[] suits = new string[6];

        int jokerPosition = 0, jokerCount = 0;

        bestCategory = -1;
        bestTieScore = -1;

        for (int i = 1; i <= 5; i++)
        {
            PaiGowCard card = _cards[indexes[i]]!;
            if (IsJokerCard(card))
            {
                jokerCount++;
                jokerPosition = i;
            }
            else
            {
                ranks[i] = RankValue(card);
                suits[i] = card.Suit.Trim().ToUpperInvariant();
            }
        }

        // NO JOKER
        if (jokerCount == 0)
        {
            double totalScore = ScoreConcreteHand(ranks, suits, false);
            bestCategory = (int)(totalScore / 1000000000000.0);
            bestTieScore = totalScore - (bestCategory * 1000000000000.0);
            return totalScore;
        }

        // JOKER SUBSTITUTIONS
        string[] suitList = { "", "C", "D", "H", "S" };
        int[] testRanks = new int[6];
        string[] testSuits = new string[6];

        for (int r = 2; r <= 14; r++)
        {
            for (int s = 1; s <= 4; s++)
            {
                for (int i = 1; i <= 5; i++)
                {
                    testRanks[i] = ranks[i];
                    testSuits[i] = suits[i];
                }

                testRanks[jokerPosition] = r;
                testSuits[jokerPosition] = suitList[s];

                double totalScore = ScoreConcreteHand(testRanks, testSuits, r == 14);
                int category = (int)(totalScore / 1000000000000.0);
                double tieScore = totalScore - (category * 1000000000000.0);

                bool better = totalScore > (bestCategory * 1000000000000.0 + bestTieScore);

                if (r == 14)
                {
                    if (better)
                    {
                        bestCategory = category;
                        bestTieScore = tieScore;
                    }
                }
                else if (IsSpecialCategory(category) && better)
                {
                    bestCategory = category;
                    bestTieScore = tieScore;
                }
            }
        }

        // SAFETY FALLBACK
        if (bestCategory < 0)
        {
            for (int i = 1; i <= 5; i++)
            {
                testRanks[i] = ranks[i];
                testSuits[i] = suits[i];
            }

            testRanks[jokerPosition] = 14;
            testSuits[jokerPosition] = "";

            double totalScore = ScoreConcreteHand(testRanks, testSuits, true);
            bestCategory = (int)(totalScore / 1000000000000.0);
            bestTieScore = totalScore - (bestCategory * 1000000000000.0);
        }

        return (bestCategory * 1000000000000.0) + bestTieScore;
    }

    private static double ScoreConcreteHand(int[] ranks, string[] suits, bool jokerAsAce)
    {
        int[] r = new int[6];
        for (int i = 1; i <= 5; i++) r[i] = ranks[i];

        int[] counts = new int[15]; // 2-14
        for (int i = 1; i <= 5; i++)
        {
            if (r[i] is >= 2 and <= 14) counts[r[i]]++;
        }

        // Sort ranks descending (indexes 1-5).
        for (int i = 1; i <= 4; i++)
        {
            for (int j = i + 1; j <= 5; j++)
            {
                if (r[j] > r[i]) (r[i], r[j]) = (r[j], r[i]);
            }
        }

        // FLUSH
        bool flush = suits[1].Length > 0;
        if (flush)
        {
            for (int i = 2; i <= 5; i++)
            {
                if (!string.Equals(suits[i], suits[1], StringComparison.OrdinalIgnoreCase))
                {
                    flush = false;
                    break;
                }
            }
        }

        // STRAIGHT
        bool straight = DetermineStraight(r, out int straightHigh);

        // FIVE ACES
        if (jokerAsAce && counts[14] == 5)
        {
            return (10.0 * 1000000000000.0) + 1000000.0;
        }

        // ROYAL FLUSH
        if (flush && straight && straightHigh == 14)
        {
            return (9.0 * 1000000000000.0) + 100000000.0;
        }

        // STRAIGHT FLUSH
        if (flush && straight)
        {
            return (8.0 * 1000000000000.0) + StraightTieValue(straightHigh);
        }

        // FOUR OF A KIND
        int quadRank = 0;
        for (int i = 14; i >= 2; i--)
        {
            if (counts[i] == 4) { quadRank = i; break; }
        }

        if (quadRank > 0)
        {
            int kicker = 0;
            for (int i = 14; i >= 2; i--)
            {
                if (i != quadRank && counts[i] > 0) { kicker = i; break; }
            }

            return (7.0 * 1000000000000.0) + (quadRank * 1000000.0) + (kicker * 1000.0);
        }

        // FULL HOUSE
        int tripRankValue = 0;
        for (int i = 14; i >= 2; i--)
        {
            if (counts[i] == 3) { tripRankValue = i; break; }
        }

        if (tripRankValue > 0)
        {
            int pairRank = 0;
            for (int i = 14; i >= 2; i--)
            {
                if (i != tripRankValue && counts[i] >= 2) { pairRank = i; break; }
            }

            if (pairRank > 0)
            {
                return (6.0 * 1000000000000.0) + (tripRankValue * 1000000.0) + (pairRank * 1000.0);
            }
        }

        // FLUSH
        if (flush)
        {
            return (5.0 * 1000000000000.0) + (r[1] * 100000000.0) + (r[2] * 1000000.0) + (r[3] * 10000.0) + (r[4] * 100.0) + r[5];
        }

        // STRAIGHT
        if (straight)
        {
            return (4.0 * 1000000000000.0) + StraightTieValue(straightHigh);
        }

        // THREE OF A KIND
        if (tripRankValue > 0)
        {
            int tripK1 = 0, tripK2 = 0;
            for (int i = 14; i >= 2; i--)
            {
                if (i != tripRankValue && counts[i] > 0)
                {
                    if (tripK1 == 0) tripK1 = i;
                    else { tripK2 = i; break; }
                }
            }

            return (3.0 * 1000000000000.0) + (tripRankValue * 1000000.0) + (tripK1 * 1000.0) + tripK2;
        }

        // TWO PAIR
        int[] pairRanks = new int[3];
        int pairCount = 0;
        for (int i = 14; i >= 2; i--)
        {
            if (counts[i] == 2)
            {
                pairCount++;
                if (pairCount <= 2) pairRanks[pairCount] = i;
            }
        }

        if (pairCount >= 2)
        {
            int kicker = 0;
            for (int i = 14; i >= 2; i--)
            {
                if (counts[i] > 0 && i != pairRanks[1] && i != pairRanks[2]) { kicker = i; break; }
            }

            return (2.0 * 1000000000000.0) + (pairRanks[1] * 1000000.0) + (pairRanks[2] * 1000.0) + kicker;
        }

        // ONE PAIR
        if (pairCount == 1)
        {
            int k1 = 0, k2 = 0, k3 = 0;
            for (int i = 14; i >= 2; i--)
            {
                if (i != pairRanks[1] && counts[i] > 0)
                {
                    if (k1 == 0) k1 = i;
                    else if (k2 == 0) k2 = i;
                    else { k3 = i; break; }
                }
            }

            return (1.0 * 1000000000000.0) + (pairRanks[1] * 100000000.0) + (k1 * 1000000.0) + (k2 * 10000.0) + k3;
        }

        // HIGH CARD
        return (r[1] * 100000000.0) + (r[2] * 1000000.0) + (r[3] * 10000.0) + (r[4] * 100.0) + r[5];
    }

    private static bool DetermineStraight(int[] r, out int straightHigh)
    {
        straightHigh = 0;

        // A-2-3-4-5
        if (r[1] == 14 && r[2] == 5 && r[3] == 4 && r[4] == 3 && r[5] == 2)
        {
            straightHigh = 5;
            return true;
        }

        // Normal straight
        for (int i = 1; i <= 4; i++)
        {
            if (r[i] - r[i + 1] != 1) return false;
        }

        straightHigh = r[1];
        return true;
    }

    private static int StraightTieValue(int straightHigh) => straightHigh switch
    {
        14 => 100,
        5 => 99,
        _ => 10 + straightHigh
    };

    /// <summary>Pair beats any non-pair. Otherwise compare high card, then low card.</summary>
    private double TwoCardScore(int card1, int card2)
    {
        int r1 = HouseRank(card1);
        int r2 = HouseRank(card2);

        if (r1 == r2)
        {
            return (1.0 * 1000000000.0) + (r1 * 1000000.0);
        }

        int highRank = Math.Max(r1, r2);
        int lowRank = Math.Min(r1, r2);

        return (highRank * 1000000.0) + lowRank;
    }

    // ============================================================
    // HAND COMPARISON
    // ============================================================

    private bool HandMatches(int userLow1, int userLow2, int[] userHigh, int correctLow1, int correctLow2, int[] correctHigh)
    {
        // Compared by RANK, not by exact card index. With duplicate ranks
        // in the 7-card deal, an equally correct answer may use the OTHER
        // instance of the same rank - Low never depends on suit or which
        // physical duplicate was used. NOTE: High only depends on suit when
        // the correct answer is specifically a Flush/Straight Flush - that
        // narrower case isn't covered by this rank-only check.
        int[] userLowRanks = { HouseRank(userLow1), HouseRank(userLow2) };
        int[] correctLowRanks = { HouseRank(correctLow1), HouseRank(correctLow2) };

        if (!RanksMatchNumeric(userLowRanks, correctLowRanks)) return false;

        int[] userHighRanks = new int[5];
        int[] correctHighRanks = new int[5];
        for (int i = 0; i < 5; i++)
        {
            userHighRanks[i] = HouseRank(userHigh[i + 1]);
            correctHighRanks[i] = HouseRank(correctHigh[i + 1]);
        }

        return RanksMatchNumeric(userHighRanks, correctHighRanks);
    }

    /// <summary>Order-independent rank-multiset comparison. Each rank in a must be consumed by exactly one matching rank in b.</summary>
    private static bool RanksMatchNumeric(int[] a, int[] b)
    {
        bool[] used = new bool[a.Length];

        for (int i = 0; i < a.Length; i++)
        {
            bool found = false;
            for (int j = 0; j < b.Length; j++)
            {
                if (!used[j] && a[i] == b[j])
                {
                    used[j] = true;
                    found = true;
                    break;
                }
            }

            if (!found) return false;
        }

        return true;
    }

    // ============================================================
    // DESCRIPTIONS
    // ============================================================

    private string DescribeLowHand(int card1, int card2)
    {
        int r1 = HouseRank(card1);
        int r2 = HouseRank(card2);

        if (r1 == r2) return $"Pair of {RankNamePlural(r1)}";
        return r1 > r2 ? $"{RankNameSingular(r1)} high" : $"{RankNameSingular(r2)} high";
    }

    private string DescribeHighHand(int[] indexes)
    {
        EvaluateFive(indexes[1], indexes[2], indexes[3], indexes[4], indexes[5], out int category, out _);

        return category switch
        {
            10 => "Five Aces",
            9 => "Royal Flush",
            8 => "Straight Flush",
            7 => $"Four of a Kind — {DescribeGroupRank(indexes, 4)}",
            6 => "Full House",
            5 => $"Flush — {HighestFiveCardRankName(indexes)} high",
            4 => "Straight",
            3 => $"Three of a Kind — {DescribeGroupRank(indexes, 3)}",
            2 => $"Two Pair — {DescribeTwoPairs(indexes)}",
            1 => $"Pair of {DescribeGroupRank(indexes, 2)}",
            _ => $"{HighestFiveCardRankName(indexes)} high"
        };
    }

    private string DescribeTwoPairs(int[] indexes)
    {
        int[] counts = new int[15];
        for (int i = 1; i <= 5; i++)
        {
            int r = HouseRank(indexes[i]);
            if (r is >= 2 and <= 14) counts[r]++;
        }

        int firstPair = 0, secondPair = 0;
        for (int r = 14; r >= 2; r--)
        {
            if (counts[r] >= 2)
            {
                if (firstPair == 0) firstPair = r;
                else { secondPair = r; break; }
            }
        }

        return $"{RankNamePlural(firstPair)} and {RankNamePlural(secondPair)}";
    }

    private string DescribeGroupRank(int[] indexes, int groupSize)
    {
        int[] counts = new int[15];
        for (int i = 1; i <= 5; i++)
        {
            int r = HouseRank(indexes[i]);
            if (r is >= 2 and <= 14) counts[r]++;
        }

        for (int r = 14; r >= 2; r--)
        {
            if (counts[r] >= groupSize) return RankNamePlural(r);
        }

        return "";
    }

    private string HighestFiveCardRankName(int[] indexes)
    {
        int highest = 0;
        for (int i = 1; i <= 5; i++)
        {
            if (HouseRank(indexes[i]) > highest) highest = HouseRank(indexes[i]);
        }

        return RankNameSingular(highest);
    }

    private static string RankNamePlural(int rank) => rank switch
    {
        14 => "Aces", 13 => "Kings", 12 => "Queens", 11 => "Jacks", 10 => "Tens",
        9 => "Nines", 8 => "Eights", 7 => "Sevens", 6 => "Sixes", 5 => "Fives",
        4 => "Fours", 3 => "Threes", 2 => "Twos", _ => "Unknown"
    };

    private static string RankNameSingular(int rank) => rank switch
    {
        14 => "Ace", 13 => "King", 12 => "Queen", 11 => "Jack", 10 => "Ten",
        9 => "Nine", 8 => "Eight", 7 => "Seven", 6 => "Six", 5 => "Five",
        4 => "Four", 3 => "Three", 2 => "Two", _ => "Unknown"
    };
}

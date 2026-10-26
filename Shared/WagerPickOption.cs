namespace DealerSchoolTrainerWeb.Shared;

// One choice in a WagerPick pop-up: Key identifies it, Text is shown on
// the choice and under the wager name once picked, Css colors both.
public sealed record WagerPickOption(string Key, string Text, string Css);

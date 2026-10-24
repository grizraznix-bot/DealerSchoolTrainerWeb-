namespace DealerSchoolTrainerWeb.Core;

// Lets the sidebar / phone menu tell the game page that's already open
// to go back to its own start screen -- the same thing that page's Back
// button does. Needed because tapping the menu link for the page you're
// already on doesn't reload it, so on its own it would do nothing.
public sealed class GameResetService
{
    public event Action? ResetRequested;

    public void RequestReset() => ResetRequested?.Invoke();
}

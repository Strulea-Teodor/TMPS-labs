namespace CinemaBooking.Commands;

/// <summary>
/// COMMAND - the "Command" interface.
///
/// A request ("book these seats", "move this screening") is turned into an
/// object. The object carries everything needed to perform the request and
/// everything needed to reverse it, so requests can be logged, queued and
/// undone by code that knows nothing about what they actually do.
/// </summary>
public interface ICommand
{
    string Description { get; }
    void Execute();
    void Undo();
}

using CinemaBooking.Domain;

namespace CinemaBooking.Commands;

/// <summary>
/// COMMAND - concrete command: move a screening to another time. The
/// "receiver" is the Screening. Undo moves it back to where it was.
/// </summary>
public class RescheduleScreeningCommand : ICommand
{
    private readonly Screening _screening;
    private readonly DateTime _newStartTime;

    private DateTime? _previousStartTime;   // remembered by Execute for Undo

    public RescheduleScreeningCommand(Screening screening, DateTime newStartTime)
    {
        _screening = screening;
        _newStartTime = newStartTime;
    }

    public string Description =>
        $"Move screening #{_screening.Id} ({_screening.Movie.Title}) to {_newStartTime:ddd dd MMM HH:mm}";

    public void Execute()
    {
        _previousStartTime = _screening.StartTime;
        _screening.Reschedule(_newStartTime);
    }

    public void Undo()
    {
        if (_previousStartTime is null)
            throw new InvalidOperationException("Nothing to undo: this command was never executed.");

        _screening.Reschedule(_previousStartTime.Value);
        _previousStartTime = null;
    }
}

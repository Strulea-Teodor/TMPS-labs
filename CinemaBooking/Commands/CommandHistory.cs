namespace CinemaBooking.Commands;

/// <summary>
/// COMMAND - the "Invoker". Runs commands and remembers the ones that
/// succeeded, most recent on top, so the last action can be undone.
///
/// It works only with ICommand: it has no idea whether a command sells
/// tickets or moves a screening, so new kinds of command need no change here.
/// </summary>
public class CommandHistory
{
    private readonly Stack<ICommand> _executed = new();

    public int Count => _executed.Count;

    /// <summary>Descriptions of the executed commands, oldest first.</summary>
    public IReadOnlyList<string> Log
    {
        get
        {
            var descriptions = _executed.Select(c => c.Description).ToList();   // newest first
            descriptions.Reverse();
            return descriptions;
        }
    }

    /// <summary>Runs the command. It is recorded only if it did not throw.</summary>
    public void Execute(ICommand command)
    {
        command.Execute();
        _executed.Push(command);
    }

    /// <summary>Undoes the most recent command. Returns false if there is nothing to undo.</summary>
    public bool UndoLast()
    {
        if (_executed.Count == 0)
        {
            Console.WriteLine("    Nothing to undo.");
            return false;
        }

        ICommand command = _executed.Pop();
        Console.WriteLine($"    Undo: {command.Description}");
        command.Undo();
        return true;
    }
}

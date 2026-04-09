using System.Collections.Generic;

namespace AcrossReportDesigner.UndoRedo;

public sealed class CompositeCommand : IUndoableCommand
{
    private readonly List<IUndoableCommand> _commands;
    public CompositeCommand(IEnumerable<IUndoableCommand> commands)
    {
        _commands = new List<IUndoableCommand>(commands);
    }
    public void Redo()
    {
        // 追加順にRedo
        foreach (var c in _commands)
            c.Redo();
    }
    public void Undo()
    {
        // 逆順にUndo（重要）
        for (int i = _commands.Count - 1; i >= 0; i--)
            _commands[i].Undo();
    }
    public bool IsEmpty => _commands.Count == 0;
}

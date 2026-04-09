using System;

namespace AcrossReportDesigner.UndoRedo;

public sealed class PropertyChangeCommand : IUndoableCommand
{
    private readonly Action _undo;
    private readonly Action _redo;
    public PropertyChangeCommand(Action redo, Action undo)
    {
        _redo = redo;
        _undo = undo;
    }
    public void Undo() => _undo();
    public void Redo() => _redo();
}

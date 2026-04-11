using System;
using System.Collections.Generic;

namespace AcrossReportDesigner.UndoRedo;

public sealed class UndoRedoManager
{
    private readonly Stack<IUndoableCommand> _undoStack = new();
    private readonly Stack<IUndoableCommand> _redoStack = new();

    // ✅ CanUndo / CanRedo（ボタン有効化用）
    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    // ✅ 状態変化通知（ボタン有効化をViewに伝える）
    public event Action? StateChanged;

    public void Execute(IUndoableCommand command)
    {
        if (command == null) return;
        command.Redo();
        _undoStack.Push(command);
        _redoStack.Clear();
        StateChanged?.Invoke();
    }

    public void Undo()
    {
        if (_undoStack.Count == 0) return;
        var cmd = _undoStack.Pop();
        cmd.Undo();
        _redoStack.Push(cmd);
        StateChanged?.Invoke();
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;
        var cmd = _redoStack.Pop();
        cmd.Redo();
        _undoStack.Push(cmd);
        StateChanged?.Invoke();
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke();
    }
}

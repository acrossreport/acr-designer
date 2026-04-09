using System.Collections.Generic;

namespace AcrossReportDesigner.UndoRedo;

public sealed class UndoRedoManager
{
    private readonly Stack<IUndoableCommand> _undoStack = new();
    private readonly Stack<IUndoableCommand> _redoStack = new();
    /// <summary>
    /// コマンド実行（Redoを呼び出して履歴に積む）
    /// </summary>
    public void Execute(IUndoableCommand command)
    {
        if (command == null)return;
        command.Redo();
        _undoStack.Push(command);
        _redoStack.Clear();
    }
    /// <summary>
    /// Undo 実行
    /// </summary>
    public void Undo()
    {
        if (_undoStack.Count == 0) return;
        var cmd = _undoStack.Pop();
        cmd.Undo();
        _redoStack.Push(cmd);
    }
    /// <summary>
    /// Redo 実行
    /// </summary>
    public void Redo()
    {
        if (_redoStack.Count == 0) return;
        var cmd = _redoStack.Pop();
        cmd.Redo();
        _undoStack.Push(cmd);
    }
}

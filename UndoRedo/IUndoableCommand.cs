namespace AcrossReportDesigner.UndoRedo;

public interface IUndoableCommand
{
    void Undo();
    void Redo();
}

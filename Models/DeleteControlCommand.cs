using AcrossReportDesigner.Designer;
using AcrossReportDesigner.Models;

namespace AcrossReportDesigner.UndoRedo;

public sealed class DeleteControlCommand : IUndoableCommand
{
    private readonly DesignerCanvasLogic _logic;
    private readonly DesignControl _control;
    private SectionDefinition? _section;
    private int _index;

    public DeleteControlCommand(
        DesignerCanvasLogic logic,
        DesignControl control)
    {
        _logic = logic;
        _control = control;
    }

    public void Redo()
    {
        _section = _logic.FindOwnerSection(_control);
        if (_section == null)return;
        _index = _logic.GetControlIndex(_section, _control);
        _logic.DeleteControl(_control);
    }
    public void Undo()
    {
        if (_section == null)return;
        _logic.InsertControl(_section, _index, _control);
    }
}

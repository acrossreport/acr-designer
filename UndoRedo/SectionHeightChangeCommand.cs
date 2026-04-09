using AcrossReportDesigner.Designer;

namespace AcrossReportDesigner.UndoRedo;

public sealed class SectionHeightChangeCommand : IUndoableCommand
{
    private readonly DesignerCanvasLogic _logic;
    private readonly string _sectionName;
    private readonly double _oldMm;
    private readonly double _newMm;
    public SectionHeightChangeCommand(
        DesignerCanvasLogic logic,
        string sectionName,
        double oldMm,
        double newMm)
    {
        _logic = logic;
        _sectionName = sectionName;
        _oldMm = oldMm;
        _newMm = newMm;
    }
    public void Redo()
    {
        _logic.UpdateSectionHeight(_sectionName, _newMm);
        _logic.Render();   // ← 修正
    }
    public void Undo()
    {
        _logic.UpdateSectionHeight(_sectionName, _oldMm);
        _logic.Render();   // ← 修正
    }
}

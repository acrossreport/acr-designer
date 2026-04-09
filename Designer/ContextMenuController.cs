using AcrossReportDesigner.UndoRedo;
using Avalonia.Controls;
using Avalonia;

namespace AcrossReportDesigner.Designer
{
    public sealed class ContextMenuController
    {
        private readonly Canvas _canvas;
        private readonly DesignerCanvasLogic _logic;
        private readonly SelectionManager _selection;
        private readonly UndoRedoManager _undo;
       
        public ContextMenuController(
            Canvas canvas,
            DesignerCanvasLogic logic,
            SelectionManager selection,
            UndoRedoManager undo)
        {
            _canvas = canvas;
            _logic = logic;
            _selection = selection;
            _undo = undo;
        }
        public void Show(Point pos)
        {
            var hasSelection = _selection.Primary != null;
            var menu = new ContextMenu();
            var addGroupItem = new MenuItem
            {
                Header = "グループ追加",
                IsEnabled = !hasSelection
            };
            addGroupItem.Click += (_, __) =>
            {
                _logic.AddGroupSection();
                _logic.Render();  // ★追加
            };
            var deleteItem = new MenuItem
            {
                Header = "削除",
                IsEnabled = hasSelection
            };
            deleteItem.Click += (_, __) =>
            {
                if (_selection.Primary == null)return;
                var cmd = new DeleteControlCommand(
                    _logic,
                    _selection.Primary);
                _undo.Execute(cmd);
                _selection.Clear();
            };
            var frontItem = new MenuItem
            {
                Header = "前面へ",
                IsEnabled = hasSelection
            };
            frontItem.Click += (_, __) =>
            {
                var ctrl = _selection.Primary;
                if (ctrl != null) _logic.MoveControlToFront(ctrl);
            };
            var backItem = new MenuItem
            {
                Header = "背面へ",
                IsEnabled = hasSelection
            };
            backItem.Click += (_, __) =>
            {
                var ctrl = _selection.Primary;
                if (ctrl != null)
                    _logic.MoveControlToBack(ctrl);
            };
            menu.Items.Add(addGroupItem);
            menu.Items.Add(new Separator());
            menu.Items.Add(deleteItem);
            menu.Items.Add(frontItem);
            menu.Items.Add(backItem);
            menu.Open(_canvas);
        }
    }
}

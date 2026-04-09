using AcrossReportDesigner.Models;
using AcrossReportDesigner.Views;
using Avalonia;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AcrossReportDesigner.Designer
{
    public sealed class SelectionManager
    {
        private readonly Canvas _canvas;
        public DesignControl? Primary { get; private set; }
        public HashSet<DesignControl> MultiSelected { get; } = new();
        public event Action<DesignControl?>? SelectionChanged;
        public SelectionManager(Canvas canvas)
        {
            _canvas = canvas;
        }
        public void Clear()
        {
            foreach (var view in _canvas.Children.OfType<DesignControlView>())
                view.SetSelected(false);
            MultiSelected.Clear();
            Primary = null;
            SelectionChanged?.Invoke(null);
        }
        public void SelectSingle(DesignControl ctrl)
        {
            Clear();
            var view = FindView(ctrl);
            if (view != null)view.SetSelected(true);
            Primary = ctrl;
            MultiSelected.Add(ctrl);
            SelectionChanged?.Invoke(ctrl);
        }
        public void SelectByRect(Rect box, bool additive)
        {
            if (!additive) Clear();
            DesignControl? last = null;
            foreach (var view in _canvas.Children.OfType<DesignControlView>())
            {
                var topLeft = view.TranslatePoint(new Point(0, 0), _canvas);
                if (topLeft == null)continue;
                var bounds = new Rect(topLeft.Value, view.Bounds.Size);
                if (box.Intersects(bounds))
                {
                    view.SetSelected(true);
                    MultiSelected.Add(view.Model);
                    last = view.Model;
                }
            }
            if (last != null)
            {
                Primary = last;
                SelectionChanged?.Invoke(last);
            }
        }
        private DesignControlView? FindView(DesignControl ctrl)
        {
            return _canvas.Children
                          .OfType<DesignControlView>()
                          .FirstOrDefault(v => v.Model == ctrl);
        }
    }
}

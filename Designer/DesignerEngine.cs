using AcrossReportDesigner.Engine;
using AcrossReportDesigner.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AcrossReportDesigner.Designer
{
    public sealed class DesignerEngine
    {
        private readonly ReportEngine _engine;

        public DesignerEngine(ReportEngine engine)
        {
            _engine = engine;
        }

        // =========================
        // 選択管理
        // =========================
        public DesignControl? SelectedControl { get; private set; }

        public void SelectControl(DesignControl ctrl)
        {
            SelectedControl = ctrl;
        }

        public void ClearSelection()
        {
            SelectedControl = null;
        }

        // =========================
        // 追加・削除（Designer操作）
        // =========================
        public void AddControl(SectionDefinition section, DesignControl ctrl)
        {
            _engine.AddControl(section, ctrl);
        }

        public void DeleteSelected()
        {
            if (SelectedControl == null)
                return;

            var owner = _engine.FindOwnerSection(SelectedControl);
            if (owner == null)
                return;

            _engine.RemoveControl(owner, SelectedControl);
            SelectedControl = null;
        }

        // =========================
        // 移動（ドラッグ後確定）
        // =========================
        public void MoveSelected(double leftMm, double topMm)
        {
            if (SelectedControl == null)
                return;

            _engine.MoveControl(SelectedControl, leftMm, topMm);
        }

        // =========================
        // セクション取得（Designer用）
        // =========================
        public SectionDefinition? FindSectionByPageMm(double pageYmm)
        {
            double pos = 0;

            foreach (var sec in _engine.Sections)
            {
                double start = pos;
                double end = pos + sec.HeightMm;

                if (pageYmm >= start && pageYmm < end)
                    return sec;

                pos += sec.HeightMm;
            }

            return null;
        }

        // =========================
        // 名前自動生成（Designer専用）
        // =========================
        public string GenerateNextName(string type)
        {
            int max = 0;

            foreach (var sec in _engine.Sections)
            {
                foreach (var c in sec.Controls)
                {
                    if (!string.Equals(c.Type, type, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (string.IsNullOrWhiteSpace(c.Name))
                        continue;

                    var digits = new string(
                        c.Name.SkipWhile(ch => !char.IsDigit(ch)).ToArray());

                    if (int.TryParse(digits, out int n))
                        if (n > max) max = n;
                }
            }

            return type + (max + 1);
        }
    }
}

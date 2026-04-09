using AcrossReportDesigner.Models;
using AcrossReportDesigner.Engines;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AcrossReportDesigner.Engine
{
    public sealed class ReportEngine
    {
        // =============================
        // Sections
        // =============================
        public List<SectionDefinition> Sections { get; } = new List<SectionDefinition>();

        // =============================
        // Paper
        // =============================
        public double PaperWidthMm { get; set; } = 210;
        public double PaperHeightMm { get; set; } = 297;

        public double LeftMarginMm { get; set; } = 0;
        public double RightMarginMm { get; set; } = 0;
        public double TopMarginMm { get; set; } = 0;
        public double BottomMarginMm { get; set; } = 0;

        // =============================
        // Printable Area
        // =============================
        public double PrintableWidthMm
        {
            get { return PaperWidthMm - LeftMarginMm - RightMarginMm; }
        }

        public double PrintableHeightMm
        {
            get { return PaperHeightMm - TopMarginMm - BottomMarginMm; }
        }

        // =============================
        // Control Operations
        // =============================
        public void AddControl(SectionDefinition section, DesignControl ctrl)
        {
            section.Controls.Add(ctrl);
        }

        public void RemoveControl(SectionDefinition section, DesignControl ctrl)
        {
            section.Controls.Remove(ctrl);
        }

        public void MoveControl(DesignControl ctrl, double leftMm, double topMm)
        {
            ctrl.LeftMm = leftMm;
            ctrl.TopMm = topMm;
        }

        public SectionDefinition? FindOwnerSection(DesignControl ctrl)
        {
            return Sections.FirstOrDefault(s => s.Controls.Contains(ctrl));
        }

        public double GetSectionStartMm(SectionDefinition target)
        {
            double y = 0;

            foreach (var sec in Sections)
            {
                if (sec == target)
                    return y;

                y += sec.HeightMm;
            }

            return 0;
        }
    }
}

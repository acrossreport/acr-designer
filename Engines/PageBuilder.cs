using AcrossReportDesigner.Models;
using System.Collections.Generic;

namespace AcrossReportDesigner.Engines
{
    public static class PageBuilder
    {
        public static List<List<SectionNode>> BuildPages(
            List<SectionNode> sections,
            double pageWidthTwips,
            double pageHeightTwips,
            double topMarginTwips,
            double bottomMarginTwips,
            double leftMarginTwips,
            double rightMarginTwips)
        {
            var pages = new List<List<SectionNode>>();
            List<SectionNode> currentPage = new List<SectionNode>();
            double yOffset = topMarginTwips;
            double usableBottom = pageHeightTwips - bottomMarginTwips;

            foreach (var section in sections)
            {
                if (yOffset + section.Height > usableBottom)
                {
                    pages.Add(currentPage);
                    currentPage = new List<SectionNode>();
                    yOffset = topMarginTwips;
                }

                currentPage.Add(section);
                yOffset += section.Height;

                // Handle page breaks
                if (section.PageBreak == PageBreakMode.After)
                {
                    pages.Add(currentPage);
                    currentPage = new List<SectionNode>();
                    yOffset = topMarginTwips;
                }
            }

            if (currentPage.Count > 0)
                pages.Add(currentPage);

            return pages;
        }
    }
}

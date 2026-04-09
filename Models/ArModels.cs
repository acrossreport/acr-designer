using System.Collections.Generic;

namespace AcrossReportDesigner.Models;

// =========================
// 帳票ルート
// =========================
public sealed class ArSections
{
    public List<ArSection> Section { get; set; } = new();
}

// =========================
// セクション
// =========================
public sealed class ArSection
{
    public string Name { get; set; } = "";

    public double Left { get; set; }
    public double Top { get; set; }

    public double Width { get; set; }
    public double Height { get; set; }

    // コントロール群（旧 Controls ではない）
    public List<ArControl>? Control { get; set; }

    // ★ 必須：DeepClone
    public ArSection DeepClone()
    {
        return new ArSection
        {
            Name = this.Name,
            Left = this.Left,
            Top = this.Top,
            Width = this.Width,
            Height = this.Height,
            Control = this.Control?
                .ConvertAll(c => c.DeepClone())
        };
    }
}

// =========================
// コントロール種別
// =========================
public enum ArControlType
{
    Text,
    Shape,
    Image
}

// =========================
// コントロール
// =========================
public sealed class ArControl
{
    public ArControlType Type { get; set; } = ArControlType.Text;

    // データ連携キー
    public string Name { get; set; } = "";

    // 表示文字列
    public string? Text { get; set; }

    // 位置・サイズ（mm）
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    // 描画属性
    public int ForeColor { get; set; } = unchecked((int)0xFF000000);
    public int BackColor { get; set; } = unchecked((int)0x00000000);
    public int LineColor { get; set; } = unchecked((int)0xFF000000);

    public double FontSize { get; set; } = 10;
    public double Radius { get; set; } = 0;

    // Z順（Designer / Viewer 共通）
    public int VirtualZ { get; set; }

    // ★ 必須：DeepClone
    public ArControl DeepClone()
    {
        return new ArControl
        {
            Type = this.Type,
            Name = this.Name,
            Text = this.Text,
            Left = this.Left,
            Top = this.Top,
            Width = this.Width,
            Height = this.Height,
            ForeColor = this.ForeColor,
            BackColor = this.BackColor,
            LineColor = this.LineColor,
            FontSize = this.FontSize,
            Radius = this.Radius,
            VirtualZ = this.VirtualZ
        };
    }
}

using AcrossReportDesigner.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace AcrossReportDesigner.Views;

public sealed class TableControlView : Border
{
    private const double Dpi = 96.0;
    public TableDefinition Model { get; }
    private readonly Grid _host = new(); // Thumb を重ねるための親
    private readonly Grid _grid = new(); // セル本体
    public event Action<int, int>? CellSelectionChanged;
    private Border? _lastSelected;

    private readonly List<Canvas> _cellCanvases = new();

    public TableControlView(TableDefinition model)
    {
        Model = model;

        BorderBrush = Brushes.DimGray;
        BorderThickness = new Thickness(1.5);
        Background = Brushes.Transparent;

        _grid.Background = Brushes.White;

        _host.Children.Add(_grid);
        Child = _host;

        BuildGrid();

        // テーブル背景クリック＝テーブル選択（row=-1,col=-1）
        PointerPressed += (_, e) =>
        {
            // セル側でHandledされていなければここに来る
            CellSelectionChanged?.Invoke(-1, -1);
        };
    }
    private static double MmToPx(double mm)
    {
        return mm * (Dpi / 25.4);
    }
    private static GridLength Px(double px)
    {
        return new GridLength(px, GridUnitType.Pixel);
    }
    // =========================
    // グリッド構築
    // =========================
    private void BuildGrid()
    {
        _cellCanvases.Clear();
        _grid.Children.Clear();
        _grid.ColumnDefinitions.Clear();
        _grid.RowDefinitions.Clear();
        // 列
        foreach (var col in Model.Columns)
        {
            var px = MmToPx(col.WidthMm);
            _grid.ColumnDefinitions.Add(new ColumnDefinition(Px(px)));
        }
        // 行
        foreach (var row in Model.Rows)
        {
            var px = MmToPx(row.HeightMm);
            _grid.RowDefinitions.Add(new RowDefinition(Px(px)));
        }
        // セル生成
        for (int r = 0; r < Model.Rows.Count; r++)
            for (int c = 0; c < Model.Columns.Count; c++)
            {
                var cellBorder = new Border
                {
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Background = Brushes.Transparent
                };
                var cellCanvas = new Canvas
                {
                    Background = Brushes.Transparent
                };
                cellBorder.Child = cellCanvas;
                _cellCanvases.Add(cellCanvas);
                cellBorder.Child = cellCanvas;
                int rr = r;
                int cc = c;
                cellBorder.PointerPressed += (_, e) =>
                {
                    e.Handled = true;
                    HighlightCell(cellBorder);
                    CellSelectionChanged?.Invoke(rr, cc);
                };
                Grid.SetRow(cellBorder, r);
                Grid.SetColumn(cellBorder, c);
                _grid.Children.Add(cellBorder);
            }
        AddColumnResizeThumbs();
        AddRowResizeThumbs();
    }
    public Canvas? GetCellCanvas(int row, int col)
    {
        int index = row * Model.Columns.Count + col;
        if (index < 0 || index >= _cellCanvases.Count) return null;
        return _cellCanvases[index];
    }
    // =========================
    // セル選択ハイライト
    // =========================
    private void HighlightCell(Border b)
    {
        if (_lastSelected != null)
        {
            _lastSelected.BorderBrush = Brushes.Gray;
            _lastSelected.BorderThickness = new Thickness(1);
        }
        _lastSelected = b;
        b.BorderBrush = Brushes.DodgerBlue;
        b.BorderThickness = new Thickness(2);
    }
    // =========================
    // 列リサイズ（AR風）
    // =========================
    private void AddColumnResizeThumbs()
    {
        // 既存Thumbを削除（再構築時）
        for (int i = _host.Children.Count - 1; i >= 0; i--)
        {
            if (_host.Children[i] is Thumb)
                _host.Children.RemoveAt(i);
        }
        double x = 0;
        for (int i = 0; i < Model.Columns.Count - 1; i++)
        {
            x += _grid.ColumnDefinitions[i].Width.Value;
            var thumb = new Thumb
            {
                Width = 6,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch,
                Cursor = new Cursor(StandardCursorType.SizeWestEast),
                Background = Brushes.Transparent,
                Margin = new Thickness(x - 3, 0, 0, 0)
            };
            int colIndex = i;
            thumb.DragDelta += (_, e) =>
            {
                var delta = e.Vector.X;
                var left = _grid.ColumnDefinitions[colIndex].Width.Value;
                var right = _grid.ColumnDefinitions[colIndex + 1].Width.Value;
                const double minPx = 20;
                var newLeft = Math.Max(minPx, left + delta);
                var newRight = Math.Max(minPx, right - delta);
                if (newRight <= minPx) return;
                _grid.ColumnDefinitions[colIndex].Width = Px(newLeft);
                _grid.ColumnDefinitions[colIndex + 1].Width = Px(newRight);
                // mmへ反映（保存用）
                Model.Columns[colIndex].WidthMm = newLeft / (Dpi / 25.4);
                Model.Columns[colIndex + 1].WidthMm = newRight / (Dpi / 25.4);
                // Thumb位置再配置
                RepositionThumbs();
            };

            _host.Children.Add(thumb);
        }
        RepositionThumbs();
    }
    private void RepositionThumbs()
    {
        double x = 0;
        int thumbIndex = 0;

        for (int col = 0; col < Model.Columns.Count - 1; col++)
        {
            x += _grid.ColumnDefinitions[col].Width.Value;

            // hostの中から Thumb を順番に数える（grid以外はThumbだけの想定）
            // host.Children[0] は _grid
            while (thumbIndex + 1 < _host.Children.Count && _host.Children[thumbIndex + 1] is not Thumb)
                thumbIndex++;

            int actualIndex = 1 + col; // gridの次から並ぶ想定
            if (actualIndex >= _host.Children.Count) break;

            if (_host.Children[actualIndex] is Thumb t)
            {
                t.Margin = new Thickness(x - 3, 0, 0, 0);
            }
        }
    }
    private void AddRowResizeThumbs()
    {
        double y = 0;

        for (int i = 0; i < Model.Rows.Count - 1; i++)
        {
            y += _grid.RowDefinitions[i].Height.Value;

            var thumb = new Thumb
            {
                Height = 6,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Cursor = new Cursor(StandardCursorType.SizeNorthSouth),
                Background = Brushes.Transparent,
                Margin = new Thickness(0, y - 3, 0, 0)
            };

            int rowIndex = i;

            thumb.DragDelta += (_, e) =>
            {
                var delta = e.Vector.Y;
                var top = _grid.RowDefinitions[rowIndex].Height.Value;
                var bottom = _grid.RowDefinitions[rowIndex + 1].Height.Value;
                const double min = 16;
                var newTop = Math.Max(min, top + delta);
                var newBottom = Math.Max(min, bottom - delta);
                if (newBottom <= min) return;
                _grid.RowDefinitions[rowIndex].Height = Px(newTop);
                _grid.RowDefinitions[rowIndex + 1].Height = Px(newBottom);
                Model.Rows[rowIndex].HeightMm =
                    newTop / (Dpi / 25.4);
                Model.Rows[rowIndex + 1].HeightMm =
                    newBottom / (Dpi / 25.4);
            };
            _host.Children.Add(thumb);
        }
    }
}

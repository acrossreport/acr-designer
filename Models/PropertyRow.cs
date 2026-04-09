using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace AcrossReportDesigner.Models;

public class PropertyRow : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // 表示名
    public string Property { get; set; } = "";

    // 編集可否
    public bool IsEditable { get; set; }

    // Editor種別
    private string _editor = "textbox";
    public string Editor
    {
        get => _editor;
        set
        {
            if (_editor == value) return;
            _editor = value;
            Raise(nameof(Editor));
            Raise(nameof(IsCombo));
            Raise(nameof(IsCheck));
            Raise(nameof(IsNumeric));
            Raise(nameof(IsText));
        }
    }

    public bool IsCombo => Editor == "combo";
    public bool IsCheck => Editor == "checkbox";
    public bool IsNumeric => Editor == "numeric";
    public bool IsText => Editor == "textbox";

    // ComboItems
    public IEnumerable<string>? ComboItems { get; set; }

    // ★ 追加：変更適用（ここが本体）
    // newValue は Value の文字列（checkbox/numericでも最終的には文字列にする）
    public Action<string>? Apply { get; set; }

    // ★ 追加：Undo用に「変更前」を取るためのフック
    public Action<string, string>? ApplyWithOldNew { get; set; }

    // Value
    private string _value = "";
    public string Value
    {
        get => _value;
        set
        {
            var next = value ?? "";
            if (_value == next) return;
            var old = _value;
            _value = next;
            Raise(nameof(Value));
            Raise(nameof(BoolValue));
            Raise(nameof(NumericValue));
            // ★ 空文字のときは Apply を呼ばない
            if (string.IsNullOrWhiteSpace(_value)) return;

            try  // ★ 追加
            {
                if (ApplyWithOldNew != null)
                    ApplyWithOldNew(old, _value);
                else
                    Apply?.Invoke(_value);
            }
            catch (Exception ex)  // ★ 追加
            {
                System.Diagnostics.Debug.WriteLine($"★ PropertyRow例外: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"★ old={old} new={_value}");
            }
        }
    }    // CheckBox用
    public bool BoolValue
    {
        get => string.Equals(Value, "true", StringComparison.OrdinalIgnoreCase);
        set
        {
            Value = value ? "true" : "false";
            Raise(nameof(BoolValue));
        }
    }
    // Numeric用
    public double NumericValue
    {
        get
        {
            // ★ 空文字のときは0を返す（例外を起こさない）
            if (string.IsNullOrWhiteSpace(Value)) return 0;
            if (double.TryParse(
                Value,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var d))
                return d;
            return 0;
        }
        set
        {
            Value = value.ToString(CultureInfo.InvariantCulture);
            Raise(nameof(NumericValue));
        }
    }
    public PropertyRow(
        string prop,
        string value,
        bool editable,
        string editor = "textbox",
        IEnumerable<string>? comboItems = null)
    {
        Property = prop;
        _value = value ?? "";
        IsEditable = editable;
        Editor = editor;
        ComboItems = comboItems;
    }

    private void Raise(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

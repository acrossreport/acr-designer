using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AcrossReportDesigner.Models;

public sealed class OutlineNode : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public object? Target { get; set; }

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            OnPropertyChanged();
        }
    }

    // ★ List → ObservableCollection に変更
    public ObservableCollection<OutlineNode> Children { get; set; } = new();

    public string Icon => Type switch
    {
        "Page" => "📄",
        "Section" => "📂",
        "Label" => "🏷️",
        "TextBox" => "🔤",
        "Line" => "📏",
        "Shape" => "⬛",
        _ => "🔹"
    };

    public string DisplayName
    {
        get
        {
            if (Target is DesignControl ctrl)
            {
                if (!string.IsNullOrWhiteSpace(ctrl.Text))
                    return $"{Name}  [{ctrl.Text}]";
                return Name;
            }
            return Name;
        }
    }
}
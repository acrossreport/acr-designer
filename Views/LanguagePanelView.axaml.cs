using AcrossReportDesigner.ViewModels;
using Avalonia.Controls;
using System;

namespace AcrossReportDesigner.Views;

public partial class LanguagePanelView : UserControl
{
    // ✅ MainWindow が購読して再起動フローを実行する
    public event Action<string>? RestartRequested;

    public LanguagePanelView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is LanguagePanelViewModel vm)
            {
                vm.RestartRequested += code => RestartRequested?.Invoke(code);
            }
        };
    }
}

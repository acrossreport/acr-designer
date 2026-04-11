using AcrossReportDesigner;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Threading.Tasks;

namespace AcrossReportDesigner.Views.Dialogs;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Topmost = true;
        KeyDown += OnKeyDown;
        Opened += (_, __) => Focus();

        // ✅ ボタン文字列を LocalizationManager から取得（生成時点の言語を使用）
        var loc = LocalizationManager.Instance;
        YesButton.Content = loc["Dialog_Yes"];
        NoButton.Content  = loc["Dialog_No"];
    }

    public void SetMessage(string message)
    {
        MessageBlock.Text = message;
    }

    private void Yes_Click(object? sender, RoutedEventArgs e) => Close(true);
    private void No_Click(object? sender, RoutedEventArgs e)  => Close(false);

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)  { Close(true);  e.Handled = true; }
        if (e.Key == Key.Escape) { Close(false); e.Handled = true; }
    }

    public static async Task<bool> Show(Window owner, string message)
    {
        var dlg = new ConfirmDialog();
        dlg.SetMessage(message);
        dlg.ShowInTaskbar = false;
        return await dlg.ShowDialog<bool>(owner);
    }
}

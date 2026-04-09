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

        SizeToContent = SizeToContent.Manual;
        Width = 200;
        Height = 100;

        // ✅ 追加
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Topmost = true;

        KeyDown += OnKeyDown;
        Opened += (_, __) => Focus();   // ✅ フォーカス確保
    }

    public void SetMessage(string message)
    {
        MessageBlock.Text = message;
    }

    private void Yes_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void No_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Close(true);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            Close(false);
            e.Handled = true;
        }
    }
    public static async Task<bool> Show(Window owner, string message)
    {
        var dlg = new ConfirmDialog();
        dlg.SetMessage(message);

        // ✅ 追加：Owner を明示
        dlg.ShowInTaskbar = false;

        return await dlg.ShowDialog<bool>(owner);
    }
}

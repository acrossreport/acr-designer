using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AcrossReportDesigner.Views.Dialogs;

public partial class MessageDialog : Window
{
    public bool Result { get; private set; }

    public MessageDialog()
    {
        InitializeComponent();
        KeyDown += MessageDialog_KeyDown;
    }

    public void SetMessage(string message)
    {
        MessageBlock.Text = message;
    }

    private void Yes_Click(object? sender, RoutedEventArgs e)
    {
        Result = true;
        Close();
    }

    private void No_Click(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }

    private void MessageDialog_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Result = true;
            Close();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Result = false;
            Close();
            e.Handled = true;
        }
    }
}

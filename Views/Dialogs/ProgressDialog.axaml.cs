using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace AcrossReportDesigner.Views.Dialogs;

public partial class ProgressDialog : Window
{
    private ProgressBar? _progress;
    private TextBlock? _percentText;
    private TextBlock? _countText;
    private TextBlock? _messageText;

    public ProgressDialog()
    {
        InitializeComponent();

        _progress = this.FindControl<ProgressBar>("MainProgress");
        _percentText = this.FindControl<TextBlock>("PercentText");
        _countText = this.FindControl<TextBlock>("CountText");
        _messageText = this.FindControl<TextBlock>("MessageText");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void SetProgress(double percent)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_progress != null)
                _progress.Value = percent;

            if (_percentText != null)
                _percentText.Text = $"{percent:0}%";
        });
    }

    public void SetCount(int count)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_countText != null)
                _countText.Text = $"データ件数：{count} 件";
        });
    }

    public void SetMessage(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_messageText != null)
                _messageText.Text = text;
        });
    }
}

using AcrossReportDesigner.Services;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AcrossReportDesigner.Views;

public partial class MainWindow : Window
{
    // =========================
    // Åö Ç±Ç±Çí«â¡Ç∑ÇÈ
    // =========================
    private readonly DesignerView _designerView;
    private readonly DbConnectorView _dbConnectorView;
    private readonly ViewerView _viewerView;
    public MainWindow()
    {
        InitializeComponent();
        // Åö Ç±Ç±Ç≈ê∂ê¨Ç∑ÇÈ
        _designerView = new DesignerView();
        _dbConnectorView = new DbConnectorView();
        _viewerView = new ViewerView();
        MainContent.Content = _designerView;
        Icon = new WindowIcon("Assets/app.png");
        this.WindowState = WindowState.Maximized;
    }
    private void Designer_Click(object? sender, RoutedEventArgs e)
    {
        MainContent.Content = _designerView;
    }
    private void DataConnector_Click(object? sender, RoutedEventArgs e)
    {
        MainContent.Content = _dbConnectorView;
    }
    private void DataViewer_Click(object? sender, RoutedEventArgs e)
    {
        MainContent.Content = _viewerView;
    }
    private bool _exitDialogOpen = false;
    private async void Exit_Click(object? sender, RoutedEventArgs e)
    {
        if (_exitDialogOpen) return;
        _exitDialogOpen = true;
        try
        {
            bool result = await DialogService.ShowConfirmAsync(this, "èIóπÇµÇ‹Ç∑Ç©ÅH", "");
            if (result) Close();
        }
        finally
        {
            _exitDialogOpen = false;
        }
    }
}

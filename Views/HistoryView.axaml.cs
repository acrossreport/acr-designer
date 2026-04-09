using Avalonia.Controls;
using Avalonia.Interactivity;
using AcrossReportDesigner.Models;
using System.Collections.Generic;

namespace AcrossReportDesigner.Views;

public partial class HistoryView : Window
{
    public SqlHistoryItem? SelectedItem { get; private set; }

    public HistoryView(List<SqlHistoryItem> items)
    {
        InitializeComponent();

        HistoryList.ItemsSource = items;
    }
     
    private void Select_Click(object? sender, RoutedEventArgs e)
    {
        SelectedItem = HistoryList.SelectedItem as SqlHistoryItem;
        Close();
    }
}

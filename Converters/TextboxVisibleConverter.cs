using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AcrossReportDesigner.Converters;

public class TextboxVisibleConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        var editor = value?.ToString()?.ToLowerInvariant();

        // textbox のときだけ表示
        return editor == "textbox";
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
        => throw new NotSupportedException();
}

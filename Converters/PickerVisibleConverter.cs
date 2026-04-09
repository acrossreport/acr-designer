using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AcrossReportDesigner.Converters;

public class PickerVisibleConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        var editor = value?.ToString()?.ToLowerInvariant();

        return editor == "fontpicker"
            || editor == "colorpicker";
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
        => throw new NotSupportedException();
}

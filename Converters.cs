using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ShinySuite;

/// <summary>Converts a hex color string like "#E6B800" to a SolidColorBrush.</summary>
[ValueConversion(typeof(string), typeof(SolidColorBrush))]
public class StringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            var hex = value?.ToString();
            if (string.IsNullOrEmpty(hex)) return Brushes.Gray;
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }
        catch { return Brushes.Gray; }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

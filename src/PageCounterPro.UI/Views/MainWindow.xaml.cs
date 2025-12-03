using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PageCounterPro.UI.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}

/// <summary>
/// Converter that compares a string value to a parameter and returns true if they match.
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string stringValue && parameter is string paramValue)
        {
            return stringValue.Equals(paramValue, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue && parameter is string paramValue)
        {
            return paramValue;
        }
        return DependencyProperty.UnsetValue;
    }
}

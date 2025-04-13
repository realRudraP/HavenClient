using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace HavenClient.Converters // Ensure this namespace is correct
{
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public Visibility TrueValue { get; set; } = Visibility.Visible;
        public Visibility FalseValue { get; set; } = Visibility.Collapsed;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!(value is bool))
                return FalseValue; // Default or fallback value
            return (bool)value ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // Optionally implement ConvertBack if needed
            if (value is Visibility visibility)
            {
                return visibility == TrueValue;
            }
            return DependencyProperty.UnsetValue; // Indicate failure
            // throw new NotImplementedException();
        }
    }
}
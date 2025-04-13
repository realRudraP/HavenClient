using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace HavenClient.Converters // Ensure this namespace is correct
{
    public sealed class BoolToAlignmentConverter : IValueConverter
    {
        public HorizontalAlignment TrueValue { get; set; } = HorizontalAlignment.Right;
        public HorizontalAlignment FalseValue { get; set; } = HorizontalAlignment.Left;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!(value is bool))
                return FalseValue; // Default or fallback value
            return (bool)value ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
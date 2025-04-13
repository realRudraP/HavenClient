using Microsoft.UI; // For Colors
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace HavenClient.Converters 
{
    public sealed class BoolToBrushConverter : IValueConverter
    {
        
        public Brush TrueValue { get; set; } = new SolidColorBrush(Colors.LightBlue);
        public Brush FalseValue { get; set; } = new SolidColorBrush(Colors.DarkGray);   

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
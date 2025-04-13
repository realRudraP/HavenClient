using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization; // Needed for CultureInfo if you want specific culture formatting

namespace HavenClient.Converters
{
    public class DateTimeFormatConverter : IValueConverter
    {
         public string Format { get; set; } = "g"; // Default format (general short date/time)

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime dateTime)
            {
                 DateTime localTime = dateTime.ToLocalTime();

                 string formatToUse = this.Format;

                 if (parameter is string paramFormat && !string.IsNullOrEmpty(paramFormat))
                {
                    formatToUse = paramFormat;
                }

                try
                {
                     return localTime.ToString(formatToUse);
                }
                catch (FormatException)
                {
                     return localTime.ToString(); // Fallback to default ToString()
                }
            }

             return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
             throw new NotImplementedException();
        }
    }
}
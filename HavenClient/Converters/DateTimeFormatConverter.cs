using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization; // Needed for CultureInfo if you want specific culture formatting

namespace HavenClient.Converters
{
    public class DateTimeFormatConverter : IValueConverter
    {
        // Property to control the format string via XAML if needed (optional)
        public string Format { get; set; } = "g"; // Default format (general short date/time)

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime dateTime)
            {
                // *** CONVERT TO LOCAL TIME HERE ***
                DateTime localTime = dateTime.ToLocalTime();

                // Use the Format property or a default
                string formatToUse = this.Format;

                // Optional: Allow overriding format via converter parameter in XAML
                if (parameter is string paramFormat && !string.IsNullOrEmpty(paramFormat))
                {
                    formatToUse = paramFormat;
                }

                try
                {
                    // Format the *local* time
                    return localTime.ToString(formatToUse);
                }
                catch (FormatException)
                {
                    // Handle invalid format string gracefully
                    return localTime.ToString(); // Fallback to default ToString()
                }
            }

            // Return empty string or original value if not a DateTime
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // Not typically needed for display formatting
            throw new NotImplementedException();
        }
    }
}
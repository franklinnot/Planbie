using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Presentation
{
    public class TimeAxisLabelConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || !(values[0] is double) || !(values[1] is DateTime))
                return string.Empty;

            double value = (double)values[0];
            DateTime now = (DateTime)values[1];

            TimeSpan timeDiff = TimeSpan.FromHours(2 * value / 10); // Asumiendo que hay 10 puntos de datos en 2 horas
            DateTime pointInTime = now.Subtract(timeDiff);

            if (value == 0)
                return "Ahora";
            else if (value == 10)
                return "-2h";
            else
                return $"-{timeDiff.TotalHours:F1}h";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

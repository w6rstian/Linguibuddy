using Linguibuddy.Resources.Strings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.Helpers
{
    public class ResourceKeyToStringConverter : IValueConverter, IMultiValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string key && !string.IsNullOrEmpty(key))
            {
                return AppResources.ResourceManager.GetString(key, culture) ?? key;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length > 0 && values[0] is string key && !string.IsNullOrEmpty(key))
            {
                var targetCulture = culture;
                if (values.Length > 1 && values[1] is CultureInfo ci)
                {
                    targetCulture = ci;
                }

                return AppResources.ResourceManager.GetString(key, targetCulture) ?? key;
            }
            return values.FirstOrDefault();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using Linguibuddy.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.Helpers
{
    public class AchievementIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UserAchievement ua && ua.Achievement != null)
            {
                return ua.IsUnlocked ? ua.Achievement.IconUrl : ua.Achievement.LockedIconUrl;
            }
            return null; // Domyślna ikona lub pusty
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

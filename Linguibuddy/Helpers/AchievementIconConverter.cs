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
                var unlockedUrl = ua.Achievement.IconUrl;
                var lockedUrl = ua.Achievement.LockedIconUrl;
                if (Application.Current.RequestedTheme == AppTheme.Dark)
                {
                    unlockedUrl = unlockedUrl.Replace("light", "dark");
                    lockedUrl = lockedUrl.Replace("light", "dark");
                }

                return ua.IsUnlocked ? unlockedUrl : lockedUrl;
            }
            return null; // Domyślna ikona lub pusty
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

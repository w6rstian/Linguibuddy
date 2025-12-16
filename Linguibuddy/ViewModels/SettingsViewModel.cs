using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LocalizationResourceManager.Maui;
using System.Globalization;

namespace Linguibuddy.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ILocalizationResourceManager _resourceManager;
        private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");
        private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl");

        [ObservableProperty]
        private string _themeName;
        public SettingsViewModel(ILocalizationResourceManager resourceManager)
        {
            _resourceManager = resourceManager;

            UpdateThemeName();
        }

        [RelayCommand]
        public void ChangeLanguage()
        {
            var currentCulture = _resourceManager.CurrentCulture;

            if (currentCulture.Name == English.Name)
            {
                _resourceManager.CurrentCulture = Polish;
            }
            else
            {
                _resourceManager.CurrentCulture = English;
            }

            UpdateThemeName();

            return;
        }

        [RelayCommand]
        public void ChangeTheme()
        {
            if (Application.Current.RequestedTheme == AppTheme.Light)
            {
                Application.Current.UserAppTheme = AppTheme.Dark;
            }
            else
            {
                Application.Current.UserAppTheme = AppTheme.Light;
            }

            UpdateThemeName();
        }

        private void UpdateThemeName()
        {
            ThemeName = Application.Current.RequestedTheme == AppTheme.Light
                ? _resourceManager["ThemeNameLight"]
                : _resourceManager["ThemeNameDark"];
        }
    }
}

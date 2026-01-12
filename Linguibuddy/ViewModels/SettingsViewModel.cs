using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Helpers;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
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

        [ObservableProperty]
        private string _translationApiName;

        public IReadOnlyList<DifficultyLevel> AvailableDifficulties { get; }
            = Enum.GetValues(typeof(DifficultyLevel)).Cast<DifficultyLevel>().ToList();

        [ObservableProperty]
        private DifficultyLevel _selectedDifficulty;

        public SettingsViewModel(ILocalizationResourceManager resourceManager)
        {
            _resourceManager = resourceManager;

            UpdateThemeName();
            UpdateApiName();
            LoadDifficulty();
        }

        partial void OnSelectedDifficultyChanged(DifficultyLevel value)
        {
            Preferences.Default.Set(Constants.DifficultyLevelKey, (int)value);
        }

        private void LoadDifficulty()
        {
            int savedLevel = Preferences.Default.Get(Constants.DifficultyLevelKey, (int)DifficultyLevel.A1);

            if (Enum.IsDefined(typeof(DifficultyLevel), savedLevel))
            {
                SelectedDifficulty = (DifficultyLevel)savedLevel;
            }
            else
            {
                SelectedDifficulty = DifficultyLevel.A1;
            }
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

        [RelayCommand]
        public void ChangeTranslationApi()
        {
            int currentApiInt = Preferences.Default.Get(Constants.TranslationApiKey, (int)TranslationProvider.DeepL);
            var currentProvider = (TranslationProvider)currentApiInt;

            var newProvider = currentProvider == TranslationProvider.DeepL
                ? TranslationProvider.OpenAi
                : TranslationProvider.DeepL;

            Preferences.Default.Set(Constants.TranslationApiKey, (int)newProvider);

            UpdateApiName();
        }

        private void UpdateApiName()
        {
            int currentApiInt = Preferences.Default.Get(Constants.TranslationApiKey, (int)TranslationProvider.DeepL);
            var provider = (TranslationProvider)currentApiInt;

            TranslationApiName = provider == TranslationProvider.OpenAi
                ? "OpenAI (GPT)"
                : "DeepL";
        }

        private void UpdateThemeName()
        {
            ThemeName = Application.Current.RequestedTheme == AppTheme.Light
                ? AppResources.ThemeNameLight
                : AppResources.ThemeNameDark;
        }
    }
}

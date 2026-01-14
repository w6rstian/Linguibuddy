using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Helpers;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;
using LocalizationResourceManager.Maui;

namespace Linguibuddy.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl");
    private readonly AppUserService _appUserService;
    private readonly ILocalizationResourceManager _resourceManager;

    [ObservableProperty] private DifficultyLevel _selectedDifficulty;

    [ObservableProperty] private string _themeName;

    [ObservableProperty] private string _translationApiName;

    public SettingsViewModel(ILocalizationResourceManager resourceManager, AppUserService appUserService)
    {
        _resourceManager = resourceManager;
        _appUserService = appUserService;

        LoadLanguage();
        LoadTheme();
        UpdateThemeName();
        UpdateApiName();
        //LoadDifficulty();

        Task.Run(LoadDifficultyAsync);
    }

    public IReadOnlyList<DifficultyLevel> AvailableDifficulties { get; }
        = Enum.GetValues(typeof(DifficultyLevel)).Cast<DifficultyLevel>().ToList();

    partial void OnSelectedDifficultyChanged(DifficultyLevel value)
    {
        //Preferences.Default.Set(Constants.DifficultyLevelKey, (int)value);
        Task.Run(async () =>
        {
            try
            {
                await _appUserService.SetUserDifficultyAsync(value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd zapisu poziomu trudności: {ex.Message}");
            }
        });
    }

    private async Task LoadDifficultyAsync()
    {
        var level = await _appUserService.GetUserDifficultyAsync();
        // Musimy to zrobić na głównym wątku, bo aktualizujemy UI
        MainThread.BeginInvokeOnMainThread(() => { SelectedDifficulty = level; });
    }

    /*
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
    */

    private void LoadLanguage()
    {
        var savedLanguage = Preferences.Default.Get(Constants.LanguageKey, "pl");
        if (savedLanguage == "pl")
            _resourceManager.CurrentCulture = Polish;
        else
            _resourceManager.CurrentCulture = English;
    }

    private void LoadTheme()
    {
        var savedTheme = Preferences.Default.Get(Constants.AppThemeKey, (int)AppTheme.Light);
        if (Enum.IsDefined(typeof(AppTheme), savedTheme))
            Application.Current.UserAppTheme = (AppTheme)savedTheme;
        else
            Application.Current.UserAppTheme = AppTheme.Light;
    }

    [RelayCommand]
    public void ChangeLanguage()
    {
        var currentCulture = _resourceManager.CurrentCulture;

        if (currentCulture.Name == English.Name)
        {
            _resourceManager.CurrentCulture = Polish;
            Preferences.Default.Set(Constants.LanguageKey, "pl");
        }
        else
        {
            _resourceManager.CurrentCulture = English;
            Preferences.Default.Set(Constants.LanguageKey, "en");
        }

        UpdateThemeName();
    }

    [RelayCommand]
    public void ChangeTheme()
    {
        if (Application.Current.RequestedTheme == AppTheme.Light)
        {
            Application.Current.UserAppTheme = AppTheme.Dark;
            Preferences.Default.Set(Constants.AppThemeKey, (int)AppTheme.Dark);
        }
        else
        {
            Application.Current.UserAppTheme = AppTheme.Light;
            Preferences.Default.Set(Constants.AppThemeKey, (int)AppTheme.Light);
        }

        UpdateThemeName();
    }

    [RelayCommand]
    public void ChangeTranslationApi()
    {
        var currentApiInt = Preferences.Default.Get(Constants.TranslationApiKey, (int)TranslationProvider.DeepL);
        var currentProvider = (TranslationProvider)currentApiInt;

        var newProvider = currentProvider == TranslationProvider.DeepL
            ? TranslationProvider.OpenAi
            : TranslationProvider.DeepL;

        Preferences.Default.Set(Constants.TranslationApiKey, (int)newProvider);

        UpdateApiName();
    }

    private void UpdateApiName()
    {
        var currentApiInt = Preferences.Default.Get(Constants.TranslationApiKey, (int)TranslationProvider.DeepL);
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
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Views;
using LocalizationResourceManager.Maui;

namespace Linguibuddy.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl");
    private readonly IAppUserService _appUserService;
    private readonly FirebaseAuthClient _authClient;
    private readonly ICollectionService _collectionService;
    private readonly ILocalizationResourceManager _resourceManager;
    private readonly IServiceProvider _services;

    [ObservableProperty] private DifficultyLevel _selectedDifficulty;

    [ObservableProperty] private int _selectedLessonLength;

    [ObservableProperty] private string _themeName;

    [ObservableProperty] private string _translationApiName;

    public SettingsViewModel(
        ILocalizationResourceManager resourceManager,
        IAppUserService appUserService,
        FirebaseAuthClient authClient,
        IServiceProvider services,
        ICollectionService collectionService)
    {
        _resourceManager = resourceManager;
        _appUserService = appUserService;
        _authClient = authClient;
        _services = services;
        _collectionService = collectionService;

        LoadLanguage();
        LoadTheme();
        UpdateThemeName();
        UpdateApiName();
    }

    public IReadOnlyList<DifficultyLevel> AvailableDifficulties { get; }
        = Enum.GetValues(typeof(DifficultyLevel)).Cast<DifficultyLevel>().ToList();

    public IReadOnlyList<int> AvailableLessonLengths { get; }
        = new List<int>
        {
            10, 20, 50, 100
        };

    partial void OnSelectedDifficultyChanged(DifficultyLevel value)
    {
        RunInBackground(async () =>
        {
            try
            {
                await _appUserService.SetUserDifficultyAsync(value);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd zapisu poziomu trudności: {ex.Message}");
            }
        });
    }

    partial void OnSelectedLessonLengthChanged(int value)
    {
        RunInBackground(async () =>
        {
            try
            {
                await _appUserService.SetUserLessonLengthAsync(value);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd zapisu długości lekcji: {ex.Message}");
            }
        });
    }

    public async Task LoadDifficultyAsync()
    {
        var level = await _appUserService.GetUserDifficultyAsync();
        SelectedDifficulty = level;
    }

    public async Task LoadLessonLengthAsync()
    {
        var length = await _appUserService.GetUserLessonLengthAsync();
        SelectedLessonLength = length;
    }

    private void LoadLanguage()
    {
        var savedLanguage = GetPreference(Constants.LanguageKey, "pl");
        if (savedLanguage == "pl")
            _resourceManager.CurrentCulture = Polish;
        else
            _resourceManager.CurrentCulture = English;
    }

    private void LoadTheme()
    {
        var savedTheme = GetPreference(Constants.AppThemeKey, (int)AppTheme.Light);
        if (Enum.IsDefined(typeof(AppTheme), savedTheme))
            SetAppTheme((AppTheme)savedTheme);
        else
            SetAppTheme(AppTheme.Light);
    }

    [RelayCommand]
    public async Task ChangeLanguage()
    {
        var currentCulture = _resourceManager.CurrentCulture;

        if (currentCulture.Name == English.Name)
        {
            _resourceManager.CurrentCulture = Polish;
            SetPreference(Constants.LanguageKey, "pl");
        }
        else
        {
            _resourceManager.CurrentCulture = English;
            SetPreference(Constants.LanguageKey, "en");
        }

        UpdateThemeName();

        await _appUserService.MarkAiAnalysisRequiredAsync();
        var collections = await _collectionService.GetUserCollectionsAsync();
        foreach (var collection in collections)
        {
            collection.RequiresAiAnalysis = true;
            await _collectionService.UpdateCollectionAsync(collection);
        }
    }

    [RelayCommand]
    public void ChangeTheme()
    {
        if (GetAppTheme() == AppTheme.Light)
        {
            SetAppTheme(AppTheme.Dark);
            SetPreference(Constants.AppThemeKey, (int)AppTheme.Dark);
        }
        else
        {
            SetAppTheme(AppTheme.Light);
            SetPreference(Constants.AppThemeKey, (int)AppTheme.Light);
        }

        UpdateThemeName();
    }

    [RelayCommand]
    public void ChangeTranslationApi()
    {
        var currentApiInt = GetPreference(Constants.TranslationApiKey, (int)TranslationProvider.OpenAi);
        var currentProvider = (TranslationProvider)currentApiInt;

        var newProvider = currentProvider == TranslationProvider.DeepL
            ? TranslationProvider.OpenAi
            : TranslationProvider.DeepL;

        SetPreference(Constants.TranslationApiKey, (int)newProvider);

        UpdateApiName();
    }

    private void UpdateApiName()
    {
        var currentApiInt = GetPreference(Constants.TranslationApiKey, (int)TranslationProvider.OpenAi);
        var provider = (TranslationProvider)currentApiInt;

        TranslationApiName = provider == TranslationProvider.OpenAi
            ? "OpenAI (GPT)"
            : "DeepL";
    }

    private void UpdateThemeName()
    {
        ThemeName = GetAppTheme() == AppTheme.Light
            ? AppResources.ThemeNameLight
            : AppResources.ThemeNameDark;
    }

    [RelayCommand]
    private async Task ChangeDisplayName()
    {
        var result = await Shell.Current.DisplayPromptAsync(
            AppResources.ChangeUserName,
            $"{AppResources.ChangeUserNameDescription}:",
            AppResources.Save, AppResources.Cancel,
            initialValue: _authClient.User.Info.DisplayName);

        if (!string.IsNullOrWhiteSpace(result) && result != _authClient.User.Info.DisplayName)
        {
            await _authClient.User.ChangeDisplayNameAsync(result);
            var appUser = await _appUserService.GetCurrentUserAsync();
            appUser.UserName = _authClient.User.Info.DisplayName;
            await _appUserService.UpdateAppUserAsync(appUser);

            await Shell.Current.DisplayAlert(
                AppResources.Success,
                $"{AppResources.ChangeUserNameSuccess}!",
                AppResources.OK);
        }
    }


    [RelayCommand]
    private async Task SignOut()
    {
        _authClient.SignOut();
        NavigateToSignIn();
    }

    protected virtual void RunInBackground(Func<Task> action)
    {
        Task.Run(action);
    }

    protected virtual string GetPreference(string key, string defaultValue)
    {
        return Preferences.Default.Get(key, defaultValue);
    }

    protected virtual int GetPreference(string key, int defaultValue)
    {
        return Preferences.Default.Get(key, defaultValue);
    }

    protected virtual void SetPreference(string key, string value)
    {
        Preferences.Default.Set(key, value);
    }

    protected virtual void SetPreference(string key, int value)
    {
        Preferences.Default.Set(key, value);
    }

    protected virtual AppTheme GetAppTheme()
    {
        return Application.Current.RequestedTheme;
    }

    protected virtual void SetAppTheme(AppTheme theme)
    {
        Application.Current.UserAppTheme = theme;
    }

    protected virtual void NavigateToSignIn()
    {
        var signInPage = _services.GetRequiredService<SignInPage>();
        if (Application.Current?.Windows.Count > 0)
            Application.Current.Windows[0].Page = new NavigationPage(signInPage);
    }
}
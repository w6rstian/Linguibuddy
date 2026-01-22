using CommunityToolkit.Mvvm.ComponentModel;
using Firebase.Auth;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Views;

namespace Linguibuddy.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAchievementRepository _achievementRepository;
    private readonly IAppUserService _appUserService;
    private readonly FirebaseAuthClient _authClient;
    private readonly ICollectionService _collectionService;
    private readonly ILearningService _learningService;
    private readonly IOpenAiService _openAiService;
    private readonly IServiceProvider _services;
    private readonly SettingsViewModel _settingsViewModel;

    [ObservableProperty] private string _aiFeedback;

    [ObservableProperty] private int _bestStreak;

    [ObservableProperty] private int _currentStreak;

    [ObservableProperty] private string _displayName;

    [ObservableProperty] private string _email;

    [ObservableProperty] private bool _isAiThinking;

    [ObservableProperty] private bool _isCurrentStreakBest;

    [ObservableProperty] private int _points;

    [ObservableProperty] private int _unlockedAchievementsCount;

    private AppUser _user;

    public MainViewModel(
        IServiceProvider services,
        IAppUserService appUserService,
        ILearningService learningService,
        FirebaseAuthClient authClient,
        IAchievementRepository achievementRepository,
        ICollectionService collectionService,
        IOpenAiService openAiService,
        SettingsViewModel settingsViewModel)
    {
        _appUserService = appUserService;
        _learningService = learningService;
        _authClient = authClient;
        _achievementRepository = achievementRepository;
        _collectionService = collectionService;
        _openAiService = openAiService;
        _services = services;
        _settingsViewModel = settingsViewModel;
        _isAiThinking = true;
    }

    public async Task LoadProfileInfoAsync()
    {
        if (IsUserAuthenticated())
        {
            DisplayName = GetUserDisplayName();
        }
        else
        {
            NavigateToSignIn();
            return;
        }

        Email = GetUserEmail();
        Points = await _appUserService.GetUserPointsAsync();
        CurrentStreak = await _learningService.GetCurrentStreakAsync();
        BestStreak = await _appUserService.GetUserBestStreakAsync();
        _user = await _appUserService.GetCurrentUserAsync();

        if (CurrentStreak == BestStreak)
            IsCurrentStreakBest = true;

        UnlockedAchievementsCount = await _achievementRepository.GetUnlockedAchievementsCountAsync();

        await GetAiFeedback();
    }

    public async Task GetAiFeedback()
    {
        if (!_user.RequiresAiAnalysis && !string.IsNullOrEmpty(_user.LastAiAnalysis))
        {
            AiFeedback = _user.LastAiAnalysis;
            IsAiThinking = false;
            return;
        }

        AiFeedback = AppResources.AiAnalysisThinking;

        try
        {
            var collections = await _collectionService.GetUserCollectionsAsync();

            var language = GetPreference(Constants.LanguageKey, "pl");
            var feedback = await _openAiService.AnalyzeComprehensiveProfileAsync(_user, CurrentStreak,
                UnlockedAchievementsCount, collections, language);

            AiFeedback = feedback;

            _user.LastAiAnalysis = feedback;
            _user.RequiresAiAnalysis = false;
            await _appUserService.UpdateAppUserAsync(_user);
        }
        catch (Exception)
        {
            AiFeedback = AppResources.AiAnalysisError;
        }
        finally
        {
            IsAiThinking = false;
        }
    }

    protected virtual bool IsUserAuthenticated()
    {
        return _authClient.User != null;
    }

    protected virtual string GetUserDisplayName()
    {
        return _authClient.User?.Info?.DisplayName ?? string.Empty;
    }

    protected virtual string GetUserEmail()
    {
        return _authClient.User?.Info?.Email ?? string.Empty;
    }

    protected virtual void NavigateToSignIn()
    {
        var signInPage = _services.GetRequiredService<SignInPage>();
        if (Application.Current?.Windows.Count > 0)
            Application.Current.Windows[0].Page = new NavigationPage(signInPage);
    }

    protected virtual string GetPreference(string key, string defaultValue)
    {
        return Preferences.Default.Get(key, defaultValue);
    }
}
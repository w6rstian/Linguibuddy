using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Views;
using Linguibuddy.Helpers;
using LocalizationResourceManager.Maui;

namespace Linguibuddy.ViewModels;

//TODO: AI profile analysis like in CollectionDetailsViewModel
public partial class MainViewModel : ObservableObject
{
    private readonly IAchievementRepository _achievementRepository;
    private readonly IAppUserService _appUserService;
    private readonly FirebaseAuthClient _authClient;
    private readonly ILearningService _learningService;
    private readonly IServiceProvider _services;
    private readonly ICollectionService _collectionService;
    private readonly IOpenAiService _openAiService;

    private AppUser user;

    [ObservableProperty] private int _bestStreak;

    [ObservableProperty] private int _currentStreak;

    [ObservableProperty] private string _displayName;

    [ObservableProperty] private string _email;

    [ObservableProperty] private bool _isCurrentStreakBest;

    [ObservableProperty] private int _points;

    [ObservableProperty] private int _unlockedAchievementsCount;

    [ObservableProperty] private string _aiFeedback;

    [ObservableProperty] private bool _isAiThinking;

    public MainViewModel(
        IServiceProvider services,
        IAppUserService appUserService,
        ILearningService learningService,
        FirebaseAuthClient authClient, 
        IAchievementRepository achievementRepository,
        ICollectionService collectionService,
        IOpenAiService openAiService)
    {
        _appUserService = appUserService;
        _learningService = learningService;
        _authClient = authClient;
        _achievementRepository = achievementRepository;
        _collectionService = collectionService;
        _openAiService = openAiService;
        _services = services;
        _isAiThinking = true;
    }

    public async Task LoadProfileInfoAsync()
    {
        if (_authClient.User != null)
        {
            DisplayName = _authClient.User.Info.DisplayName;
        }
        else
        {
            var signInPage = _services.GetRequiredService<SignInPage>();
            Application.Current.Windows[0].Page = new NavigationPage(signInPage);
        }

        Email = _authClient.User.Info.Email;
        Points = await _appUserService.GetUserPointsAsync();
        CurrentStreak = await _learningService.GetCurrentStreakAsync();
        BestStreak = await _appUserService.GetUserBestStreakAsync();
        user = await _appUserService.GetCurrentUserAsync();

        if (CurrentStreak == BestStreak)
            IsCurrentStreakBest = true;

        UnlockedAchievementsCount = await _achievementRepository.GetUnlockedAchievementsCountAsync();

        await GetAiFeedback();
    }

    public async Task GetAiFeedback()
    {
        if (!user.RequiresAiAnalysis && !string.IsNullOrEmpty(user.LastAiAnalysis))
        {
            AiFeedback = user.LastAiAnalysis;
            IsAiThinking = false;
            return;
        }
        AiFeedback = "Trener analizuje Twój profil i kolekcje...";

        var collections = await _collectionService.GetUserCollectionsAsync();

        var language = Preferences.Default.Get(Constants.LanguageKey, "pl");
        var feedback = await _openAiService.AnalyzeComprehensiveProfileAsync(user, CurrentStreak, UnlockedAchievementsCount, collections, language);

        AiFeedback = feedback;

        user.LastAiAnalysis = feedback;
        user.RequiresAiAnalysis = false;
        IsAiThinking = false;
        await _appUserService.UpdateAppUserAsync(user);
    }
}
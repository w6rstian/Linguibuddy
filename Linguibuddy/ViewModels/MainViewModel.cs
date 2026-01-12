using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Linguibuddy.Services;
using Linguibuddy.Views;

namespace Linguibuddy.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;
        private readonly IServiceProvider _services;
        private readonly SettingsViewModel _settingsViewModel;

        [ObservableProperty]
        private string _username;

        public MainViewModel(
            FirebaseAuthClient authClient,
            IServiceProvider services,
            SettingsViewModel settingsViewModel)
        {
            _authClient = authClient;
            _services = services;
            _settingsViewModel = settingsViewModel;

            if (_authClient.User != null)
                Username = _authClient.User.Info.DisplayName;
        }

        [RelayCommand]
        private async Task NavigateToDictionary()
        {
            await Shell.Current.GoToAsync("//DictionaryPage");
        }
        [RelayCommand]
        private async Task NavigateToFlashcards()
        {
            await Shell.Current.GoToAsync("//FlashcardsCollectionsPage");
        }
        [RelayCommand]
        private async Task NavigateToMinigames()
        {
            await Shell.Current.GoToAsync("//MiniGamesPage");
        }
        [RelayCommand]
        private async Task NavigateToAchievements()
        {
            await Shell.Current.GoToAsync("//AchievementsPage");
        }

        [RelayCommand]
        private async Task NavigateToSettings()
        {
            await Shell.Current.GoToAsync("//SettingsPage");
        }

        [RelayCommand]
        private async Task SignOut()
        {
            _authClient.SignOut();
            var signInPage = _services.GetRequiredService<SignInPage>();
            Application.Current.Windows[0].Page = new NavigationPage(signInPage);
        }
    }
}

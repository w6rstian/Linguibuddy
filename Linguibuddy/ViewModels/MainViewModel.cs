using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Linguibuddy.Data;
using Linguibuddy.Services;
using Linguibuddy.Views;

namespace Linguibuddy.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;
        private readonly IServiceProvider _services;
        private readonly MockDataSeeder _dataSeeder;

        [ObservableProperty]
        private string? apiResponseStatus;
        [ObservableProperty]
        private string _username;

        public MainViewModel(
            FirebaseAuthClient authClient, 
            IServiceProvider services, 
            MockDataSeeder dataSeeder)
        {
            _authClient = authClient;
            _services = services;
            _dataSeeder = dataSeeder;

            if (_authClient.User != null)
                Username = _authClient.User.Info.DisplayName;

            Task.Run(InitializeData);
        }

        private async Task InitializeData()
        {
            try
            {
                await _dataSeeder.SeedAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Seeding error: {ex.Message}");
            }
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
        private async Task NavigateToAwards()
        {
            await Shell.Current.GoToAsync("//AwardsPage");
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

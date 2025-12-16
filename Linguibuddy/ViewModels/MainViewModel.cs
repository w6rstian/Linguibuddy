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
        private readonly FlashcardService _flashcardService; // flashcard service
        private readonly OpenAiService _openAiService; // test AI
        private readonly DataContext _dataContext;
        private readonly FirebaseAuthClient _authClient;
        private readonly IServiceProvider _services;

        [ObservableProperty]
        private string? apiResponseStatus;
        [ObservableProperty]
        private string _username;

        public MainViewModel(DataContext dataContext, OpenAiService openAiService, FirebaseAuthClient authClient, IServiceProvider services, FlashcardService flashcardService)
        {
            _dataContext = dataContext;
            _openAiService = openAiService;
            _authClient = authClient;
            _services = services;
            _flashcardService = flashcardService;
            ApiResponseStatus = "Kliknij przycisk, aby przetestować API";

            Username = _authClient.User.Info.DisplayName;
            _flashcardService = flashcardService;

            //CreateSampleCollection().ConfigureAwait(false);
        }

        [RelayCommand]
        private async Task TestOpenAiAsync()
        {
            try
            {
                ApiResponseStatus = "Łączenie z OpenAI...";
                var result = await _openAiService.TestConnectionAsync();
                ApiResponseStatus = $"Odpowiedź API: {result}";
            }
            catch (Exception ex)
            {
                ApiResponseStatus = $"Błąd podczas testu OpenAI: {ex.Message}";
            }
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

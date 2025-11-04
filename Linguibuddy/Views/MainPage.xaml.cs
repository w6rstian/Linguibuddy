using Linguibuddy.Services;

namespace Linguibuddy.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly DictionaryApiService _dictionaryService = new();
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnCounterClicked(object? sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }

        private async void OnSearchClicked(object sender, EventArgs e)
        {
            var word = WordEntry.Text?.Trim();
            if (string.IsNullOrEmpty(word))
            {
                await DisplayAlert("Błąd", "Wpisz słowo do przetłumaczenia.", "OK");
                return;
            }

            var translations = await _dictionaryService.GetPolishTranslationsAsync(word);

            if (translations.Count == 0)
                await DisplayAlert("Brak wyników", $"Nie znaleziono tłumaczeń dla '{word}'.", "OK");

            ResultsList.ItemsSource = translations;
        }
    }
}

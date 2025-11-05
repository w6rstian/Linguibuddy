using Linguibuddy.Services;
using LocalizationResourceManager.Maui;
using Microsoft.Maui.Controls;

namespace Linguibuddy.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly ILocalizationResourceManager _localization; // mozna wykorzystac do zmiany jezyka w aplikacji

        private readonly DictionaryApiService _dictionaryService = new();

        public MainPage(ILocalizationResourceManager localization)
        {
            InitializeComponent();
            _localization = localization;
        }

        // Szukanie słowa w słowniku tylko do testu do usunięcia później
        private async void OnSearchClicked(object sender, EventArgs e)
        {
            ResultsLabel.Text = "⏳ Szukam...";
            var word = WordEntry.Text?.Trim().ToLower();

            if (string.IsNullOrEmpty(word))
                return;

            var entry = await _dictionaryService.GetEnglishWordAsync(word);
            if (entry == null)
            {
                ResultsLabel.Text = "Nie znaleziono słowa 😞";
                return;
            }

            ResultsLabel.Text = $"{entry.Word}\n[{entry.Phonetic}]\n\n";

            foreach (var meaning in entry.Meanings)
            {
                ResultsLabel.Text += $"{meaning.PartOfSpeech}:\n";
                foreach (var def in meaning.Definitions)
                {
                    ResultsLabel.Text += $"• {def.DefinitionText}\n";
                    if (!string.IsNullOrEmpty(def.Example))
                        ResultsLabel.Text += $"   ⤷ {def.Example}\n";
                }
                ResultsLabel.Text += "\n";
            }

            // jeśli istnieje audio, włącz przycisk
            var audioUrl = entry.Phonetics.FirstOrDefault(p => !string.IsNullOrEmpty(p.Audio))?.Audio;
            AudioButton.IsEnabled = !string.IsNullOrEmpty(audioUrl);
            AudioButton.CommandParameter = audioUrl;
        }

        private async void OnPlayAudioClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is string url)
            {
                // niektóre linki zaczynają się od "//", więc trzeba dodać https:
                if (url.StartsWith("//"))
                    url = "https:" + url;

                await Launcher.OpenAsync(url);
            }
        }
    }
}

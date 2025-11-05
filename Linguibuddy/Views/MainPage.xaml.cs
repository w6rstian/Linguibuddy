using Linguibuddy.Data;
using Linguibuddy.Services;
using Linguibuddy.ViewModels;
using LocalizationResourceManager.Maui;
using Microsoft.Maui.Controls;

namespace Linguibuddy.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly ILocalizationResourceManager _localization; // mozna wykorzystac do zmiany jezyka w aplikacji
        private readonly DictionaryApiService _dictionaryService = new();
        private readonly DeepLTranslationService _translationService;

        public MainPage(MainViewModel viewModel, ILocalizationResourceManager localization, DeepLTranslationService translationService)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _localization = localization;
            _translationService = translationService;
        }

        // Szukanie słowa w słowniku tylko do testu do usunięcia później
        private async void OnSearchClicked(object sender, EventArgs e)
        {
            ResultsLabel.Text = "⏳ Searching...";
            var word = WordEntry.Text?.Trim().ToLower();

            if (string.IsNullOrEmpty(word))
            {
                ResultsLabel.Text = "⚠️ Please enter a word.";
                return;
            }

            try
            {
                var entry = await _dictionaryService.GetEnglishWordAsync(word);
                if (entry == null)
                {
                    ResultsLabel.Text = "No results found 😞";
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

                // enable audio if exists
                var audioUrl = entry.Phonetics.FirstOrDefault(p => !string.IsNullOrEmpty(p.Audio))?.Audio;
                AudioButton.IsEnabled = !string.IsNullOrEmpty(audioUrl);
                AudioButton.CommandParameter = audioUrl;

                // 🌍 test translation to Polish using DeepL
                var translated = await _translationService.TranslateTextAsync(entry.Word, "PL");
                ResultsLabel.Text += $"\n🌐 Translation (PL): {translated}";
            }
            catch (Exception ex)
            {
                ResultsLabel.Text = $"❌ Error: {ex.Message}";
            }
        }

        private async void OnPlayAudioClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is string url)
            {
                if (url.StartsWith("//"))
                    url = "https:" + url;

                await Launcher.OpenAsync(url);
            }
        }
    }
}
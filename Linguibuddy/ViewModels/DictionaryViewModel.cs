using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Data;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.ViewModels
{
    public partial class DictionaryViewModel : ObservableObject
    {
        private readonly DataContext _dataContext;
        private readonly DictionaryApiService _dictionaryService;
        private readonly DeepLTranslationService _translationService;

        [ObservableProperty]
        public partial WordEntry? Word { get; set; }
        [ObservableProperty]
        public partial string? InputText { get; set; }
        [ObservableProperty]
        public partial string? ResultsText { get; set; }
        public IRelayCommand LookupWordCommand { get; }

        public DictionaryViewModel(DataContext dataContext, DictionaryApiService dictionaryService, DeepLTranslationService translationService)
        {
            _dataContext = dataContext;
            _dictionaryService = dictionaryService;
            _translationService = translationService;
            LookupWordCommand = new RelayCommand(async () => await LookUpWord());
        }

        public async Task LookUpWord()
        {
            ResultsText = AppResources.SearchingText;

            // Najpierw poszukać w bazie danych

            var word = InputText?.Trim().ToLower();
            if (string.IsNullOrEmpty(word))
            {
                ResultsText = AppResources.EnterWordWarning;
                return;
            }

            try
            {
                var entry = await _dictionaryService.GetEnglishWordAsync(word);
                if (entry == null)
                {
                    ResultsText = AppResources.NoResultsFoundText;
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine($"{entry.Word}\n[{entry.Phonetic}]\n");

                foreach (var meaning in entry.Meanings)
                {
                    sb.AppendLine($"{meaning.PartOfSpeech}:");
                    foreach (var def in meaning.Definitions)
                    {
                        sb.AppendLine($"• {def.DefinitionText}");
                        if (!string.IsNullOrEmpty(def.Example))
                            sb.AppendLine($"   ⤷ {def.Example}");
                    }
                    sb.AppendLine();
                }

                var translated = await _translationService.TranslateTextAsync(entry.Word, "PL");
                sb.AppendLine($"\n{AppResources.TranslationText}: {translated}");

                ResultsText = sb.ToString();
            }
            catch (Exception ex)
            {
                ResultsText = $"{AppResources.ErrorText}: {ex.Message}";
            }
            return;
        }
    }
}

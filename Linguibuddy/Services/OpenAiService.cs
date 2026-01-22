using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Newtonsoft.Json;
using OpenAI.Chat;
using System.Diagnostics;

namespace Linguibuddy.Services;

public class OpenAiService : IOpenAiService
{
    private readonly IOpenAiClientWrapper _client;

    public OpenAiService(IOpenAiClientWrapper client)
    {
        _client = client;
    }

    public async Task<string> TestConnectionAsync()
    {
        try
        {
            var response = await _client.CompleteChatAsync("Jesteś online? Odpowiedz tylko wyrazem 'TAK'");

            return response ?? "Empty response";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public async Task<string> TranslateWithContextAsync(string word, string definition, string partOfSpeech)
    {
        if (string.IsNullOrWhiteSpace(word)) return string.Empty;

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(
                    "Jesteś precyzyjnym słownikiem angielsko-polskim. " +
                    "Twoim zadaniem jest przetłumaczenie podanego słowa na język polski, " +
                    "ściśle dopasowując je do podanej definicji i części mowy. " +
                    "Zwróć TYLKO jedno przetłumaczone słowo (lub krótką frazę), bez żadnych dodatkowych opisów, kropek czy cudzysłowów."),

                new UserChatMessage(
                    $"Word to translate: {word}\n" +
                    $"Part of Speech: {partOfSpeech}\n" +
                    $"Context/Definition: {definition}")
            };

            var response = await _client.CompleteChatAsync(messages);

            return response.Trim().TrimEnd('.');
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OpenAI Error: {ex.Message}");
            return word;
        }
    }

    public async Task<(string English, string Polish)?> GenerateSentenceAsync(string targetWord, string difficultyLevel, string definition = "")
    {
        try
        {
            var userMessage = $"Target word: {targetWord}\n" +
                              $"Difficulty Level: {difficultyLevel}";

            if (!string.IsNullOrWhiteSpace(definition))
            {
                userMessage += $"\nContext/Definition: {definition}";
            }

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(
                    "Jesteś nauczycielem języka angielskiego. " +
                    "Twoim zadaniem jest ułożenie prostego zdania w języku angielskim zawierającego podane słowo. " +
                    "Zdanie musi być dostosowane do podanego poziomu trudności (CEFR). " +
                    "Jeśli podano definicję lub kontekst, upewnij się, że zdanie pasuje do tego znaczenia słowa. " +
                    "Odpowiedź musi być poprawnym obiektem JSON o strukturze: { \"english_sentence\": \"...\", \"polish_translation\": \"...\" }." +
                    "Zwróć TYLKO czysty JSON, bez bloków markdown (```json)."),

                new UserChatMessage(userMessage)
            };

            var responseText = await _client.CompleteChatAsync(messages);

            responseText = responseText.Trim()
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            var result = JsonConvert.DeserializeObject<SentenceResponse>(responseText);

            if (result != null && !string.IsNullOrEmpty(result.EnglishSentence))
                return (result.EnglishSentence, result.PolishTranslation);

            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OpenAI/Newtonsoft Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Analizuje postępy użytkownika w danej kolekcji i generuje porady.
    /// </summary>
    public async Task<string> AnalyzeCollectionProgressAsync(WordCollection collection, DifficultyLevel userDifficulty, string language)
    {
        if (collection == null || collection.Items.Count == 0)
            return "Ta kolekcja jest pusta. Dodaj słówka i zacznij naukę, aby otrzymać porady.";

        string FormatDate(DateTime? date) => date.HasValue && date.Value > DateTime.MinValue
            ? date.Value.ToString("yyyy-MM-dd")
            : "Nigdy";

        string FormatScore(double score) => $"{score:P0}"; // Formatuje 0.5 jako "50%"

        var statsReport = $"""
                           RAPORT POSTĘPÓW:
                           Kolekcja: "{collection.Name}"
                           Liczba słów: {collection.Items.Count}
                           Aktualny poziom trudności aplikacji: {userDifficulty}

                           STATYSTYKI LEKCJI (Wyniki 0-100%):

                           1. Rozpoznaj audio (Słuchanie):
                              - Najlepszy wynik: {FormatScore(collection.AudioBestScore)}
                              - Ostatni wynik: {FormatScore(collection.AudioLastScore)}
                              - Data ostatniej lekcji: {FormatDate(collection.AudioLastPlayed)}

                           2. Wymowa (Poprawne czytanie zdań):
                              - Najlepszy wynik: {FormatScore(collection.SpeakingBestScore)}
                              - Ostatni wynik: {FormatScore(collection.SpeakingLastScore)}
                              - Data ostatniej lekcji: {FormatDate(collection.SpeakingLastPlayed)}

                           3. Szyk zdania (Gramatyka/Zdania):
                              - Najlepszy wynik: {FormatScore(collection.SentenceBestScore)}
                              - Ostatni wynik: {FormatScore(collection.SentenceLastScore)}
                              - Data ostatniej lekcji: {FormatDate(collection.SentenceLastPlayed)}

                           4. Dopasuj do obrazka (Skojarzenia wzrokowe):
                              - Najlepszy wynik: {FormatScore(collection.ImageBestScore)}
                              - Ostatni wynik: {FormatScore(collection.ImageLastScore)}
                              - Data ostatniej lekcji: {FormatDate(collection.ImageLastPlayed)}

                           5. Wisielec (Słownictwo):
                              - Najlepszy wynik: {FormatScore(collection.HangmanBestScore)}
                              - Ostatni wynik: {FormatScore(collection.HangmanLastScore)}
                              - Data ostatniej lekcji: {FormatDate(collection.HangmanLastPlayed)}
                           """;

        try
        {
            var targetLanguage = language.ToLower().StartsWith("pl") ? "Polish" : "English";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(
                    "Jesteś osobistym, motywującym trenerem językowym w aplikacji 'Linguibuddy'.\n" +
                    "Twoim zadaniem jest analiza statystyk ucznia i udzielenie konkretnych wskazówek.\n\n" +
                    "ZASADY ANALIZY:\n" +
                    "1. Zaniedbania: Zwróć uwagę na rodzaj lekcji, których użytkownik dawno się nie uczył (data 'Nigdy' lub stara) lub ma w nich 0%.\n" +
                    "2. Progres: Jeśli 'Ostatni wynik' jest dużo gorszy od 'Najlepszego', zasugeruj powtórkę.\n" +
                    "3. Poziom trudności: Jeśli użytkownik ma wszędzie wyniki >90%, zasugeruj, że kolekcja jest opanowana i warto podnieść poziom trudności (CEFR) w ustawieniach.\n\n" +
                    $"WAŻNE: Całą odpowiedź wygeneruj w języku: {targetLanguage} (przetłumacz również nagłówki z sekcji FORMAT ODPOWIEDZI).\n\n" +
                    "FORMAT ODPOWIEDZI (Bądź zwięzły, używaj emoji):\n" +
                    "Ocena ogólna: [Krótkie podsumowanie]\n" +
                    "Sugerowane działania:\n" +
                    "- [Porada 1]\n" +
                    "- [Porada 2]\n" +
                    "Werdykt: Np. 'Kolekcja opanowana!' lub 'Wymaga ćwiczeń']"),

                new UserChatMessage($"Oto moje statystyki:\n{statsReport}")
            };

            return await _client.CompleteChatAsync(messages);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AI Analysis Error: {ex.Message}");
            return "Przepraszam, nie udało się połączyć z trenerem AI. Sprawdź połączenie internetowe.";
        }
    }

    /// <summary>
    /// Analizuje ogólny profil ucznia.
    /// </summary>
    public async Task<string> AnalyzeComprehensiveProfileAsync(AppUser user, int currentStreak, int unlockedAchievements, IEnumerable<WordCollection> collections, string language)
    {
        if (user == null) return "Brak danych użytkownika.";

        var wordCollections = collections.ToList();
        
        string collectionStatsReport;
        if (wordCollections.Count == 0)
        {
            collectionStatsReport = "Brak kolekcji. Użytkownik jeszcze nie dodał żadnych słówek.";
        }
        else
        {
            int totalWords = wordCollections.Sum(c => c.Items.Count);
            int activeCollectionsCount = wordCollections.Count(c => c.Items.Count > 0);
            var activeCollections = wordCollections.Where(c => c.Items.Count > 0).ToList();
            
            if (activeCollections.Count > 0)
            {
                string FormatScore(double score) => $"{score:P0}";
                
                double avgAudio = activeCollections.Average(c => c.AudioBestScore);
                double avgSpeaking = activeCollections.Average(c => c.SpeakingBestScore);
                double avgSentence = activeCollections.Average(c => c.SentenceBestScore);
                double avgImage = activeCollections.Average(c => c.ImageBestScore);
                double avgHangman = activeCollections.Average(c => c.HangmanBestScore);

                var bestCollection = activeCollections
                    .OrderByDescending(c => (c.AudioBestScore + c.SpeakingBestScore + c.SentenceBestScore + c.ImageBestScore) / 4)
                    .FirstOrDefault();

                var neglectedCollection = activeCollections
                    .OrderBy(c => (c.AudioBestScore + c.SpeakingBestScore + c.SentenceBestScore + c.ImageBestScore) / 4)
                    .FirstOrDefault();

                collectionStatsReport = $"""
                                         Liczba kolekcji: {wordCollections.Count} (Aktywne: {activeCollectionsCount})
                                         Łączna liczba słów: {totalWords}
                                         
                                         ŚREDNIE WYNIKI LEKCJI (Skill Breakdown):
                                         🎧 Słuchanie (Rozpoznaj audio): {FormatScore(avgAudio)}
                                         🗣️ Mówienie (Wymowa): {FormatScore(avgSpeaking)}
                                         📝 Gramatyka/Tłumaczenie (Szyk zdań): {FormatScore(avgSentence)}
                                         🖼️ Skojarzenia (Dopasuj do obrazka): {FormatScore(avgImage)}
                                         🔤 Słownictwo (Wisielec): {FormatScore(avgHangman)}

                                         Najlepsza kolekcja: "{(bestCollection?.Name ?? "Brak")}"
                                         Najsłabsza kolekcja: "{(neglectedCollection?.Name ?? "Brak")}"
                                         """;
            }
            else
            {
                collectionStatsReport = "Użytkownik ma kolekcje, ale są one puste (brak słówek).";
            }
        }

        var comprehensiveReport = $"""
                           RAPORT KOMPLEKSOWY UŻYTKOWNIKA:
                           
                           DANE PROFILOWE:
                           - Punkty: {user.Points}
                           - Aktualny streak (dni z rzędu): {currentStreak}
                           - Najdłuższy streak: {user.BestLearningStreak}
                           - Poziom trudności (ustawienia): {user.DifficultyLevel}
                           - Zdobyte osiągnięcia: {unlockedAchievements}
                           
                           STATYSTYKI KOLEKCJI I UMIEJĘTNOŚCI:
                           {collectionStatsReport}
                           """;

        try
        {
            var targetLanguage = language.ToLower().StartsWith("pl") ? "Polish" : "English";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(
                    "Jesteś głównym trenerem językowym w aplikacji 'Linguibuddy'. Twoim celem jest analiza postępów ucznia.\n" +
                    "Otrzymasz pełny raport zawierający dane o regularności (streak), punktach oraz wynikach z lekcji językowych.\n\n" +
                    "TWOJE ZADANIE:\n" +
                    "1. Przeanalizuj regularność (streak). Jeśli jest wysoki - pochwal. Jeśli niski lub 0 - zmotywuj do codziennej nauki.\n" +
                    "2. Spójrz na wyniki lekcji (Słuchanie, Mówienie, Gramatyka, itp.). Zidentyfikuj mocne i słabe strony. Powiedz konkretnie nad czym pracować.\n" +
                    "3. Jeśli wyniki są bardzo wysokie (>90%), a poziom trudności niski (A1/A2), zasugeruj jego zmianę.\n" +
                    "4. Zwróć uwagę na balans - czy uczeń nie unika np. Mówienia na rzecz innego trybu?\n" +
                    "5. Skup się na najważniejszym aktualnie aspekcie. Bądź pozytywny i motywujący, możesz dać jakąś przyjazną emotkę na koniec, ale nie jest to wymagane.\n\n" +
                    $"WAŻNE: Całą odpowiedź wygeneruj w języku: {targetLanguage}.\n\n" +
                    "FORMAT ODPOWIEDZI:\n" +
                    "[Jedno zdanie o stylu nauki użytkownika na podstawie danych]\n" +
                    "[Jedno zdanie podsumowujące mocne strony i to nad czym trzeba popracować.]\n" +
                    "[Jedno zdanie podsumowujące co robić dalej]"
                    ),

                new UserChatMessage($"Oto moje pełne statystyki:\n{comprehensiveReport}")
            };

            var response = await _client.CompleteChatAsync(messages);

            return response.Trim();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AI Analysis Error: {ex.Message}");
            return "Nie udało się wygenerować kompleksowego raportu. Spróbuj ponownie później.";
        }
    }



    private class SentenceResponse
    {
        [JsonProperty("english_sentence")] public string EnglishSentence { get; set; } = string.Empty;

        [JsonProperty("polish_translation")] public string PolishTranslation { get; set; } = string.Empty;
    }
}
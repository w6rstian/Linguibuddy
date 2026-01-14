using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Newtonsoft.Json;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Diagnostics;

namespace Linguibuddy.Services;

public class OpenAiService : IOpenAiService
{
    // gpt-4o-mini jest jednym z najtańszych, można będzie przetestować inne
    private const string Model = "openai/gpt-4.1-mini";

    // inny URL bo korzystamy z wersji od Githuba
    private const string Endpoint = "https://models.github.ai/inference";

    // klient OpenAI do czatu (są różne lub ogólny openai, w którym można skorzystać z opcji kilku na raz)
    // tutaj używamy ChatClient do modelu czatu do testu API (na razie)
    private readonly ChatClient _client;

    public OpenAiService(string? apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentException("API key is required", nameof(apiKey));

        _client = new ChatClient(
            Model,
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(Endpoint)
            }
        );
    }

    public async Task<string> TestConnectionAsync()
    {
        try
        {
            ChatCompletion completion = await _client.CompleteChatAsync("Jesteś online? Odpowiedz tylko wyrazem 'TAK'");

            return completion.Content[0].Text ?? "Empty response";
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

            ChatCompletion completion = await _client.CompleteChatAsync(messages);

            return completion.Content[0].Text.Trim().TrimEnd('.');
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OpenAI Error: {ex.Message}");
            return word;
        }
    }

    public async Task<(string English, string Polish)?> GenerateSentenceAsync(string targetWord, string difficultyLevel)
    {
        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(
                    "Jesteś nauczycielem języka angielskiego. " +
                    "Twoim zadaniem jest ułożenie prostego zdania w języku angielskim zawierającego podane słowo. " +
                    "Zdanie musi być dostosowane do podanego poziomu trudności (CEFR). " +
                    "Odpowiedź musi być poprawnym obiektem JSON o strukturze: { \"english_sentence\": \"...\", \"polish_translation\": \"...\" }." +
                    "Zwróć TYLKO czysty JSON, bez bloków markdown (```json)."),

                new UserChatMessage(
                    $"Target word: {targetWord}\n" +
                    $"Difficulty Level: {difficultyLevel}")
            };

            ChatCompletion completion = await _client.CompleteChatAsync(messages);

            var responseText = completion.Content[0].Text.Trim();

            responseText = responseText
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
    public async Task<string> AnalyzeCollectionProgressAsync(WordCollection collection, DifficultyLevel userDifficulty)
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

                           STATYSTYKI GIER (Wyniki 0-100%):

                           1. Audio Quiz (Słuchanie):
                              - Najlepszy wynik: {FormatScore(collection.AudioBestScore)}
                              - Ostatni wynik: {FormatScore(collection.AudioLastScore)}
                              - Data ostatniej gry: {FormatDate(collection.AudioLastPlayed)}

                           2. Speaking Quiz (Wymowa):
                              - Najlepszy wynik: {FormatScore(collection.SpeakingBestScore)}
                              - Ostatni wynik: {FormatScore(collection.SpeakingLastScore)}
                              - Data ostatniej gry: {FormatDate(collection.SpeakingLastPlayed)}

                           3. Sentence Quiz (Gramatyka/Zdania):
                              - Najlepszy wynik: {FormatScore(collection.SentenceBestScore)}
                              - Ostatni wynik: {FormatScore(collection.SentenceLastScore)}
                              - Data ostatniej gry: {FormatDate(collection.SentenceLastPlayed)}

                           4. Image Quiz (Skojarzenia wzrokowe):
                              - Najlepszy wynik: {FormatScore(collection.ImageBestScore)}
                              - Ostatni wynik: {FormatScore(collection.ImageLastScore)}
                              - Data ostatniej gry: {FormatDate(collection.ImageLastPlayed)}

                           5. Hangman (Słownictwo):
                              - Najlepszy wynik: {FormatScore(collection.HangmanBestScore)}
                              - Ostatni wynik: {FormatScore(collection.HangmanLastScore)}
                              - Data ostatniej gry: {FormatDate(collection.HangmanLastPlayed)}
                           """;

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(
                    "Jesteś osobistym, motywującym trenerem językowym w aplikacji 'Linguibuddy'. " +
                    "Twoim zadaniem jest analiza statystyk ucznia i udzielenie konkretnych wskazówek.\n\n" +
                    "ZASADY ANALIZY:\n" +
                    "1. Zaniedbania: Zwróć uwagę na gry, w które użytkownik dawno nie grał (data 'Nigdy' lub stara) lub ma w nich 0%.\n" +
                    "2. Progres: Jeśli 'Ostatni wynik' jest dużo gorszy od 'Najlepszego', zasugeruj powtórkę.\n" +
                    "3. Poziom trudności: Jeśli użytkownik ma wszędzie wyniki >90%, zasugeruj, że kolekcja jest opanowana i warto podnieść poziom trudności (CEFR) w ustawieniach.\n\n" +
                    "FORMAT ODPOWIEDZI (Bądź zwięzły, używaj emoji):\n" +
                    "📊 **Ocena ogólna:** [Krótkie podsumowanie]\n" +
                    "💡 **Sugerowane działania:**\n" +
                    "- [Porada 1]\n" +
                    "- [Porada 2]\n" +
                    "🎯 **Werdykt:** [Np. 'Kolekcja opanowana!' lub 'Wymaga ćwiczeń']"),

                new UserChatMessage($"Oto moje statystyki:\n{statsReport}")
            };

            ChatCompletion completion = await _client.CompleteChatAsync(messages);

            return completion.Content[0].Text.Trim();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AI Analysis Error: {ex.Message}");
            return "Przepraszam, nie udało się połączyć z trenerem AI. Sprawdź połączenie internetowe.";
        }
    }

    private class SentenceResponse
    {
        [JsonProperty("english_sentence")] public string EnglishSentence { get; set; } = string.Empty;

        [JsonProperty("polish_translation")] public string PolishTranslation { get; set; } = string.Empty;
    }
}
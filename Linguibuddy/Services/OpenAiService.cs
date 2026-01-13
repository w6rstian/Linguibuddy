using System.ClientModel;
using System.Diagnostics;
using Newtonsoft.Json;
using OpenAI;
using OpenAI.Chat;

namespace Linguibuddy.Services;

public class OpenAiService
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

            // Czyszczenie odpowiedzi na wypadek, gdyby AI jednak dodało markdown
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

    private class SentenceResponse
    {
        [JsonProperty("english_sentence")] public string EnglishSentence { get; set; } = string.Empty;

        [JsonProperty("polish_translation")] public string PolishTranslation { get; set; } = string.Empty;
    }
}
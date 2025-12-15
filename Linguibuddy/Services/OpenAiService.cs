using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace Linguibuddy.Services
{
    public class OpenAiService
    {
        // klient OpenAI do czatu (są różne lub ogólny openai, w którym można skorzystać z opcji kilku na raz)
        // tutaj używamy ChatClient do modelu czatu do testu API (na razie)
        private readonly ChatClient _client;

        // gpt-4o-mini jest jednym z najtańszych, można będzie przetestować inne
        private const string Model = "openai/gpt-4o-mini";
        // inny URL bo korzystamy z wersji od Githuba
        private const string Endpoint = "https://models.github.ai/inference";

        public OpenAiService(string? apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentException("API key is required", nameof(apiKey));

            _client = new ChatClient(
                model: Model,
                credential: new ApiKeyCredential(apiKey),
                options: new OpenAIClientOptions()
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
                System.Diagnostics.Debug.WriteLine($"OpenAI Error: {ex.Message}");
                return word;
            }
        }
    }
}
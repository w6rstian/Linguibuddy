using Linguibuddy.Interfaces;
using OpenAI.Chat;

namespace Linguibuddy.Services;

public class OpenAiClientWrapper : IOpenAiClientWrapper
{
    private readonly ChatClient _client;

    public OpenAiClientWrapper(ChatClient client)
    {
        _client = client;
    }

    public async Task<string> CompleteChatAsync(string prompt)
    {
        var completion = await _client.CompleteChatAsync(prompt);
        return completion.Value.Content[0].Text;
    }

    public async Task<string> CompleteChatAsync(IEnumerable<ChatMessage> messages)
    {
        var completion = await _client.CompleteChatAsync(messages);
        return completion.Value.Content[0].Text;
    }
}
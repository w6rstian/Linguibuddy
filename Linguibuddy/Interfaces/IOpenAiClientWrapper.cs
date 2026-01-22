using OpenAI.Chat;

namespace Linguibuddy.Interfaces;

public interface IOpenAiClientWrapper
{
    Task<string> CompleteChatAsync(string prompt);
    Task<string> CompleteChatAsync(IEnumerable<ChatMessage> messages);
}
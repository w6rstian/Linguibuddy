using Firebase.Auth;
using Linguibuddy.Interfaces;

namespace Linguibuddy.Services;

public class FirebaseAuthService : IAuthService
{
    private readonly FirebaseAuthClient _client;

    public FirebaseAuthService(FirebaseAuthClient client)
    {
        _client = client;
    }

    public string CurrentUserId => _client.User?.Uid ?? string.Empty;
}

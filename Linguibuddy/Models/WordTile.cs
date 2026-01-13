using CommunityToolkit.Mvvm.ComponentModel;

namespace Linguibuddy.Models;

public class WordTile : ObservableObject
{
    public WordTile(string text)
    {
        Text = text;
    }

    public string Text { get; }
    public Guid Id { get; } = Guid.NewGuid();
}
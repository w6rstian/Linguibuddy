using CommunityToolkit.Mvvm.ComponentModel;

namespace Linguibuddy.ViewModels
{
    public partial class WordTile : ObservableObject
    {
        public string Text { get; }
        public Guid Id { get; } = Guid.NewGuid();

        public WordTile(string text)
        {
            Text = text;
        }
    }
}

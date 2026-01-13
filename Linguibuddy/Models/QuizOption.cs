using CommunityToolkit.Mvvm.ComponentModel;

namespace Linguibuddy.Models
{
    public partial class QuizOption : ObservableObject
    {
        public CollectionItem Word { get; }

        [ObservableProperty]
        private Color _backgroundColor;

        [ObservableProperty]
        private bool _isEnabled;

        public QuizOption(CollectionItem word)
        {
            Word = word;
            BackgroundColor = Application.Current.Resources["Primary"] as Color ?? Colors.LightGray;
            IsEnabled = true;
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using Linguibuddy.Models;
using Microsoft.Maui.Graphics;

namespace Linguibuddy.ViewModels
{
    public partial class QuizOption : ObservableObject
    {
        public DictionaryWord Word { get; }

        [ObservableProperty]
        private Color _backgroundColor;

        [ObservableProperty]
        private bool _isEnabled;

        public QuizOption(DictionaryWord word)
        {
            Word = word;
            BackgroundColor = Application.Current.Resources["Gray600"] as Color ?? Colors.LightGray;
            IsEnabled = true;
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;

namespace Linguibuddy.ViewModels
{
    public partial class HangmanLetter : ObservableObject
    {
        public char Character { get; }

        [ObservableProperty]
        private bool _isEnabled = true;

        // Domyślnie używamy przezroczystego tła (styl outline)
        [ObservableProperty]
        private Color _backgroundColor = Colors.Transparent;

        [ObservableProperty]
        private Color _borderColor;

        [ObservableProperty]
        private Color _textColor;

        public HangmanLetter(char character, Color defaultColor)
        {
            Character = character;
            BorderColor = defaultColor; // Np. Primary
            TextColor = defaultColor;   // Np. Primary
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;

namespace Linguibuddy.Models;

public partial class HangmanLetter : ObservableObject
{
    // Domyślnie używamy przezroczystego tła (styl outline)
    [ObservableProperty] private Color _backgroundColor = Colors.Transparent;

    [ObservableProperty] private Color _borderColor;

    [ObservableProperty] private bool _isEnabled = true;

    [ObservableProperty] private Color _textColor;

    public HangmanLetter(char character, Color defaultColor)
    {
        Character = character;
        BorderColor = defaultColor; // Np. Primary
        TextColor = defaultColor; // Np. Primary
    }

    public char Character { get; }
}
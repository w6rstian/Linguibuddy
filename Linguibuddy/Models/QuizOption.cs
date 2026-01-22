using CommunityToolkit.Mvvm.ComponentModel;

namespace Linguibuddy.Models;

public partial class QuizOption : ObservableObject
{
    [ObservableProperty] private Color _backgroundColor;

    [ObservableProperty] private bool _isEnabled;

    public QuizOption(CollectionItem word)
    {
        Word = word;
        BackgroundColor = Application.Current.RequestedTheme == AppTheme.Light
            ? Application.Current.Resources["Primary"] as Color ?? Colors.LightGray
            : Application.Current.Resources["PrimaryDark"] as Color ?? Colors.LightGray;
        IsEnabled = true;
    }

    public CollectionItem Word { get; }
}
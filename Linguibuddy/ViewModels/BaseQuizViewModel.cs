using CommunityToolkit.Mvvm.ComponentModel;

namespace Linguibuddy.ViewModels;

public abstract partial class BaseQuizViewModel : ObservableObject
{
    [ObservableProperty] private Color _feedbackColor;

    [ObservableProperty] private string _feedbackMessage;

    [ObservableProperty] private bool _isAnswered;

    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string _title;

    public abstract Task LoadQuestionAsync();
}
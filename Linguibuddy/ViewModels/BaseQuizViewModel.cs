using CommunityToolkit.Mvvm.ComponentModel;

namespace Linguibuddy.ViewModels
{
    public abstract partial class BaseQuizViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private bool _isAnswered;

        [ObservableProperty]
        private string _feedbackMessage;

        [ObservableProperty]
        private Color _feedbackColor;

        public abstract Task LoadQuestionAsync();
    }
}
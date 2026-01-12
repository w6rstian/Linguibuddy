using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class SpeakingQuizPage : ContentPage
{
    private readonly SpeakingQuizViewModel _vm;
    public SpeakingQuizPage(SpeakingQuizViewModel vm)
	{
		InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadQuestionAsync();
    }
}
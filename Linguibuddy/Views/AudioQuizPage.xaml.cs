using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class AudioQuizPage : ContentPage
{
    private readonly AudioQuizViewModel _viewModel;

    public AudioQuizPage(AudioQuizViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.ImportCollectionAsync();
        await _viewModel.LoadQuestionAsync();
    }
}
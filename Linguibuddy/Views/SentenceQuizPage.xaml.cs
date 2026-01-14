using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class SentenceQuizPage : ContentPage
{
    private readonly SentenceQuizViewModel _viewModel;

    public SentenceQuizPage(SentenceQuizViewModel viewModel)
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
using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class ImageQuizPage : ContentPage
{
    private readonly ImageQuizViewModel _vm;

    public ImageQuizPage(ImageQuizViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.ImportCollectionAsync();
        await _vm.LoadQuestionAsync();
    }
}
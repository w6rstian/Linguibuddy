using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class FlashcardsCollectionsPage : ContentPage
{
    private readonly FlashcardsCollectionsViewModel _viewModel;

    public FlashcardsCollectionsPage(FlashcardsCollectionsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCollectionsCommand.ExecuteAsync(null);
    }
}
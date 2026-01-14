using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class CollectionsPage : ContentPage
{
    private readonly CollectionsViewModel _viewModel;

    public CollectionsPage(CollectionsViewModel viewModel)
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
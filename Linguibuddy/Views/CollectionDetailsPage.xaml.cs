using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class CollectionDetailsPage : ContentPage
{
    private readonly CollectionDetailsViewModel _viewModel;

    public CollectionDetailsPage(CollectionDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataCommand.ExecuteAsync(null);
    }
}
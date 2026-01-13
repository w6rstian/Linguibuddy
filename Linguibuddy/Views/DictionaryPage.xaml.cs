using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class DictionaryPage : ContentPage
{
    private readonly DictionaryViewModel _viewModel;

    public DictionaryPage(DictionaryViewModel viewModel)
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
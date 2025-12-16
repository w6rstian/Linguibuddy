using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class FlashcardsCollectionsPage : ContentPage
{
    public FlashcardsCollectionsPage(FlashcardsCollectionsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is FlashcardsCollectionsViewModel vm)
        {
            vm.LoadCollectionsCommand.Execute(null);
        }
    }
}
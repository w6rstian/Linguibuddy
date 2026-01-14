using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class CollectionDetailsPage : ContentPage
{
    public CollectionDetailsPage(CollectionDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
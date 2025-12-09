using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class FlashcardsPage : ContentPage
{
	public FlashcardsPage(FlashcardsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
    }
}
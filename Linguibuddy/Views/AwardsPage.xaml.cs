using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class AwardsPage : ContentPage
{
	public AwardsPage(AwardsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class AchievementsPage : ContentPage
{
	public AchievementsPage(AchievementsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
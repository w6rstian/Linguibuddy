using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class AchievementsPage : ContentPage
{
	private readonly AchievementsViewModel _viewModel;
    public AchievementsPage(AchievementsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}
	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);
    }
}
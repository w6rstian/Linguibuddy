using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class LeaderboardPage : ContentPage
{
    public LeaderboardPage(LeaderboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is LeaderboardViewModel vm) await vm.LoadLeaderboardCommand.ExecuteAsync(null);
    }
}
using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDifficultyAsync();
        await _viewModel.LoadLessonLengthAsync();
    }
}
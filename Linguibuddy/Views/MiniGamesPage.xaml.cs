using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class MiniGamesPage : ContentPage
{
    public MiniGamesPage(MiniGamesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
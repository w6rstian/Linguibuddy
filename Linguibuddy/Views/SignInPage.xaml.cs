using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class SignInPage : ContentPage
{
    public SignInPage(SignInViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;
    }
}
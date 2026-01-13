using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class HangmanPage : ContentPage
{
    private readonly HangmanViewModel _vm;

    public HangmanPage(HangmanViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadQuestionAsync();
    }
}
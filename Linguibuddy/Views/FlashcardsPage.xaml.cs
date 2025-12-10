using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class FlashcardsPage : ContentPage
{
    private bool _isAnimating = false;
    private bool _isFrontVisible = true;

    public FlashcardsPage(FlashcardsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
    }

    private async void OnCardTapped(object sender, EventArgs e)
    {
        if (_isAnimating) return;
        _isAnimating = true;

        await FlashcardBorder.RotateYTo(90, 200, Easing.CubicIn);

        _isFrontVisible = !_isFrontVisible;
        FrontView.IsVisible = _isFrontVisible;
        BackView.IsVisible = !_isFrontVisible;

        FlashcardBorder.RotationY = -90;

        await FlashcardBorder.RotateYTo(0, 200, Easing.CubicOut);

        _isAnimating = false;
    }

    private void OnNextCardClicked(object sender, EventArgs e)
    {
        if (!_isFrontVisible)
        {
            FrontView.IsVisible = true;
            BackView.IsVisible = false;
            _isFrontVisible = true;
            FlashcardBorder.RotationY = 0;
        }
    }
}
using CommunityToolkit.Maui.Views;
using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class WordCollectionPopup : Popup
{
    public WordCollectionPopup(WordCollectionPopupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
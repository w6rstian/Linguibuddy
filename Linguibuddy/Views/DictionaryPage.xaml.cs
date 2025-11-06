using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class DictionaryPage : ContentPage
{
	public DictionaryPage(DictionaryViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
using Linguibuddy.ViewModels;

namespace Linguibuddy.Views;

public partial class SettingsPage : ContentPage
{
	public SettingsPage(SettingsViewModel viewModel)
	{
		InitializeComponent();
		
		BindingContext = viewModel;
	}
}
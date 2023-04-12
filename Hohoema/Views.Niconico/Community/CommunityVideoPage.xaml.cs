#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.ViewModels.Pages.Niconico.Community;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Hohoema.Views.Pages.Niconico.Community;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class CommunityVideoPage : Page
	{
		public CommunityVideoPage()
		{
			this.InitializeComponent();
			DataContext = _vm = Ioc.Default.GetRequiredService<CommunityVideoPageViewModel>();
		}

		private readonly CommunityVideoPageViewModel _vm;
	}

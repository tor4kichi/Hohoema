#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.ViewModels.Pages.Niconico.Search;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Views.Pages.Niconico.Search;

public sealed partial class SearchResultKeywordPage : Page
	{
		public SearchResultKeywordPage()
		{
			this.InitializeComponent();
        DataContext = _vm = Ioc.Default.GetRequiredService<SearchResultKeywordPageViewModel>();
    }

    private readonly SearchResultKeywordPageViewModel _vm;
}

#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.ViewModels.Pages.Niconico.Search;
using NiconicoToolkit.Live;
using System.Linq;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Views.Pages.Niconico.Search;

public sealed partial class SearchResultLivePage : Page
	{
		public SearchResultLivePage()
		{
			this.InitializeComponent();
        DataContext = _vm = Ioc.Default.GetRequiredService<SearchResultLivePageViewModel>();
    }

    private readonly SearchResultLivePageViewModel _vm;

    private void Flyout_Closed(object sender, object e)
    {
        (DataContext as SearchResultLivePageViewModel)!.SearchOptionsUpdatedCommand.Execute(null);
    }
}

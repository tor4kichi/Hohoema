using NicoPlayerHohoema.Models.Live;
using NicoPlayerHohoema.Models.Niconico;
using NicoPlayerHohoema.Views;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.LiveVideoInfoContent
{
	public class CommentLiveInfoContentViewModel : LiveInfoContentViewModelBase
	{
		public ReadOnlyObservableCollection<Comment> LiveComments { get; private set; }

		public CommentLiveInfoContentViewModel(ReadOnlyObservableCollection<Comment> comments)
		{
			LiveComments = comments;
		}
	}
}

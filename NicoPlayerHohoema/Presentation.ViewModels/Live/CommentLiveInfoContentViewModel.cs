using Hohoema.Models.Domain.Live;
using Hohoema.Models.Domain.Niconico;
using Hohoema.Presentation.Views;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Live
{
	public class CommentLiveInfoContentViewModel : LiveInfoContentViewModelBase
	{
		public ReadOnlyObservableCollection<LiveComment> LiveComments { get; private set; }

		public CommentLiveInfoContentViewModel(ReadOnlyObservableCollection<LiveComment> comments)
		{
			LiveComments = comments;
		}
	}
}

using Hohoema.Models.Live;
using Hohoema.Models.Niconico;
using Hohoema.Views;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.LiveVideoInfoContent
{
	public class CommentLiveInfoContentViewModel : LiveInfoContentViewModelBase
	{
		public ReadOnlyObservableCollection<Comment> LiveComments { get; private set; }

		public NicoLiveVideo NicoLiveVideo { get; private set; }

		public CommentLiveInfoContentViewModel(NicoLiveVideo liveVideo, ReadOnlyObservableCollection<Comment> comments)
		{
			NicoLiveVideo = liveVideo;

			LiveComments = comments;
		}


	}
}

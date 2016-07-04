using NicoPlayerHohoema.Models;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.VideoInfoContent
{
	public class SettingsVideoInfoContentViewModel : MediaInfoViewModel
	{
		public SettingsVideoInfoContentViewModel(PlayerSettings settings)
			: base("")
		{
			_PlayerSettings = settings;


			CommentRenderringFPSList = new List<uint>()
			{
				5, 10, 15, 24, 30, 45, 60, 75, 90, 120
			};

			DefaultCommentDisplay = settings.ToReactivePropertyAsSynchronized(x => x.DefaultCommentDisplay);
			IncrementReadablityOwnerComment = settings.ToReactivePropertyAsSynchronized(x => x.IncrementReadablityOwnerComment);
			CommentRenderingFPS = settings.ToReactivePropertyAsSynchronized(x => x.CommentRenderingFPS);
			CommentFontScale = settings.ToReactivePropertyAsSynchronized(x => x.DefaultCommentFontScale);
			IsKeepDisplayInPlayback = settings.ToReactivePropertyAsSynchronized(x => x.IsKeepDisplayInPlayback);
			IsKeepFrontsideInPlayback = settings.ToReactivePropertyAsSynchronized(x => x.IsKeepFrontsideInPlayback);

			Observable.Merge(
				DefaultCommentDisplay.ToUnit(),
				IncrementReadablityOwnerComment.ToUnit(),
				CommentRenderingFPS.ToUnit(),
				CommentFontScale.ToUnit(),
				IsKeepDisplayInPlayback.ToUnit(),
				IsKeepFrontsideInPlayback.ToUnit()
				)
				.SubscribeOnUIDispatcher()
				.Subscribe(_ => _PlayerSettings.Save().ConfigureAwait(false));
		}

		// TODO: Dispose


		public ReactiveProperty<bool> DefaultCommentDisplay { get; private set; }
		public ReactiveProperty<bool> IncrementReadablityOwnerComment { get; private set; }
		public ReactiveProperty<uint> CommentRenderingFPS { get; private set; }
		public ReactiveProperty<float> CommentFontScale { get; private set; }
		public ReactiveProperty<bool> IsKeepDisplayInPlayback { get; private set; }
		public ReactiveProperty<bool> IsKeepFrontsideInPlayback { get; private set; }

		public List<uint> CommentRenderringFPSList { get; private set; }

		private PlayerSettings _PlayerSettings;
	}

	
}

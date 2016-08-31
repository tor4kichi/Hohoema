using NicoPlayerHohoema.Models;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace NicoPlayerHohoema.ViewModels.VideoInfoContent
{
	public class SettingsVideoInfoContentViewModel : MediaInfoViewModel
	{
		static SettingsVideoInfoContentViewModel()
		{
			CommentRenderringFPSList = new List<uint>()
			{
				5, 10, 15, 24, 30, 45, 60, 75, 90, 120
			};

			CommentColorList = new List<Color>()
			{
				Colors.WhiteSmoke,
				Colors.Black,
			};
		}



		public SettingsVideoInfoContentViewModel(PlayerSettings settings)
		{
			_PlayerSettings = settings;


			DefaultCommentDisplay = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.DefaultCommentDisplay)
				.AddTo(_CompositeDisposable);
			CommentRenderingFPS = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentRenderingFPS)
				.AddTo(_CompositeDisposable);
			CommentFontScale = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.DefaultCommentFontScale)
				.AddTo(_CompositeDisposable);
			CommentColor = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentColor)
				.AddTo(_CompositeDisposable);
			IsPauseWithCommentWriting = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.PauseWithCommentWriting)
				.AddTo(_CompositeDisposable);
			IsKeepDisplayInPlayback = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsKeepDisplayInPlayback)
				.AddTo(_CompositeDisposable);
			ScrollVolumeFrequency = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.ScrollVolumeFrequency)
				.AddTo(_CompositeDisposable);

			Observable.Merge(
				DefaultCommentDisplay.ToUnit(),
				CommentRenderingFPS.ToUnit(),
				CommentFontScale.ToUnit(),
				CommentColor.ToUnit(),
				IsKeepDisplayInPlayback.ToUnit(),
				IsPauseWithCommentWriting.ToUnit(),
				ScrollVolumeFrequency.ToUnit()
				)
				.SubscribeOnUIDispatcher()
				.Subscribe(_ => _PlayerSettings.Save().ConfigureAwait(false))
				.AddTo(_CompositeDisposable);
		}

		// TODO: Dispose
		protected override void OnDispose()
		{
			
		}

		public ReactiveProperty<bool> DefaultCommentDisplay { get; private set; }
		public ReactiveProperty<uint> CommentRenderingFPS { get; private set; }
		public ReactiveProperty<double> CommentFontScale { get; private set; }
		public ReactiveProperty<Color> CommentColor { get; private set; }
		public ReactiveProperty<bool> IsPauseWithCommentWriting { get; private set; }
		public ReactiveProperty<bool> IsKeepDisplayInPlayback { get; private set; }
		public ReactiveProperty<double> ScrollVolumeFrequency { get; private set; }


		public static List<Color> CommentColorList { get; private set; }
		public static List<uint> CommentRenderringFPSList { get; private set; }

		private PlayerSettings _PlayerSettings;

		
	}

	
}

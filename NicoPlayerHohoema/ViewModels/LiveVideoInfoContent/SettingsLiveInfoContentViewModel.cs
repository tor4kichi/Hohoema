using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Live;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace NicoPlayerHohoema.ViewModels.LiveVideoInfoContent
{
	public class SettingsLiveInfoContentViewModel : LiveInfoContentViewModelBase
	{
		public static List<Color> CommentColorList { get; private set; }
		public static List<uint> CommentRenderringFPSList { get; private set; }

		static SettingsLiveInfoContentViewModel()
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

		public SettingsLiveInfoContentViewModel(NicoLiveVideo liveVideo, HohoemaApp hohoemaApp)
		{
			_PlayerSettings = hohoemaApp.UserSettings.PlayerSettings;


			CommentRenderingFPS = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentRenderingFPS)
				.AddTo(_CompositeDisposable);
			CommentDisplayDuration = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentDisplayDuration, x => x.TotalSeconds, x => TimeSpan.FromSeconds(x))
				.AddTo(_CompositeDisposable);
			CommentFontScale = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.DefaultCommentFontScale)
				.AddTo(_CompositeDisposable);
			CommentColor = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentColor)
				.AddTo(_CompositeDisposable);
			ScrollVolumeFrequency = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.SoundVolumeChangeFrequency)
				.AddTo(_CompositeDisposable);
		}


		public ReactiveProperty<uint> CommentRenderingFPS { get; private set; }
		public ReactiveProperty<double> CommentDisplayDuration { get; private set; }
		public ReactiveProperty<double> CommentFontScale { get; private set; }
		public ReactiveProperty<Color> CommentColor { get; private set; }
		public ReactiveProperty<double> ScrollVolumeFrequency { get; private set; }

		private PlayerSettings _PlayerSettings;
	}
}

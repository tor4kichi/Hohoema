using Hohoema.Models;
using Hohoema.Models.Live;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace Hohoema.ViewModels.LiveVideoInfoContent
{
	public class SettingsLiveInfoContentViewModel : LiveInfoContentViewModelBase
	{
        public SettingsLiveInfoContentViewModel(NicoLiveVideo liveVideo, PlayerSettings playerSettings)
        {
            PlayerSettings = playerSettings;

            CommentRenderingFPS = PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentRenderingFPS)
                .AddTo(_CompositeDisposable);
            CommentDisplayDuration = PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentDisplayDuration, x => x.TotalSeconds, x => TimeSpan.FromSeconds(x))
                .AddTo(_CompositeDisposable);
            CommentFontScale = PlayerSettings.ToReactivePropertyAsSynchronized(x => x.DefaultCommentFontScale)
                .AddTo(_CompositeDisposable);
            CommentColor = PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentColor)
                .AddTo(_CompositeDisposable);
            ScrollVolumeFrequency = PlayerSettings.ToReactivePropertyAsSynchronized(x => x.SoundVolumeChangeFrequency)
                .AddTo(_CompositeDisposable);
        }

        static public List<Color> CommentColorList { get; private set; }
        static public List<uint> CommentRenderringFPSList { get; private set; }

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


        public PlayerSettings PlayerSettings { get; }


        public ReactiveProperty<uint> CommentRenderingFPS { get; private set; }
		public ReactiveProperty<double> CommentDisplayDuration { get; private set; }
		public ReactiveProperty<double> CommentFontScale { get; private set; }
		public ReactiveProperty<Color> CommentColor { get; private set; }
		public ReactiveProperty<double> ScrollVolumeFrequency { get; private set; }

		
	}
}

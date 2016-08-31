using NicoPlayerHohoema.Models;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace NicoPlayerHohoema.ViewModels
{
	public class VideoPlaySettingsPageContentViewModel : SettingsPageContentViewModel
	{
		public VideoPlaySettingsPageContentViewModel(HohoemaApp hohoemaApp, string title)
			: base(title)
		{
			_PlayerSettings = hohoemaApp.UserSettings.PlayerSettings;

			

			IsDefaultPlayWithLowQuality = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsLowQualityDeafult);
			IsFullScreenDefault = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsFullScreenDefault);

			IsKeepDisplayInPlayback = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsKeepDisplayInPlayback);
			ScrollVolumeFrequency = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.ScrollVolumeFrequency);
		}

		public override void OnLeave()
		{
			_PlayerSettings.Save().ConfigureAwait(false);
		}

		public ReactiveProperty<bool> IsDefaultPlayWithLowQuality { get; private set; }
		public ReactiveProperty<bool> IsFullScreenDefault { get; private set; }

		public ReactiveProperty<bool> IsKeepDisplayInPlayback { get; private set; }
		public ReactiveProperty<double> ScrollVolumeFrequency { get; private set; }


		private PlayerSettings _PlayerSettings;
	}
}

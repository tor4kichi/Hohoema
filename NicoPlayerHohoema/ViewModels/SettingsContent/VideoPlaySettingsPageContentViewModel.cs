using NicoPlayerHohoema.Models;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
	public class VideoPlaySettingsPageContentViewModel : SettingsPageContentViewModel
	{
		public VideoPlaySettingsPageContentViewModel(HohoemaApp hohoemaApp, string title)
			: base(title)
		{
			_PlayerSettings = hohoemaApp.UserSettings.PlayerSettings;

			IsDefaultPlayWithLowQuality = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsLowQualityDeafult);
			IsKeepDisplayInPlayback = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsKeepDisplayInPlayback);
			IsPauseWithCommentWriting = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.PauseWithCommentWriting);
			ScrollVolumeFrequency = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.ScrollVolumeFrequency);
		}

		public override void OnLeave()
		{
			_PlayerSettings.Save().ConfigureAwait(false);
		}


		public ReactiveProperty<bool> IsDefaultPlayWithLowQuality { get; private set; }
		public ReactiveProperty<bool> IsKeepDisplayInPlayback { get; private set; }
		public ReactiveProperty<bool> IsPauseWithCommentWriting { get; private set; }
		public ReactiveProperty<float> ScrollVolumeFrequency { get; private set; }

		private PlayerSettings _PlayerSettings;
	}
}

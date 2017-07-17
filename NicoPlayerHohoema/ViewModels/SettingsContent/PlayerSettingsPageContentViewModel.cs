using NicoPlayerHohoema.Models;
using Prism.Commands;
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
	public class PlayerSeetingPageContentViewModel : SettingsPageContentViewModel
	{







        public static List<NicoVideoQuality> QualityList { get; private set; }
        public ReactiveProperty<NicoVideoQuality> DefaultQuality { get; private set; }

        public ReactiveProperty<bool> IsFullScreenDefault { get; private set; }
        public ReactiveProperty<bool> IsForceLandscapeDefault { get; private set; }

        public ReactiveProperty<bool> IsKeepDisplayInPlayback { get; private set; }
        public ReactiveProperty<double> ScrollVolumeFrequency { get; private set; }

        public ReactiveProperty<double> AutoHideDelayTime { get; private set; }

        public ReactiveProperty<double> DefaultPlaybackRate { get; private set; }
        public DelegateCommand ResetDefaultPlaybackRateCommand { get; private set; }


        public ReactiveProperty<bool> CommentGlassMowerEnable { get; private set; }



        public ReactiveProperty<bool> IsDefaultCommentWithAnonymous { get; private set; }
        public ReactiveProperty<bool> DefaultCommentDisplay { get; private set; }
        public ReactiveProperty<uint> CommentRenderingFPS { get; private set; }
        public ReactiveProperty<double> CommentDisplayDuration { get; private set; }
        public ReactiveProperty<double> CommentFontScale { get; private set; }
        public ReactiveProperty<Color> CommentColor { get; private set; }
        public ReactiveProperty<bool> IsPauseWithCommentWriting { get; private set; }

        public static List<CommentOpacityKind> CommentOpacityList { get; private set; }
        public ReactiveProperty<CommentOpacityKind> CommentOpacity { get; private set; }


        public static List<Color> CommentColorList { get; private set; }
        public static List<uint> CommentRenderringFPSList { get; private set; }


        public ReactiveProperty<bool> IsEnableOwnerCommentCommand { get; private set; }
        public ReactiveProperty<bool> IsEnableUserCommentCommand { get; private set; }
        public ReactiveProperty<bool> IsEnableAnonymousCommentCommand { get; private set; }


        public static List<PlaylistEndAction> PlaylistEndActionList { get; private set; }
        public ReactiveProperty<PlaylistEndAction> PlaylistEndAction { get; private set; }


        private HohoemaApp _HohoemaApp;
        private NGSettings _NGSettings;

        private PlayerSettings _PlayerSettings;
        private PlaylistSettings _PlaylistSettings;

        static PlayerSeetingPageContentViewModel()
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

            PlaylistEndActionList = new List<Models.PlaylistEndAction>()
            {
                Models.PlaylistEndAction.None,
                Models.PlaylistEndAction.ChangeIntoSplit,
                Models.PlaylistEndAction.CloseIfPlayWithCurrentWindow
            };

            QualityList = new List<NicoVideoQuality>()
            {
                NicoVideoQuality.Dmc_High,
                NicoVideoQuality.Dmc_Midium,
                NicoVideoQuality.Dmc_Low,
                NicoVideoQuality.Dmc_Mobile
            };

            CommentOpacityList = new List<CommentOpacityKind>()
            {
                CommentOpacityKind.NoSukesuke,
                CommentOpacityKind.BitSukesuke,
                CommentOpacityKind.MoreSukesuke
            };
        }


        public PlayerSeetingPageContentViewModel(HohoemaApp hohoemaApp)
			: base("プレイヤー", HohoemaSettingsKind.Player)
		{
            _HohoemaApp = hohoemaApp;
            _NGSettings = _HohoemaApp.UserSettings.NGSettings;
            _PlayerSettings = _HohoemaApp.UserSettings.PlayerSettings;
            _PlaylistSettings = _HohoemaApp.UserSettings.PlaylistSettings;

            DefaultQuality = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.DefaultQuality);
			IsFullScreenDefault = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsFullScreenDefault);

			IsKeepDisplayInPlayback = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsKeepDisplayInPlayback);
			ScrollVolumeFrequency = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.ScrollVolumeFrequency);
			IsForceLandscapeDefault = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsForceLandscape);

			AutoHideDelayTime = _PlayerSettings.ToReactivePropertyAsSynchronized(x => 
				x.AutoHidePlayerControlUIPreventTime
				, x => x.TotalSeconds
				, x => TimeSpan.FromSeconds(x)
				);

            DefaultPlaybackRate = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.DefaultPlaybackRate);
            ResetDefaultPlaybackRateCommand = new DelegateCommand(() => DefaultPlaybackRate.Value = 1.0);

            PlaylistEndAction = _PlaylistSettings.ToReactivePropertyAsSynchronized(x => x.PlaylistEndAction);

            // NG Comment User Id



            // Comment Display 
            DefaultCommentDisplay = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.DefaultCommentDisplay);
            CommentColor = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentColor);
            IsPauseWithCommentWriting = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.PauseWithCommentWriting);
            CommentRenderingFPS = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentRenderingFPS);
            CommentDisplayDuration = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentDisplayDuration, x => x.TotalSeconds, x => TimeSpan.FromSeconds(x));
            CommentFontScale = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.DefaultCommentFontScale);
            CommentGlassMowerEnable = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentGlassMowerEnable);
            IsDefaultCommentWithAnonymous = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsDefaultCommentWithAnonymous);
            CommentOpacity = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentOpacity);

            IsEnableOwnerCommentCommand = new ReactiveProperty<bool>(_PlayerSettings.CommentCommandPermission.HasFlag(CommentCommandPermissionType.Owner));
            IsEnableUserCommentCommand = new ReactiveProperty<bool>(_PlayerSettings.CommentCommandPermission.HasFlag(CommentCommandPermissionType.User));
            IsEnableAnonymousCommentCommand = new ReactiveProperty<bool>(_PlayerSettings.CommentCommandPermission.HasFlag(CommentCommandPermissionType.Anonymous));

            IsEnableOwnerCommentCommand.Subscribe(x => SetCommentCommandPermission(x, CommentCommandPermissionType.Owner));
            IsEnableUserCommentCommand.Subscribe(x => SetCommentCommandPermission(x, CommentCommandPermissionType.User));
            IsEnableAnonymousCommentCommand.Subscribe(x => SetCommentCommandPermission(x, CommentCommandPermissionType.Anonymous));



            
        }

        protected override void OnLeave()
		{
            _NGSettings.Save().ConfigureAwait(false);
            _PlayerSettings.Save().ConfigureAwait(false);
            _PlaylistSettings.Save().ConfigureAwait(false);

        }






        private void SetCommentCommandPermission(bool isEnable, CommentCommandPermissionType type)
        {
            if (isEnable)
            {
                _PlayerSettings.CommentCommandPermission |= type;
            }
            else
            {
                _PlayerSettings.CommentCommandPermission = _PlayerSettings.CommentCommandPermission & ~type;
            }
        }


	}
}

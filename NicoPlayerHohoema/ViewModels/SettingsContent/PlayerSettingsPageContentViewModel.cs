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







        public ReactiveProperty<bool> IsDefaultPlayWithLowQuality { get; private set; }
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

        public static List<Color> CommentColorList { get; private set; }
        public static List<uint> CommentRenderringFPSList { get; private set; }


        public ReactiveProperty<bool> IsEnableOwnerCommentCommand { get; private set; }
        public ReactiveProperty<bool> IsEnableUserCommentCommand { get; private set; }
        public ReactiveProperty<bool> IsEnableAnonymousCommentCommand { get; private set; }



        private HohoemaApp _HohoemaApp;
        private NGSettings _NGSettings;

        private PlayerSettings _PlayerSettings;

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
        }


        public PlayerSeetingPageContentViewModel(HohoemaApp hohoemaApp)
			: base("プレイヤー", HohoemaSettingsKind.Player)
		{
            _HohoemaApp = hohoemaApp;
            _NGSettings = _HohoemaApp.UserSettings.NGSettings;
            _PlayerSettings = hohoemaApp.UserSettings.PlayerSettings;


            IsDefaultPlayWithLowQuality = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsLowQualityDeafult);
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

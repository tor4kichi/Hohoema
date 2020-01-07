using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI;

namespace NicoPlayerHohoema.ViewModels.PlayerSidePaneContent
{
    public class SettingsSidePaneContentViewModel : SidePaneContentViewModelBase
    {
        public SettingsSidePaneContentViewModel(
            NGSettings ngSettings, 
            PlayerSettings playerSettings, 
            PlaylistSettings playlistSettings,
            IScheduler scheduler
            )
        {
            _NGSettings = ngSettings;
            _PlayerSettings = playerSettings;
            _PlaylistSettings = playlistSettings;
            _scheduler = scheduler;
            VideoPlayingQuality = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.DefaultQuality,
                convert: x => VideoPlayingQualityList.First(y => y.Value == x),
                convertBack: x => x.Value,
                raiseEventScheduler: _scheduler,
                mode: ReactivePropertyMode.DistinctUntilChanged
                )
            .AddTo(_CompositeDisposable);
            VideoPlayingQuality.Subscribe(x =>
            {
                VideoQualityChanged?.Invoke(this, x.Value);
            })
            .AddTo(_CompositeDisposable);


            VideoPlaybackRate = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.PlaybackRate);
            SetPlaybackRateCommand = VideoPlaybackRate.Select(
                rate => rate != 1.0
                )
                .ToReactiveCommand<double?>(_scheduler)
            .AddTo(_CompositeDisposable);

            SetPlaybackRateCommand.Subscribe(
                (rate) => VideoPlaybackRate.Value = rate.HasValue ? rate.Value : 1.0
                )
            .AddTo(_CompositeDisposable);



            LiveVideoPlayingQuality = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.DefaultLiveQuality,
                convert: x => LivePlayingQualityList.FirstOrDefault(y => y.Value == x),
                convertBack: x => x.Value,
                raiseEventScheduler: _scheduler,
                mode: ReactivePropertyMode.DistinctUntilChanged
                )
            .AddTo(_CompositeDisposable);

            IsLowLatency = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.LiveWatchWithLowLatency, _scheduler, mode: ReactivePropertyMode.DistinctUntilChanged)
            .AddTo(_CompositeDisposable);

            IsKeepDisplayInPlayback = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsKeepDisplayInPlayback, _scheduler)
            .AddTo(_CompositeDisposable);
            ScrollVolumeFrequency = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.SoundVolumeChangeFrequency, _scheduler)
            .AddTo(_CompositeDisposable);
            IsForceLandscapeDefault = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsForceLandscape, _scheduler)
            .AddTo(_CompositeDisposable);

            AutoHideDelayTime = _PlayerSettings.ToReactivePropertyAsSynchronized(x =>
                x.AutoHidePlayerControlUIPreventTime
                , x => x.TotalSeconds
                , x => TimeSpan.FromSeconds(x)
                , _scheduler
                )
            .AddTo(_CompositeDisposable);

            PlaylistEndAction = _PlaylistSettings.ToReactivePropertyAsSynchronized(x => x.PlaylistEndAction, _scheduler)
            .AddTo(_CompositeDisposable);

            AutoMoveNextVideoOnPlaylistEmpty = _PlaylistSettings.ToReactivePropertyAsSynchronized(x => x.AutoMoveNextVideoOnPlaylistEmpty, _scheduler)
            .AddTo(_CompositeDisposable);


            // NG Comment User Id



            // Comment Display 
            CommentColor = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentColor, _scheduler)
            .AddTo(_CompositeDisposable);
            IsPauseWithCommentWriting = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.PauseWithCommentWriting, _scheduler)
            .AddTo(_CompositeDisposable);
            CommentRenderingFPS = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentRenderingFPS, _scheduler)
            .AddTo(_CompositeDisposable);
            CommentDisplayDuration = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentDisplayDuration, x => x.TotalSeconds, x => TimeSpan.FromSeconds(x), _scheduler)
            .AddTo(_CompositeDisposable);
            CommentFontScale = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.DefaultCommentFontScale, _scheduler)
            .AddTo(_CompositeDisposable);
            IsDefaultCommentWithAnonymous = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsDefaultCommentWithAnonymous, _scheduler)
            .AddTo(_CompositeDisposable);
            CommentOpacity = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentOpacity, _scheduler)
            .AddTo(_CompositeDisposable);

            IsEnableOwnerCommentCommand = new ReactiveProperty<bool>(_scheduler, _PlayerSettings.CommentCommandPermission.HasFlag(CommentCommandPermissionType.Owner))
            .AddTo(_CompositeDisposable);
            IsEnableUserCommentCommand = new ReactiveProperty<bool>(_scheduler, _PlayerSettings.CommentCommandPermission.HasFlag(CommentCommandPermissionType.User))
            .AddTo(_CompositeDisposable);
            IsEnableAnonymousCommentCommand = new ReactiveProperty<bool>(_scheduler, _PlayerSettings.CommentCommandPermission.HasFlag(CommentCommandPermissionType.Anonymous))
            .AddTo(_CompositeDisposable);

            IsEnableOwnerCommentCommand.Subscribe(x => SetCommentCommandPermission(x, CommentCommandPermissionType.Owner))
            .AddTo(_CompositeDisposable);
            IsEnableUserCommentCommand.Subscribe(x => SetCommentCommandPermission(x, CommentCommandPermissionType.User))
            .AddTo(_CompositeDisposable);
            IsEnableAnonymousCommentCommand.Subscribe(x => SetCommentCommandPermission(x, CommentCommandPermissionType.Anonymous))
            .AddTo(_CompositeDisposable);

            NicoScript_Default_Enabled = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.NicoScript_Default_Enabled, raiseEventScheduler: _scheduler)
                .AddTo(_CompositeDisposable);
            NicoScript_DisallowSeek_Enabled = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.NicoScript_DisallowSeek_Enabled, raiseEventScheduler: _scheduler)
                .AddTo(_CompositeDisposable);
            NicoScript_DisallowComment_Enabled = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.NicoScript_DisallowComment_Enabled, raiseEventScheduler: _scheduler)
                .AddTo(_CompositeDisposable);
            NicoScript_Jump_Enabled = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.NicoScript_Jump_Enabled, raiseEventScheduler: _scheduler)
                .AddTo(_CompositeDisposable);
            NicoScript_Replace_Enabled = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.NicoScript_Replace_Enabled, raiseEventScheduler: _scheduler)
                .AddTo(_CompositeDisposable);


            // NG Comment

            NGCommentUserIdEnable = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGCommentUserIdEnable, _scheduler)
            .AddTo(_CompositeDisposable);
            NGCommentUserIds = _NGSettings.NGCommentUserIds
                .ToReadOnlyReactiveCollection(x =>
                    RemovableSettingsListItemHelper.UserIdInfoToRemovableListItemVM(x, OnRemoveNGCommentUserIdFromList),
                    _scheduler
                    )
            .AddTo(_CompositeDisposable);

            NGCommentKeywordEnable = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGCommentKeywordEnable, _scheduler)
            .AddTo(_CompositeDisposable);
            NGCommentKeywords = new ReactiveProperty<string>(_scheduler, string.Empty)
            .AddTo(_CompositeDisposable);

            NGCommentKeywordError = NGCommentKeywords
                .Select(x =>
                {
                    var keywords = x.Split('\r');
                    var invalidRegex = keywords.FirstOrDefault(keyword =>
                    {
                        Regex regex = null;
                        try
                        {
                            regex = new Regex(keyword);
                        }
                        catch { }
                        return regex == null;
                    });

                    if (invalidRegex == null)
                    {
                        return string.Empty;
                    }
                    else
                    {
                        return $"Error in \"{invalidRegex}\"";
                    }
                })
                .ToReadOnlyReactiveProperty(eventScheduler: _scheduler)
            .AddTo(_CompositeDisposable);

            NGCommentScoreTypes = ((IEnumerable<NGCommentScore>)Enum.GetValues(typeof(NGCommentScore))).ToList();

            SelectedNGCommentScore = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGCommentScoreType, _scheduler)
            .AddTo(_CompositeDisposable);



            CommentGlassMowerEnable = _PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.CommentGlassMowerEnable, _scheduler)
            .AddTo(_CompositeDisposable);





            NGCommentKeywords.Value = string.Join("\r", _NGSettings.NGCommentKeywords.Select(x => x.Keyword)) + "\r";
            NGCommentKeywords.Throttle(TimeSpan.FromSeconds(3))
                .Subscribe(_ =>
                {
                    _NGSettings.NGCommentKeywords.Clear();
                    foreach (var ngKeyword in NGCommentKeywords.Value.Split('\r'))
                    {
                        if (!string.IsNullOrWhiteSpace(ngKeyword))
                        {
                            _NGSettings.NGCommentKeywords.Add(new NGKeyword() { Keyword = ngKeyword });
                        }
                    }
                    _NGSettings.Save().ConfigureAwait(false);
                })
                .AddTo(_CompositeDisposable);
        }


        public event EventHandler<NicoVideoQuality> VideoQualityChanged;

        // Video Settings
        public static List<ValueWithAvairability<NicoVideoQuality>> VideoPlayingQualityList { get; } = new []
        {
            new ValueWithAvairability<NicoVideoQuality>(NicoVideoQuality.Dmc_SuperHigh),
            new ValueWithAvairability<NicoVideoQuality>(NicoVideoQuality.Dmc_High),
            new ValueWithAvairability<NicoVideoQuality>(NicoVideoQuality.Dmc_Midium),
            new ValueWithAvairability<NicoVideoQuality>(NicoVideoQuality.Dmc_Low),
            new ValueWithAvairability<NicoVideoQuality>(NicoVideoQuality.Dmc_Mobile),
        }.ToList();

        public ReactiveProperty<ValueWithAvairability<NicoVideoQuality>> VideoPlayingQuality { get; private set; }
        public ReactiveProperty<bool> IsLowLatency { get; private set; }

        public ReactiveProperty<double> VideoPlaybackRate { get; private set; }
        public ReactiveCommand<double?> SetPlaybackRateCommand { get; private set; }

        public static List<double> VideoPlaybackRateList { get; }

        // Live Settings
        public static List<ValueWithAvairability<string>> LivePlayingQualityList { get; } = new[]
        {
            new ValueWithAvairability<string>("super_high"),
            new ValueWithAvairability<string>("high"),
            new ValueWithAvairability<string>("normal"),
            new ValueWithAvairability<string>("low"),
            new ValueWithAvairability<string>("super_low"),
        }.ToList();
        public ReactiveProperty<ValueWithAvairability<string>> LiveVideoPlayingQuality { get; private set; }
        private bool _IsLeoPlayerLive;
        public bool IsLeoPlayerLive
        {
            get { return _IsLeoPlayerLive; }
            set { SetProperty(ref _IsLeoPlayerLive, value); }
        }

        // Player Settings
        public ReactiveProperty<bool> IsForceLandscapeDefault { get; private set; }

        public ReactiveProperty<bool> IsKeepDisplayInPlayback { get; private set; }
        public ReactiveProperty<double> ScrollVolumeFrequency { get; private set; }

        public ReactiveProperty<double> AutoHideDelayTime { get; private set; }

        public DelegateCommand ResetDefaultPlaybackRateCommand { get; private set; }




        public ReactiveProperty<bool> IsDefaultCommentWithAnonymous { get; private set; }
        public ReactiveProperty<uint> CommentRenderingFPS { get; private set; }
        public ReactiveProperty<double> CommentDisplayDuration { get; private set; }
        public ReactiveProperty<double> CommentFontScale { get; private set; }
        public ReactiveProperty<Color> CommentColor { get; private set; }
        public ReactiveProperty<bool> IsPauseWithCommentWriting { get; private set; }

        public ReactiveProperty<double> CommentOpacity { get; private set; }


        public static List<Color> CommentColorList { get; private set; }
        public static List<uint> CommentRenderringFPSList { get; private set; }


        public ReactiveProperty<bool> IsEnableOwnerCommentCommand { get; private set; }
        public ReactiveProperty<bool> IsEnableUserCommentCommand { get; private set; }
        public ReactiveProperty<bool> IsEnableAnonymousCommentCommand { get; private set; }

        public ReactiveProperty<bool> NicoScript_Default_Enabled { get; private set; }
        public ReactiveProperty<bool> NicoScript_DisallowSeek_Enabled { get; private set; }
        public ReactiveProperty<bool> NicoScript_DisallowComment_Enabled { get; private set; }
        public ReactiveProperty<bool> NicoScript_Jump_Enabled { get; private set; }
        public ReactiveProperty<bool> NicoScript_Replace_Enabled { get; private set; }

        public static List<PlaylistEndAction> PlaylistEndActionList { get; private set; }
        public ReactiveProperty<PlaylistEndAction> PlaylistEndAction { get; private set; }

        public ReactiveProperty<bool> AutoMoveNextVideoOnPlaylistEmpty { get; private set; }

        // NG Comments

        public ReactiveProperty<bool> NGCommentUserIdEnable { get; private set; }
        public ReadOnlyReactiveCollection<RemovableListItem<string>> NGCommentUserIds { get; private set; }

        public ReactiveProperty<bool> NGCommentKeywordEnable { get; private set; }
        public ReactiveProperty<string> NGCommentKeywords { get; private set; }
        public ReadOnlyReactiveProperty<string> NGCommentKeywordError { get; private set; }

        public List<NGCommentScore> NGCommentScoreTypes { get; private set; }
        public ReactiveProperty<NGCommentScore> SelectedNGCommentScore { get; private set; }


        public ReactiveProperty<bool> CommentGlassMowerEnable { get; private set; }



        private NGSettings _NGSettings;

        private PlayerSettings _PlayerSettings;
        private PlaylistSettings _PlaylistSettings;
        private readonly IScheduler _scheduler;

        static SettingsSidePaneContentViewModel()
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
                Models.PlaylistEndAction.NothingDo,
                Models.PlaylistEndAction.ChangeIntoSplit,
                Models.PlaylistEndAction.CloseIfPlayWithCurrentWindow
            };

            VideoPlaybackRateList = new List<double>()
            {
                2.0,
                1.75,
                1.5,
                1.25,
                1.0,
                .75,
                .5,
                .25,
                .05
            };
        }

        
        
        public void SetupAvairableLiveQualities(IList<string> qualities)
        {
            if (qualities == null) { return; }

            foreach (var i in LivePlayingQualityList)
            {
                i.IsAvairable = qualities.Any(x => x == i.Value);
            }
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

        protected override void OnDispose()
        {
            base.OnDispose();
        }

        private void OnRemoveNGCommentUserIdFromList(string userId)
        {
            var removeTarget = _NGSettings.NGCommentUserIds.First(x => x.UserId == userId);
            _NGSettings.NGCommentUserIds.Remove(removeTarget);
        }

    }


    public class ValueWithAvairability<T> : BindableBase
    {
        public ValueWithAvairability(T value, bool isAvairable = true)
        {
            Value = value;
            IsAvairable = isAvairable;
        }
        public T Value { get; set; }

        private bool _IsAvairable;
        public bool IsAvairable
        {
            get { return _IsAvairable; }
            set { SetProperty(ref _IsAvairable, value); }
        }
    }

}

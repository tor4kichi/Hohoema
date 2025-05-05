#nullable enable
using Hohoema.Infra;
using Hohoema.Models.Niconico.Video;
using NiconicoToolkit.Live.WatchSession;
using System;
using Windows.UI;

namespace Hohoema.Models.Player;

public enum PlaylistEndAction
{
    NothingDo,
    ChangeIntoSplit,
    CloseIfPlayWithCurrentWindow,
}

public class PlayerSettings : FlagsRepositoryBase
{
    public static TimeSpan DefaultCommentDisplayDuration { get; private set; } = TimeSpan.FromSeconds(4);


    public PlayerSettings()
    {
        _defaultVideoQualityId = Read(default(string?), nameof(DefaultVideoQualityId));

        _defaultLiveQuality = Read(LiveQualityType.Normal, nameof(DefaultLiveQuality));
        _liveQualityLimit = Read(LiveQualityLimitType.SuperHigh, nameof(LiveQualityLimit));
        _liveWatchWithLowLatency = Read(true, nameof(LiveWatchWithLowLatency));

        _isMute = Read(false, nameof(IsMute));
        _soundVolume = Read(1.0, nameof(SoundVolume));
        _soundVolumeChangeFrequency = Read(0.05, nameof(SoundVolumeChangeFrequency));
        _isLoudnessCorrectionEnabled = Read(true, nameof(IsLoudnessCorrectionEnabled));

        _isCommentDisplay_Video = Read(true, nameof(IsCommentDisplay_Video));
        _isShowCommentList_Video = Read(false, nameof(IsShowCommentList_Video));
        _isCommentDisplay_Live = Read(true, nameof(IsCommentDisplay_Live));
        _pauseWithCommentWriting = Read(false, nameof(PauseWithCommentWriting));
        _commentDisplayDuration = Read(DefaultCommentDisplayDuration, nameof(CommentDisplayDuration));
        _defaultCommentFontScale = Read(1.0, nameof(DefaultCommentFontScale));
        _commentOpacity = Read(1.0, nameof(CommentOpacity));
        _isDefaultCommentWithAnonymous = Read(true, nameof(IsDefaultCommentWithAnonymous));
        _commentColor = Read(Colors.WhiteSmoke, nameof(CommentColor));

        _isAutoHidePlayerControlUI = Read(true, nameof(IsAutoHidePlayerControlUI));
        _autoHidePlayerControlUIPreventTime = Read(TimeSpan.FromSeconds(3), nameof(AutoHidePlayerControlUIPreventTime));

        _isForceLandscape = Read(false, nameof(IsForceLandscape));
        _playbackRate = Read(1.0, nameof(PlaybackRate));


        _nicoScript_DisallowSeek_Enabled = Read(true, nameof(NicoScript_DisallowSeek_Enabled));

        _nicoScript_Default_Enabled = Read(true, nameof(NicoScript_Default_Enabled));
        _nicoScript_Jump_Enabled = Read(true, nameof(NicoScript_Jump_Enabled));
        _nicoScript_DisallowComment_Enabled = Read(true, nameof(NicoScript_DisallowComment_Enabled));
        _nicoScript_Replace_Enabled = Read(true, nameof(NicoScript_Replace_Enabled));

        _isCurrentVideoLoopingEnabled = Read(false, nameof(IsCurrentVideoLoopingEnabled));
        _isPlaylistAutoMoveEnabled = Read(true, nameof(IsPlaylistAutoMoveEnabled));
        _isPlaylistLoopingEnabled = Read(false, nameof(IsPlaylistLoopingEnabled));
        _isShuffleEnable = Read(false, nameof(IsShuffleEnable));
        _isReverseModeEnable = Read(false, nameof(IsReverseModeEnable));
        _playlistEndAction = Read(PlaylistEndAction.NothingDo, nameof(PlaylistEndAction));
        _autoMoveNextVideoOnPlaylistEmpty = Read(true, nameof(AutoMoveNextVideoOnPlaylistEmpty));
        _isMitigationForVideoLoadingEnabled = Read(false, nameof(IsMitigationForVideoLoadingEnabled));
    }


    private string? _defaultVideoQualityId;
    public string? DefaultVideoQualityId
    {
        get => _defaultVideoQualityId;
        set => SetProperty(ref _defaultVideoQualityId, value);
    }


    private LiveQualityType _defaultLiveQuality = LiveQualityType.Normal;
    public LiveQualityType DefaultLiveQuality
    {
        get => _defaultLiveQuality;
        set => SetProperty(ref _defaultLiveQuality, value);
    }

    private LiveQualityLimitType _liveQualityLimit;
    public LiveQualityLimitType LiveQualityLimit
    {
        get => _liveQualityLimit;
        set => SetProperty(ref _liveQualityLimit, value);
    }

    private bool _liveWatchWithLowLatency;
    public bool LiveWatchWithLowLatency
    {
        get => _liveWatchWithLowLatency;
        set => SetProperty(ref _liveWatchWithLowLatency, value);
    }


    #region Sound

    private bool _isMute;
    public bool IsMute
    {
        get => _isMute;
        set => SetProperty(ref _isMute, value);
    }


    private double _soundVolume;
    public double SoundVolume
    {
        get => _soundVolume;
        set => SetProperty(ref _soundVolume, Math.Min(1.0, Math.Max(0.0, value)));
    }


    private double _soundVolumeChangeFrequency;
    public double SoundVolumeChangeFrequency
    {
        get => _soundVolumeChangeFrequency;
        set => SetProperty(ref _soundVolumeChangeFrequency, Math.Max(0.001f, value));
    }

    private bool _isLoudnessCorrectionEnabled;
    public bool IsLoudnessCorrectionEnabled
    {
        get => _isLoudnessCorrectionEnabled;
        set => SetProperty(ref _isLoudnessCorrectionEnabled, value);
    }

    #endregion

    private bool _isShowCommentList_Video;
    public bool IsShowCommentList_Video
    {
        get => _isShowCommentList_Video;
        set => SetProperty(ref _isShowCommentList_Video, value);
    }

    private bool _isCommentDisplay_Video;
    public bool IsCommentDisplay_Video
    {
        get => _isCommentDisplay_Video;
        set => SetProperty(ref _isCommentDisplay_Video, value);
    }

    private bool _isCommentDisplay_Live;
    public bool IsCommentDisplay_Live
    {
        get => _isCommentDisplay_Live;
        set => SetProperty(ref _isCommentDisplay_Live, value);
    }

    private bool _pauseWithCommentWriting;
    public bool PauseWithCommentWriting
    {
        get => _pauseWithCommentWriting;
        set => SetProperty(ref _pauseWithCommentWriting, value);
    }

    private TimeSpan _commentDisplayDuration;
    public TimeSpan CommentDisplayDuration
    {
        get => _commentDisplayDuration;
        set => SetProperty(ref _commentDisplayDuration, value);
    }

    private double _defaultCommentFontScale;
    public double DefaultCommentFontScale
    {
        get => _defaultCommentFontScale;
        set => SetProperty(ref _defaultCommentFontScale, value);
    }

    private double _commentOpacity;
    public double CommentOpacity
    {
        get => _commentOpacity;
        set => SetProperty(ref _commentOpacity, Math.Min(1.0, Math.Max(0.0, value)));
    }

    private bool _isDefaultCommentWithAnonymous;
    public bool IsDefaultCommentWithAnonymous
    {
        get => _isDefaultCommentWithAnonymous;
        set => SetProperty(ref _isDefaultCommentWithAnonymous, value);
    }

    private Color _commentColor;
    public Color CommentColor
    {
        get => _commentColor;
        set => SetProperty(ref _commentColor, value);
    }

    private bool _isAutoHidePlayerControlUI;
    public bool IsAutoHidePlayerControlUI
    {
        get => _isAutoHidePlayerControlUI;
        set => SetProperty(ref _isAutoHidePlayerControlUI, value);
    }

    private TimeSpan _autoHidePlayerControlUIPreventTime;
    public TimeSpan AutoHidePlayerControlUIPreventTime
    {
        get => _autoHidePlayerControlUIPreventTime;
        set => SetProperty(ref _autoHidePlayerControlUIPreventTime, value);
    }

    private bool _isForceLandscape;
    public bool IsForceLandscape
    {
        get => _isForceLandscape;
        set => SetProperty(ref _isForceLandscape, value);
    }

    private double _playbackRate;
    public double PlaybackRate
    {
        get => _playbackRate;
        set
        {
            double trimValue = Math.Min(2.0, Math.Max(0.1, value));
            SetProperty(ref _playbackRate, trimValue);
        }
    }
    
    private bool _nicoScript_DisallowSeek_Enabled;
    public bool NicoScript_DisallowSeek_Enabled
    {
        get => _nicoScript_DisallowSeek_Enabled;
        set => SetProperty(ref _nicoScript_DisallowSeek_Enabled, value);
    }

    private bool _nicoScript_Default_Enabled;
    public bool NicoScript_Default_Enabled
    {
        get => _nicoScript_Default_Enabled;
        set => SetProperty(ref _nicoScript_Default_Enabled, value);
    }

    private bool _nicoScript_Jump_Enabled;
    public bool NicoScript_Jump_Enabled
    {
        get => _nicoScript_Jump_Enabled;
        set => SetProperty(ref _nicoScript_Jump_Enabled, value);
    }

    private bool _nicoScript_DisallowComment_Enabled;
    public bool NicoScript_DisallowComment_Enabled
    {
        get => _nicoScript_DisallowComment_Enabled;
        set => SetProperty(ref _nicoScript_DisallowComment_Enabled, value);
    }

    private bool _nicoScript_Replace_Enabled;
    public bool NicoScript_Replace_Enabled
    {
        get => _nicoScript_Replace_Enabled;
        set => SetProperty(ref _nicoScript_Replace_Enabled, value);
    }

    #region Playlist

    private bool _isCurrentVideoLoopingEnabled;
    public bool IsCurrentVideoLoopingEnabled
    {
        get => _isCurrentVideoLoopingEnabled;
        set => SetProperty(ref _isCurrentVideoLoopingEnabled, value);
    }

    private bool _isPlaylistAutoMoveEnabled;
    public bool IsPlaylistAutoMoveEnabled
    {
        get => _isPlaylistAutoMoveEnabled;
        set => SetProperty(ref _isPlaylistAutoMoveEnabled, value);
    }

    private bool _isPlaylistLoopingEnabled;
    public bool IsPlaylistLoopingEnabled
    {
        get => _isPlaylistLoopingEnabled;
        set => SetProperty(ref _isPlaylistLoopingEnabled, value);
    }

    private bool _isShuffleEnable;
    public bool IsShuffleEnable
    {
        get => _isShuffleEnable;
        set => SetProperty(ref _isShuffleEnable, value);
    }

    private bool _isReverseModeEnable;
    public bool IsReverseModeEnable
    {
        get => _isReverseModeEnable;
        set => SetProperty(ref _isReverseModeEnable, value);
    }

    private PlaylistEndAction _playlistEndAction;
    public PlaylistEndAction PlaylistEndAction
    {
        get => _playlistEndAction;
        set => SetProperty(ref _playlistEndAction, value);
    }

    private bool _autoMoveNextVideoOnPlaylistEmpty;
    public bool AutoMoveNextVideoOnPlaylistEmpty
    {
        get => _autoMoveNextVideoOnPlaylistEmpty;
        set => SetProperty(ref _autoMoveNextVideoOnPlaylistEmpty, value);
    }

    #endregion

    public bool ForceUsingDmcVideoOrigin
    {
        get => Read(false);
        set => Save(value);
    }

    private bool _isMitigationForVideoLoadingEnabled;
    public bool IsMitigationForVideoLoadingEnabled
    {
        get => _isMitigationForVideoLoadingEnabled;
        set => SetProperty(ref _isMitigationForVideoLoadingEnabled, value);
    }
}

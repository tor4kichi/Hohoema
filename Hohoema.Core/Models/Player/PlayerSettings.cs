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

    [Obsolete]
    public PlayerSettings()
    {
        _DefaultVideoQuality = Read(NicoVideoQuality.Midium, nameof(DefaultVideoQuality));

        _DefaultLiveQuality = Read(LiveQualityType.Normal, nameof(DefaultLiveQuality));
        _LiveQualityLimit = Read(LiveQualityLimitType.SuperHigh, nameof(LiveQualityLimit));
        _LiveWatchWithLowLatency = Read(true, nameof(LiveWatchWithLowLatency));

        _IsMute = Read(false, nameof(IsMute));
        _SoundVolume = Read(1.0, nameof(SoundVolume));
        _SoundVolumeChangeFrequency = Read(0.05, nameof(SoundVolumeChangeFrequency));
        _IsLoudnessCorrectionEnabled = Read(true, nameof(IsLoudnessCorrectionEnabled));

        _IsCommentDisplay_Video = Read(true, nameof(IsCommentDisplay_Video));
        _IsShowCommentList_Video = Read(false, nameof(IsShowCommentList_Video));
        _IsCommentDisplay_Live = Read(true, nameof(IsCommentDisplay_Live));
        _PauseWithCommentWriting = Read(false, nameof(PauseWithCommentWriting));
        _CommentDisplayDuration = Read(DefaultCommentDisplayDuration, nameof(CommentDisplayDuration));
        _DefaultCommentFontScale = Read(1.0, nameof(DefaultCommentFontScale));
        _CommentOpacity = Read(1.0, nameof(CommentOpacity));
        _IsDefaultCommentWithAnonymous = Read(true, nameof(IsDefaultCommentWithAnonymous));
        _CommentColor = Read(Colors.WhiteSmoke, nameof(CommentColor));

        _IsAutoHidePlayerControlUI = Read(true, nameof(IsAutoHidePlayerControlUI));
        _AutoHidePlayerControlUIPreventTime = Read(TimeSpan.FromSeconds(3), nameof(AutoHidePlayerControlUIPreventTime));

        _IsForceLandscape = Read(false, nameof(IsForceLandscape));
        _PlaybackRate = Read(1.0, nameof(PlaybackRate));


        _NicoScript_DisallowSeek_Enabled = Read(true, nameof(NicoScript_DisallowSeek_Enabled));

        _NicoScript_Default_Enabled = Read(true, nameof(NicoScript_Default_Enabled));
        _NicoScript_Jump_Enabled = Read(true, nameof(NicoScript_Jump_Enabled));
        _NicoScript_DisallowComment_Enabled = Read(true, nameof(NicoScript_DisallowComment_Enabled));
        _NicoScript_Replace_Enabled = Read(true, nameof(NicoScript_Replace_Enabled));

        _isCurrentVideoLoopingEnabled = Read(false, nameof(IsCurrentVideoLoopingEnabled));
        _isPlaylistAutoMoveEnabled = Read(true, nameof(IsPlaylistAutoMoveEnabled));
        _isPlaylistLoopingEnabled = Read(false, nameof(IsPlaylistLoopingEnabled));
        _IsShuffleEnable = Read(false, nameof(IsShuffleEnable));
        _IsReverseModeEnable = Read(false, nameof(IsReverseModeEnable));
        _PlaylistEndAction = Read(PlaylistEndAction.NothingDo, nameof(PlaylistEndAction));
        _AutoMoveNextVideoOnPlaylistEmpty = Read(true, nameof(AutoMoveNextVideoOnPlaylistEmpty));
    }



    private NicoVideoQuality _DefaultVideoQuality;

    [Obsolete]
    public NicoVideoQuality DefaultVideoQuality
    {
        get => _DefaultVideoQuality;
        set => SetProperty(ref _DefaultVideoQuality, value);
    }


    private LiveQualityType _DefaultLiveQuality = LiveQualityType.Normal;

    [Obsolete]
    public LiveQualityType DefaultLiveQuality
    {
        get => _DefaultLiveQuality;
        set => SetProperty(ref _DefaultLiveQuality, value);
    }

    private LiveQualityLimitType _LiveQualityLimit;

    [Obsolete]
    public LiveQualityLimitType LiveQualityLimit
    {
        get => _LiveQualityLimit;
        set => SetProperty(ref _LiveQualityLimit, value);
    }

    private bool _LiveWatchWithLowLatency;

    [Obsolete]
    public bool LiveWatchWithLowLatency
    {
        get => _LiveWatchWithLowLatency;
        set => SetProperty(ref _LiveWatchWithLowLatency, value);
    }


    #region Sound

    private bool _IsMute;

    [Obsolete]
    public bool IsMute
    {
        get => _IsMute;
        set => SetProperty(ref _IsMute, value);
    }


    private double _SoundVolume;

    [Obsolete]
    public double SoundVolume
    {
        get => _SoundVolume;
        set => SetProperty(ref _SoundVolume, Math.Min(1.0, Math.Max(0.0, value)));
    }






    private double _SoundVolumeChangeFrequency;

    [Obsolete]
    public double SoundVolumeChangeFrequency
    {
        get => _SoundVolumeChangeFrequency;
        set => SetProperty(ref _SoundVolumeChangeFrequency, Math.Max(0.001f, value));
    }


    private bool _IsLoudnessCorrectionEnabled;

    [Obsolete]
    public bool IsLoudnessCorrectionEnabled
    {
        get => _IsLoudnessCorrectionEnabled;
        set => SetProperty(ref _IsLoudnessCorrectionEnabled, value);
    }


    #endregion


    private bool _IsShowCommentList_Video;

    [Obsolete]
    public bool IsShowCommentList_Video
    {
        get => _IsShowCommentList_Video;
        set => SetProperty(ref _IsShowCommentList_Video, value);
    }

    private bool _IsCommentDisplay_Video;

    [Obsolete]
    public bool IsCommentDisplay_Video
    {
        get => _IsCommentDisplay_Video;
        set => SetProperty(ref _IsCommentDisplay_Video, value);
    }


    private bool _IsCommentDisplay_Live;

    [Obsolete]
    public bool IsCommentDisplay_Live
    {
        get => _IsCommentDisplay_Live;
        set => SetProperty(ref _IsCommentDisplay_Live, value);
    }



    private bool _PauseWithCommentWriting;

    [Obsolete]
    public bool PauseWithCommentWriting
    {
        get => _PauseWithCommentWriting;
        set => SetProperty(ref _PauseWithCommentWriting, value);
    }


    private TimeSpan _CommentDisplayDuration;

    [Obsolete]
    public TimeSpan CommentDisplayDuration
    {
        get => _CommentDisplayDuration;
        set => SetProperty(ref _CommentDisplayDuration, value);
    }



    private double _DefaultCommentFontScale;

    [Obsolete]
    public double DefaultCommentFontScale
    {
        get => _DefaultCommentFontScale;
        set => SetProperty(ref _DefaultCommentFontScale, value);
    }


    private double _CommentOpacity;

    [Obsolete]
    public double CommentOpacity
    {
        get => _CommentOpacity;
        set => SetProperty(ref _CommentOpacity, Math.Min(1.0, Math.Max(0.0, value)));
    }




    private bool _IsDefaultCommentWithAnonymous;

    [Obsolete]
    public bool IsDefaultCommentWithAnonymous
    {
        get => _IsDefaultCommentWithAnonymous;
        set => SetProperty(ref _IsDefaultCommentWithAnonymous, value);
    }

    private Color _CommentColor;

    [Obsolete]
    public Color CommentColor
    {
        get => _CommentColor;
        set => SetProperty(ref _CommentColor, value);
    }



    private bool _IsAutoHidePlayerControlUI;

    [Obsolete]
    public bool IsAutoHidePlayerControlUI
    {
        get => _IsAutoHidePlayerControlUI;
        set => SetProperty(ref _IsAutoHidePlayerControlUI, value);
    }

    private TimeSpan _AutoHidePlayerControlUIPreventTime;

    [Obsolete]
    public TimeSpan AutoHidePlayerControlUIPreventTime
    {
        get => _AutoHidePlayerControlUIPreventTime;
        set => SetProperty(ref _AutoHidePlayerControlUIPreventTime, value);
    }


    private bool _IsForceLandscape;

    [Obsolete]
    public bool IsForceLandscape
    {
        get => _IsForceLandscape;
        set => SetProperty(ref _IsForceLandscape, value);
    }

    private double _PlaybackRate;

    [Obsolete]
    public double PlaybackRate
    {
        get => _PlaybackRate;
        set
        {
            double trimValue = Math.Min(2.0, Math.Max(0.1, value));
            _ = SetProperty(ref _PlaybackRate, trimValue);
        }
    }


    public bool _NicoScript_DisallowSeek_Enabled;

    [Obsolete]
    public bool NicoScript_DisallowSeek_Enabled
    {
        get => _NicoScript_DisallowSeek_Enabled;
        set => SetProperty(ref _NicoScript_DisallowSeek_Enabled, value);
    }

    public bool _NicoScript_Default_Enabled;

    [Obsolete]
    public bool NicoScript_Default_Enabled
    {
        get => _NicoScript_Default_Enabled;
        set => SetProperty(ref _NicoScript_Default_Enabled, value);
    }

    public bool _NicoScript_Jump_Enabled;

    [Obsolete]
    public bool NicoScript_Jump_Enabled
    {
        get => _NicoScript_Jump_Enabled;
        set => SetProperty(ref _NicoScript_Jump_Enabled, value);
    }


    public bool _NicoScript_DisallowComment_Enabled;

    [Obsolete]
    public bool NicoScript_DisallowComment_Enabled
    {
        get => _NicoScript_DisallowComment_Enabled;
        set => SetProperty(ref _NicoScript_DisallowComment_Enabled, value);
    }


    public bool _NicoScript_Replace_Enabled;

    [Obsolete]
    public bool NicoScript_Replace_Enabled
    {
        get => _NicoScript_Replace_Enabled;
        set => SetProperty(ref _NicoScript_Replace_Enabled, value);
    }




    #region Playlist


    private bool _isCurrentVideoLoopingEnabled;

    [Obsolete]
    public bool IsCurrentVideoLoopingEnabled
    {
        get => _isCurrentVideoLoopingEnabled;
        set => SetProperty(ref _isCurrentVideoLoopingEnabled, value);
    }


    private bool _isPlaylistAutoMoveEnabled;

    [Obsolete]
    public bool IsPlaylistAutoMoveEnabled
    {
        get => _isPlaylistAutoMoveEnabled;
        set => SetProperty(ref _isPlaylistAutoMoveEnabled, value);
    }

    private bool _isPlaylistLoopingEnabled;

    [Obsolete]
    public bool IsPlaylistLoopingEnabled
    {
        get => _isPlaylistLoopingEnabled;
        set => SetProperty(ref _isPlaylistLoopingEnabled, value);
    }

    private bool _IsShuffleEnable;

    [Obsolete]
    public bool IsShuffleEnable
    {
        get => _IsShuffleEnable;
        set => SetProperty(ref _IsShuffleEnable, value);
    }


    private bool _IsReverseModeEnable;

    [Obsolete]
    public bool IsReverseModeEnable
    {
        get => _IsReverseModeEnable;
        set => SetProperty(ref _IsReverseModeEnable, value);
    }



    private PlaylistEndAction _PlaylistEndAction;

    [Obsolete]
    public PlaylistEndAction PlaylistEndAction
    {
        get => _PlaylistEndAction;
        set => SetProperty(ref _PlaylistEndAction, value);
    }


    private bool _AutoMoveNextVideoOnPlaylistEmpty;

    [Obsolete]
    public bool AutoMoveNextVideoOnPlaylistEmpty
    {
        get => _AutoMoveNextVideoOnPlaylistEmpty;
        set => SetProperty(ref _AutoMoveNextVideoOnPlaylistEmpty, value);
    }

    #endregion
}

using CommunityToolkit.Mvvm.ComponentModel;
using Hohoema.Infra;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace Hohoema.Models.Niconico.Video;

public class VideoCacheSettings_Legacy : FlagsRepositoryBase
{
    [System.Obsolete]
    public VideoCacheSettings_Legacy()
    {
        _IsEnableCache = Read(false, nameof(IsEnableCache));
        _IsUserAcceptedCache = Read(false, nameof(IsUserAcceptedCache));
        _DefaultCacheQuality = Read(NicoVideoQuality.Midium, nameof(DefaultCacheQuality));

        _IsAutoCacheOnPlayEnable = Read(false, nameof(IsAutoCacheOnPlayEnable));
        _IsAllowDownloadOnMeteredNetwork = Read(false, nameof(IsAllowDownloadOnMeteredNetwork));
        _CacheQualityOnMeteredNetwork = Read(NicoVideoQuality.Mobile, nameof(CacheQualityOnMeteredNetwork));
    }

    [System.Obsolete]
    public bool CanDownload => IsUserAcceptedCache && IsEnableCache;


    private bool _IsEnableCache;

    [System.Obsolete]
    public bool IsEnableCache
    {
        get => _IsEnableCache;
        set => SetProperty(ref _IsEnableCache, value);
    }

    private bool _IsUserAcceptedCache;

    [System.Obsolete]
    public bool IsUserAcceptedCache
    {
        get => _IsUserAcceptedCache;
        set => SetProperty(ref _IsUserAcceptedCache, value);
    }


    private bool _IsAutoCacheOnPlayEnable;

    [System.Obsolete]
    public bool IsAutoCacheOnPlayEnable
    {
        get => _IsAutoCacheOnPlayEnable;
        set => SetProperty(ref _IsAutoCacheOnPlayEnable, value);
    }


    private NicoVideoQuality _DefaultCacheQuality = NicoVideoQuality.Midium;

    [System.Obsolete]
    public NicoVideoQuality DefaultCacheQuality
    {
        get => _DefaultCacheQuality;
        set => SetProperty(ref _DefaultCacheQuality, value);
    }





    private bool _IsAllowDownloadOnMeteredNetwork = false;

    [System.Obsolete]
    public bool IsAllowDownloadOnMeteredNetwork
    {
        get => _IsAllowDownloadOnMeteredNetwork;
        set => SetProperty(ref _IsAllowDownloadOnMeteredNetwork, value);
    }



    private NicoVideoQuality _CacheQualityOnMeteredNetwork = NicoVideoQuality.Mobile;

    [System.Obsolete]
    public NicoVideoQuality CacheQualityOnMeteredNetwork
    {
        get => _CacheQualityOnMeteredNetwork;
        set => SetProperty(ref _CacheQualityOnMeteredNetwork, value);
    }
}

[DataContract]
public class TagCondition : ObservableObject
{
    public TagCondition()
    {
        Label = "";
        IncludeTags = new ObservableCollection<string>();
        ExcludeTags = new ObservableCollection<string>();
        Quality = null;
    }

    private string _Label;

    [DataMember]
    public string Label
    {
        get => _Label;
        set => SetProperty(ref _Label, value);
    }

    private NicoVideoQuality? _Quality;

    [DataMember]
    public NicoVideoQuality? Quality
    {
        get => _Quality;
        set => SetProperty(ref _Quality, value);
    }

    [DataMember]
    public ObservableCollection<string> IncludeTags { get; private set; }
    [DataMember]
    public ObservableCollection<string> ExcludeTags { get; private set; }
}

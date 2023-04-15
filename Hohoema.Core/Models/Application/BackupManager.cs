#nullable enable
using CommunityToolkit.Mvvm.Messaging.Messages;
using Hohoema.Contracts.Subscriptions;
using Hohoema.Models.LocalMylist;
using Hohoema.Models.Niconico.NicoRepo;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Pins;
using Hohoema.Models.Player;
using Hohoema.Models.Player.Comment;
using Hohoema.Models.Playlist;
using Hohoema.Models.Subscriptions;
using LiteDB;
using NiconicoToolkit.Live.WatchSession;
using NiconicoToolkit.NicoRepo;
using NiconicoToolkit.Ranking.Video;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Hohoema.Models.Application;

public sealed class SettingsRestoredMessage : ValueChangedMessage<long>
{
    public SettingsRestoredMessage() : base(0)
    {
    }
}

public sealed class BackupManager
{
    private readonly LocalMylistRepository _playlistRepository;
    private readonly SubscriptionRegistrationRepository _subscriptionRegistrationRepository;
    private readonly SubscriptionGroupRepository _subscriptionGroupRepository;
    private readonly PinSettings _pinSettings;
    private readonly VideoRankingSettings _videoRankingSettings;
    private readonly VideoFilteringSettings _videoFilteringSettings;
    private readonly PlayerSettings _playerSettings;
    private readonly AppearanceSettings _appearanceSettings;
    private readonly NicoRepoSettings _nicoRepoSettings;
    private readonly CommentFliteringRepository _commentFliteringRepository;
    private readonly JsonSerializerOptions _options;




    public BackupManager(LocalMylistRepository playlistRepository,
        SubscriptionRegistrationRepository subscriptionRegistrationRepository,
        SubscriptionGroupRepository subscriptionGroupRepository,
        PinSettings pinSettings,
        VideoRankingSettings videoRankingSettings,
        VideoFilteringSettings videoFilteringSettings,
        PlayerSettings playerSettings,
        AppearanceSettings appearanceSettings,
        NicoRepoSettings nicoRepoSettings,
        CommentFliteringRepository commentFliteringRepository
        )
    {
        _playlistRepository = playlistRepository;
        _subscriptionRegistrationRepository = subscriptionRegistrationRepository;
        _subscriptionGroupRepository = subscriptionGroupRepository;
        _pinSettings = pinSettings;
        _videoRankingSettings = videoRankingSettings;
        _videoFilteringSettings = videoFilteringSettings;
        _playerSettings = playerSettings;
        _appearanceSettings = appearanceSettings;
        _nicoRepoSettings = nicoRepoSettings;
        _commentFliteringRepository = commentFliteringRepository;
        _options = new JsonSerializerOptions()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumMemberConverter()
            }
        };
    }

    [Obsolete]
    public async Task BackupAsync(StorageFile storageFile, CancellationToken ct = default)
    {
        BackupContainer container = new()
        {
            LocalMylistItems = _playlistRepository.GetAllPlaylist()
                .Select(x => new LocalMylistBackupEntry
                {
                    Id = x.Id,
                    Label = x.Label,
                    VideoIdList = _playlistRepository.GetItems(x.Id).Select(x => x.ContentId).ToArray()
                })
                .ToArray(),
            SubscriptionItems = _subscriptionRegistrationRepository.ReadAllItems()
                .Select(x => new SubscriptionBackupEntry
                {
                    Id = x.SubscriptionId.AsPrimitive(),
                    Label = x.Label,
                    SourceType = x.SourceType switch
                    {
                        SubscriptionSourceType.Mylist => BackupSusbcriptionSourceType.Mylist,
                        SubscriptionSourceType.User => BackupSusbcriptionSourceType.User,
                        SubscriptionSourceType.Channel => BackupSusbcriptionSourceType.Channel,
                        SubscriptionSourceType.Series => BackupSusbcriptionSourceType.Series,
                        SubscriptionSourceType.SearchWithKeyword => BackupSusbcriptionSourceType.SearchWithKeyword,
                        SubscriptionSourceType.SearchWithTag => BackupSusbcriptionSourceType.SearchWithTag,
                        _ => throw new NotSupportedException()
                    },
                    SourceParameter = x.SourceParameter,
                    GroupId = x.Group?.GroupId.ToString()
                })
                .ToArray(),
            SubscriptionGroupItems = _subscriptionGroupRepository.ReadAllItems()
                .Where(x => x.GroupId != SubscriptionGroupId.DefaultGroupId)
                .Select(x => new SubscriptionGroupBackupEntry()
                {
                    GroupId = x.GroupId.ToString(),
                    Label = x.Name,
                    Order = x.Order,
                })
                .ToArray(),
            PinItems = _pinSettings.ReadAllItems()
                .OrderBy(x => x.SortIndex)
                .Select(x => new PinBackupEntry
                {
                    Id = x.Id,
                    Label = x.Label,
                    OverrideLabel = x.OverrideLabel,
                    PageName = x.PageType.ToString(),
                    Parameter = x.Parameter,
                    SubItems = x.SubItems.OrderBy(x => x.SortIndex).Select(item => new PinBackupEntry { Id = item.Id, Label = item.Label, OverrideLabel = item.OverrideLabel, PageName = item.PageType.ToString(), Parameter = item.Parameter, PinType = BackupBookmarkType.Item, SubItems = null }).ToList(),
                    PinType = x.PinType switch { BookmarkType.Item => BackupBookmarkType.Item, BookmarkType.Folder => BackupBookmarkType.Folder, _ => throw new NotSupportedException() }
                })
                .ToArray(),
            VideoRankingFiltering = new RankingFilteringBackupEntry
            {
                HiddenGenres = _videoRankingSettings.HiddenGenres.Select(x => x.ToString()).ToArray(),
                HiddenTags = _videoRankingSettings.HiddenTags.Select(x => new RankingFilteringBackupGenreTag { Label = x.Label, Genre = x.Genre.ToString(), Tag = x.Tag }).ToArray(),
                FavoriteTags = _videoRankingSettings.FavoriteTags.Select(x => new RankingFilteringBackupGenreTag { Label = x.Label, Genre = x.Genre.ToString(), Tag = x.Tag }).ToArray(),
            },

            VideoListringFiltering = new VideoFilteringBackupEntry
            {
                IsEnabledFilteringOwners = _videoFilteringSettings.NGVideoOwnerUserIdEnable,
                OnwerIds = _videoFilteringSettings.GetVideoOwnerIdFilteringEntries().Select(x => new IdAndLabel { Id = x.UserId, Label = x.Description }).ToArray(),
                IsEnabledFilteringVideoIds = _videoFilteringSettings.NGVideoIdEnable,
                VideoIds = _videoFilteringSettings.GetVideoIdFilteringEntries().Select(x => new IdAndLabel { Id = x.VideoId, Label = x.Description }).ToArray(),
                IsEnabledFilteringTitles = _videoFilteringSettings.NGVideoTitleKeywordEnable,
                Titles = _videoFilteringSettings.GetVideoTitleFilteringEntries().Select(x => x.Keyword).ToArray()
            },

            PlayerSetting = new PlayerSettingBackupEntry
            {
                DefaultQuality = _playerSettings.DefaultVideoQuality.ToString(),
                DefaultLiveQuality = _playerSettings.DefaultLiveQuality.ToString(),
                LiveQualityLimit = _playerSettings.LiveQualityLimit.ToString(),
                LiveWatchWithLowLatency = _playerSettings.LiveWatchWithLowLatency,
                IsMute = _playerSettings.IsMute,
                SoundVolume = _playerSettings.SoundVolume,
                SoundVolumeChangeFrequency = _playerSettings.SoundVolumeChangeFrequency,
                IsLoudnessCorrectionEnabled = _playerSettings.IsLoudnessCorrectionEnabled,
                IsCommentDisplay_Video = _playerSettings.IsCommentDisplay_Video,
                IsCommentDisplay_Live = _playerSettings.IsCommentDisplay_Live,
                PauseWithCommentWriting = _playerSettings.PauseWithCommentWriting,
                CommentDisplayDurationInMS = _playerSettings.CommentDisplayDuration.TotalMilliseconds,
                DefaultCommentFontScale = _playerSettings.DefaultCommentFontScale,
                CommentOpacity = _playerSettings.CommentOpacity,
                IsDefaultCommentWithAnonymous = _playerSettings.IsDefaultCommentWithAnonymous,
                CommentColor = _playerSettings.CommentColor,
                IsAutoHidePlayerControlUI = _playerSettings.IsAutoHidePlayerControlUI,
                AutoHidePlayerControlUIPreventTimeInMS = _playerSettings.AutoHidePlayerControlUIPreventTime.TotalMilliseconds,
                IsForceLandscape = _playerSettings.IsForceLandscape,
                PlaybackRate = _playerSettings.PlaybackRate,
                NicoScript_DisallowSeek_Enabled = _playerSettings.NicoScript_DisallowSeek_Enabled,
                NicoScript_Default_Enabled = _playerSettings.NicoScript_Default_Enabled,
                NicoScript_Jump_Enabled = _playerSettings.NicoScript_Jump_Enabled,
                NicoScript_DisallowComment_Enabled = _playerSettings.NicoScript_DisallowComment_Enabled,
                NicoScript_Replace_Enabled = _playerSettings.NicoScript_Replace_Enabled,
                IsCurrentVideoLoopingEnabled = _playerSettings.IsCurrentVideoLoopingEnabled,
                IsPlaylistLoopingEnabled = _playerSettings.IsPlaylistLoopingEnabled,
                IsShuffleEnable = _playerSettings.IsShuffleEnable,
                IsReverseModeEnable = _playerSettings.IsReverseModeEnable,
                PlaylistEndAction = _playerSettings.PlaylistEndAction.ToString(),
                AutoMoveNextVideoOnPlaylistEmpty = _playerSettings.AutoMoveNextVideoOnPlaylistEmpty,
            },

            AppearanceSettings = new AppearanceSettingsBackupEntry
            {
                Locale = _appearanceSettings.Locale,
                FirstAppearPageType = _appearanceSettings.FirstAppearPageType.ToString(),
                OverrideInteractionMode = _appearanceSettings.OverrideInteractionMode?.ToString(),
                ApplicationTheme = _appearanceSettings.ApplicationTheme.ToString()
            },

            NicoRepoSettings = new NicoRepoSettingsBackupEntry
            {
                DisplayNicoRepoMuteContextTriggers = _nicoRepoSettings.DisplayNicoRepoMuteContextTriggers.Select(x => x.ToString()).ToArray()
            },

            CommentSettingsBackupEntry = new CommentSettingsBackupEntry
            {
                ShareNGScore = _commentFliteringRepository.ShareNGScore,
                IsFilteringCommentOwnerIdEnabled = _commentFliteringRepository.IsFilteringCommentOwnerIdEnabled,
                CommentFilteringOwnerIdBackups = _commentFliteringRepository.GetAllFilteringCommenOwnerId().Select(x => new CommentFilteringOwnerIdBackup { UserId = x.UserId }).ToArray(),
                CommentTextTransformConditionBackups = _commentFliteringRepository.GetAllCommentTextTransformCondition().Select(x => new CommentTextTransformConditionBackup { IsEnabled = x.IsEnabled, RegexPattern = x.RegexPattern, ReplaceText = x.ReplaceText, Description = x.Description }).ToArray(),
                IsFilteringCommentTextEnabled = _commentFliteringRepository.IsFilteringCommentTextEnabled,
                CommentFilteringTextKeywordBackups = _commentFliteringRepository.GetAllFilteringCommentTextConditions().Select(x => new CommentFilteringTextKeywordBackup { ConditionRegex = x.Condition }).ToArray(),
            },
        };



        using Stream stream = await storageFile.OpenStreamForWriteAsync();
        stream.SetLength(0);
        await stream.FlushAsync();
        await JsonSerializer.SerializeAsync(stream, container, _options, ct);
        await stream.FlushAsync();
    }

    public async Task<BackupContainer> ReadBackupContainerAsync(StorageFile storageFile, CancellationToken ct = default)
    {
        using Windows.Storage.Streams.IRandomAccessStreamWithContentType stream = await storageFile.OpenReadAsync();
        using Stream jsonStream = stream.AsStreamForRead();
        return await JsonSerializer.DeserializeAsync<BackupContainer>(jsonStream, _options, ct);
    }

    public void RestoreLocalMylist(BackupContainer backup)
    {
        if (backup.LocalMylistItems == null) { return; }

        foreach (LocalMylistBackupEntry p in backup.LocalMylistItems)
        {
            PlaylistEntity playlist = null;
            if (p.Id != null)
            {
                playlist = _playlistRepository.GetPlaylist(p.Id);
            }

            playlist ??= new PlaylistEntity { Id = p.Id, Label = p.Label, PlaylistOrigin = PlaylistItemsSourceOrigin.Local };

            _playlistRepository.UpsertPlaylist(playlist);
            _ = _playlistRepository.AddItems(playlist.Id, p.VideoIdList.Select(x => (VideoId)x));
        }
    }

    public void RestoreSubscription(BackupContainer backup)
    {
        if (backup.SubscriptionGroupItems == null) { return; }

        Dictionary<SubscriptionGroupId, SubscriptionGroup> groupMaps 
            = _subscriptionGroupRepository.ReadAllItems().ToDictionary(x => x.GroupId);
        foreach (var groupBackup in backup.SubscriptionGroupItems)
        {
            if (groupBackup.GroupId == null) { continue; }

            var groupId = SubscriptionGroupId.Parse(groupBackup.GroupId);
            if (groupMaps.ContainsKey(groupId))
            {
                continue;
            }

            if (groupId == SubscriptionGroupId.DefaultGroupId)
            {
                continue;
            }

            var restoreGroup = new SubscriptionGroup(groupId, groupBackup.Label) { Order = groupBackup.Order };
            _subscriptionGroupRepository.CreateItem(restoreGroup);
            groupMaps.Add(restoreGroup.GroupId, restoreGroup);
        }

        if (backup.SubscriptionItems == null) { return; }

        List<Subscription> items = _subscriptionRegistrationRepository.ReadAllItems();
        foreach (SubscriptionBackupEntry s in backup.SubscriptionItems)
        {
            SubscriptionSourceType sourceType = s.SourceType switch
            {
                BackupSusbcriptionSourceType.User => SubscriptionSourceType.User,
                BackupSusbcriptionSourceType.Mylist => SubscriptionSourceType.Mylist,
                BackupSusbcriptionSourceType.Channel => SubscriptionSourceType.Channel,
                BackupSusbcriptionSourceType.Series => SubscriptionSourceType.Series,
                BackupSusbcriptionSourceType.SearchWithKeyword => SubscriptionSourceType.SearchWithKeyword,
                BackupSusbcriptionSourceType.SearchWithTag => SubscriptionSourceType.SearchWithTag,
                _ => throw new NotSupportedException()
            };

            // ObjectIdのパースが死んでるのでパラメータで同値判定してる
            if (items.Any(x => x.SourceType == sourceType && x.SourceParameter == s.SourceParameter)) { continue; }

            // GroupIdに該当アイテムが無い場合はグループ無指定のまま処理する
            SubscriptionGroup? group = null;
            if (s.GroupId != null)
            {
                groupMaps.TryGetValue(SubscriptionGroupId.Parse(s.GroupId), out group);
            }

            Subscription entity = new()
            {
                SubscriptionId = SusbcriptionId.NewObjectId(),
                Label = s.Label,
                SourceType = sourceType,
                SourceParameter = s.SourceParameter,
                IsEnabled = true,        
                Group = group,
                SortIndex = s.SortIndex ?? 0,
            };

            _ = _subscriptionRegistrationRepository.UpdateItem(entity);
        }

        // v1.5.4以前のバックアップデータにはSortIndexを含めていなかったので
        // 古いバックアップデータに備えて並び順を再指定しておく
        int index = 0;
        foreach (var source in _subscriptionRegistrationRepository.ReadAllItems().OrderBy(x => x.SortIndex).ThenBy(x => x.Label).ToArray())
        {
            source.SortIndex = index++;
            _subscriptionRegistrationRepository.UpdateItem(source);
        }
    }

    public void RestorePin(BackupContainer backup)
    {
        if (backup.PinItems == null) { return; }

        List<HohoemaPin> pins = _pinSettings.ReadAllItems();

        int index = 0;
        foreach (PinBackupEntry s in backup.PinItems)
        {
            if (!Enum.TryParse<PageNavigation.HohoemaPageType>(s.PageName, out HohoemaPageType pageType)) { continue; }

            if (pins.Any(x => x.PageType == pageType && x.Parameter == s.Parameter)) { continue; }

            _ = _pinSettings.CreateItem(new HohoemaPin
            {
                Id = s.Id,
                Label = s.Label,
                OverrideLabel = s.OverrideLabel,
                PageType = pageType,
                Parameter = s.Parameter,
                PinType = s.PinType switch { BackupBookmarkType.Item => BookmarkType.Item, BackupBookmarkType.Folder => BookmarkType.Folder, _ => throw new NotSupportedException() },
                SubItems = s.SubItems?.Select((x, i) => new HohoemaPin { Id = x.Id, Label = x.Label, OverrideLabel = x.OverrideLabel, PageType = Enum.Parse<HohoemaPageType>(x.PageName), Parameter = x.Parameter, SortIndex = i, PinType = BookmarkType.Item, SubItems = null }).ToList() ?? new List<HohoemaPin>(),
                SortIndex = index,
            });

            index++;
        }
    }

    [Obsolete]
    public void RestoreRankingSettings(BackupContainer backup)
    {
        if (backup.VideoRankingFiltering == null) { return; }

        foreach (string s in backup.VideoRankingFiltering.HiddenGenres)
        {
            if (!Enum.TryParse<RankingGenre>(s, out RankingGenre genre)) { continue; }

            _videoRankingSettings.AddHiddenGenre(genre);
        }

        foreach (RankingFilteringBackupGenreTag s in backup.VideoRankingFiltering.HiddenTags)
        {
            if (!Enum.TryParse<RankingGenre>(s.Genre, out RankingGenre genre)) { continue; }

            _videoRankingSettings.AddHiddenTag(genre, s.Tag, s.Label);
        }

        foreach (RankingFilteringBackupGenreTag s in backup.VideoRankingFiltering.FavoriteTags)
        {
            if (!Enum.TryParse<RankingGenre>(s.Genre, out RankingGenre genre)) { continue; }

            _videoRankingSettings.AddFavoriteTag(genre, s.Tag, s.Label);
        }
    }

    [Obsolete]
    public void RestoreVideoFilteringSettings(BackupContainer backup)
    {
        if (backup.VideoListringFiltering == null) { return; }

        VideoFilteringBackupEntry settings = backup.VideoListringFiltering;
        _videoFilteringSettings.NGVideoOwnerUserIdEnable = settings.IsEnabledFilteringOwners;
        foreach (IdAndLabel ownerId in settings.OnwerIds)
        {
            _videoFilteringSettings.AddHiddenVideoOwnerId(ownerId.Id, ownerId.Label);
        }

        _videoFilteringSettings.NGVideoIdEnable = settings.IsEnabledFilteringVideoIds;
        foreach (IdAndLabel videoId in settings.VideoIds)
        {
            _videoFilteringSettings.AddHiddenVideoId(videoId.Id, videoId.Label);
        }


        _videoFilteringSettings.NGVideoTitleKeywordEnable = settings.IsEnabledFilteringTitles;

        List<VideoTitleFilteringEntry> titles = _videoFilteringSettings.GetVideoTitleFilteringEntries();
        foreach (string title in settings.Titles)
        {
            if (titles.Any(x => x.Keyword == title)) { continue; }

            VideoTitleFilteringEntry entry = _videoFilteringSettings.CreateVideoTitleFiltering();
            entry.Keyword = title;
            _videoFilteringSettings.UpdateVideoTitleFiltering(entry);
        }
    }

    [Obsolete]
    public void RestoreAppearanceSettings(BackupContainer backup)
    {
        if (backup.AppearanceSettings == null) { return; }

        AppearanceSettingsBackupEntry restore = backup.AppearanceSettings;

        _appearanceSettings.Locale = restore.Locale;

        if (Enum.TryParse<HohoemaPageType>(restore.FirstAppearPageType, out HohoemaPageType pageType))
        {
            _appearanceSettings.FirstAppearPageType = pageType;
        }

        if (restore.OverrideInteractionMode != null
            && Enum.TryParse<ApplicationInteractionMode>(restore.OverrideInteractionMode, out ApplicationInteractionMode applicationInteractionMode))
        {
            _appearanceSettings.OverrideInteractionMode = applicationInteractionMode;
        }

        if (Enum.TryParse<Windows.UI.Xaml.ElementTheme>(restore.ApplicationTheme, out Windows.UI.Xaml.ElementTheme theme))
        {
            _appearanceSettings.ApplicationTheme = theme;
        }
    }

    [Obsolete]
    public void RestoreNicoRepoSettings(BackupContainer backup)
    {
        if (backup.NicoRepoSettings == null) { return; }

        if (backup.NicoRepoSettings.DisplayNicoRepoMuteContextTriggers?.Any() ?? false)
        {
            _nicoRepoSettings.DisplayNicoRepoMuteContextTriggers = backup.NicoRepoSettings.DisplayNicoRepoMuteContextTriggers.Select(x => Enum.TryParse<NicoRepoMuteContextTrigger>(x, out NicoRepoMuteContextTrigger type) ? type : default(NicoRepoMuteContextTrigger?)).Where(x => x != null).Select(x => x.Value).ToList();
        }
    }

    [Obsolete]
    public void RestorePlayerSettings(BackupContainer backup)
    {
        if (backup.PlayerSetting == null) { return; }

        PlayerSettingBackupEntry p = backup.PlayerSetting;

        if (Enum.TryParse<NicoVideoQuality>(p.DefaultQuality, out NicoVideoQuality videoQuality))
        {
            _playerSettings.DefaultVideoQuality = videoQuality;
        }

        if (Enum.TryParse<LiveQualityType>(p.DefaultLiveQuality, out LiveQualityType liveQuality))
        {
            _playerSettings.DefaultLiveQuality = liveQuality;
        }

        if (Enum.TryParse<LiveQualityLimitType>(p.LiveQualityLimit, out LiveQualityLimitType liveQualityLimit))
        {
            _playerSettings.LiveQualityLimit = liveQualityLimit;
        }

        _playerSettings.LiveWatchWithLowLatency = p.LiveWatchWithLowLatency;

        _playerSettings.IsMute = p.IsMute;
        _playerSettings.SoundVolume = p.SoundVolume;
        _playerSettings.SoundVolumeChangeFrequency = p.SoundVolumeChangeFrequency;
        _playerSettings.IsLoudnessCorrectionEnabled = p.IsLoudnessCorrectionEnabled;
        _playerSettings.IsCommentDisplay_Video = p.IsCommentDisplay_Video;
        _playerSettings.IsCommentDisplay_Live = p.IsCommentDisplay_Live;
        _playerSettings.CommentDisplayDuration = TimeSpan.FromMilliseconds(p.CommentDisplayDurationInMS);
        _playerSettings.DefaultCommentFontScale = p.DefaultCommentFontScale;
        _playerSettings.CommentOpacity = p.CommentOpacity;
        _playerSettings.IsDefaultCommentWithAnonymous = p.IsDefaultCommentWithAnonymous;
        _playerSettings.CommentColor = p.CommentColor;
        _playerSettings.IsAutoHidePlayerControlUI = p.IsAutoHidePlayerControlUI;
        _playerSettings.AutoHidePlayerControlUIPreventTime = TimeSpan.FromMilliseconds(p.AutoHidePlayerControlUIPreventTimeInMS);
        _playerSettings.IsForceLandscape = p.IsForceLandscape;
        _playerSettings.PlaybackRate = p.PlaybackRate;
        _playerSettings.NicoScript_DisallowSeek_Enabled = p.NicoScript_DisallowSeek_Enabled;
        _playerSettings.NicoScript_Default_Enabled = p.NicoScript_Default_Enabled;
        _playerSettings.NicoScript_Jump_Enabled = p.NicoScript_Jump_Enabled;
        _playerSettings.NicoScript_DisallowComment_Enabled = p.NicoScript_DisallowComment_Enabled;
        _playerSettings.NicoScript_Replace_Enabled = p.NicoScript_Replace_Enabled;
        _playerSettings.IsCurrentVideoLoopingEnabled = p.IsCurrentVideoLoopingEnabled;
        _playerSettings.IsPlaylistLoopingEnabled = p.IsPlaylistLoopingEnabled;
        _playerSettings.IsShuffleEnable = p.IsShuffleEnable;
        _playerSettings.IsReverseModeEnable = p.IsReverseModeEnable;

        if (Enum.TryParse<PlaylistEndAction>(p.PlaylistEndAction, out PlaylistEndAction playlistEndAction))
        {
            _playerSettings.PlaylistEndAction = playlistEndAction;
        }

        _playerSettings.AutoMoveNextVideoOnPlaylistEmpty = p.AutoMoveNextVideoOnPlaylistEmpty;
    }

    [Obsolete]
    public void RestoreCommentSettings(BackupContainer backup)
    {
        if (backup.CommentSettingsBackupEntry == null) { return; }

        CommentSettingsBackupEntry s = backup.CommentSettingsBackupEntry;
        _commentFliteringRepository.ShareNGScore = s.ShareNGScore;
        _commentFliteringRepository.IsFilteringCommentOwnerIdEnabled = s.IsFilteringCommentOwnerIdEnabled;
        _commentFliteringRepository.IsFilteringCommentTextEnabled = s.IsFilteringCommentTextEnabled;

        if (s.CommentFilteringOwnerIdBackups?.Any() ?? false)
        {
            List<CommentFliteringRepository.FilteringCommentOwnerId> ownerIds = _commentFliteringRepository.GetAllFilteringCommenOwnerId();
            foreach (CommentFilteringOwnerIdBackup ownerId in s.CommentFilteringOwnerIdBackups)
            {
                if (ownerIds.Any(x => x.UserId == ownerId.UserId)) { continue; }

                _commentFliteringRepository.AddFilteringCommenOwnerId(ownerId.UserId, string.Empty);
            }
        }

        if (s.CommentTextTransformConditionBackups?.Any() ?? false)
        {
            List<CommentFliteringRepository.CommentTextTransformCondition> transforms = _commentFliteringRepository.GetAllCommentTextTransformCondition();
            foreach (CommentTextTransformConditionBackup transform in s.CommentTextTransformConditionBackups)
            {
                if (transforms.Any(x => x.RegexPattern == transform.RegexPattern)) { continue; }

                CommentFliteringRepository.CommentTextTransformCondition t = _commentFliteringRepository.AddCommentTextTransformCondition(transform.RegexPattern, transform.ReplaceText, transform.Description);
                t.IsEnabled = transform.IsEnabled;
                _commentFliteringRepository.UpdateCommentTextTransformCondition(t);
            }
        }

        if (s.CommentFilteringTextKeywordBackups?.Any() ?? false)
        {
            List<CommentFliteringRepository.FilteringCommentTextKeyword> keywords = _commentFliteringRepository.GetAllFilteringCommentTextConditions();
            foreach (CommentFilteringTextKeywordBackup filter in s.CommentFilteringTextKeywordBackups)
            {
                if (keywords.Any(x => x.Condition == filter.ConditionRegex)) { continue; }

                _ = _commentFliteringRepository.AddFilteringCommentText(filter.ConditionRegex);
            }
        }
    }
}

public sealed class BackupContainer
{
    [JsonPropertyName("localMylistItems")]
    public LocalMylistBackupEntry[] LocalMylistItems { get; set; }

    [JsonPropertyName("subscriptions")]
    public SubscriptionBackupEntry[] SubscriptionItems { get; set; }
    
    [JsonPropertyName("subscriptionGroups")]
    public SubscriptionGroupBackupEntry[] SubscriptionGroupItems { get; set; }

    [JsonPropertyName("pins")]
    public PinBackupEntry[] PinItems { get; set; }

    [JsonPropertyName("videoRanking")]
    public RankingFilteringBackupEntry VideoRankingFiltering { get; set; }

    [JsonPropertyName("videoFiltering")]
    public VideoFilteringBackupEntry VideoListringFiltering { get; set; }

    [JsonPropertyName("appearance")]
    public AppearanceSettingsBackupEntry AppearanceSettings { get; set; }

    [JsonPropertyName("nicoRepo")]
    public NicoRepoSettingsBackupEntry NicoRepoSettings { get; set; }

    [JsonPropertyName("player")]
    public PlayerSettingBackupEntry PlayerSetting { get; set; }

    [JsonPropertyName("comment")]
    public CommentSettingsBackupEntry CommentSettingsBackupEntry { get; set; }

}


public sealed class LocalMylistBackupEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonPropertyName("data")]
    public string[] VideoIdList { get; set; }
}

public sealed class SubscriptionBackupEntry
{
    [JsonPropertyName("id")]
    public ObjectId Id { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonPropertyName("type")]
    public BackupSusbcriptionSourceType SourceType { get; set; }

    [JsonPropertyName("parameter")]
    public string SourceParameter { get; set; }

    [JsonPropertyName("groupId")]
    public string? GroupId { get; set; }

    [JsonPropertyName("sortIndex")]
    public int? SortIndex { get; set; } 
}

public sealed class SubscriptionGroupBackupEntry
{
    [JsonPropertyName("groupId")]
    public string GroupId { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }
}

public enum BackupSusbcriptionSourceType
{
    User,
    Mylist,
    Channel,
    Series,
    SearchWithKeyword,
    SearchWithTag,
}

public enum BackupBookmarkType
{
    Item,
    Folder,
}

public sealed class PinBackupEntry
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonPropertyName("overrideLabel")]
    public string OverrideLabel { get; set; }

    [JsonPropertyName("pageName")]
    public string PageName { get; set; }

    [JsonPropertyName("parameter")]
    public string Parameter { get; set; }

    [JsonPropertyName("subItems")]
    public List<PinBackupEntry> SubItems { get; set; }

    [JsonPropertyName("pinType")]
    public BackupBookmarkType PinType { get; set; }
}


public sealed class RankingFilteringBackupEntry
{
    [JsonPropertyName("hiddenGenres")]
    public string[] HiddenGenres { get; set; }

    [JsonPropertyName("hiddenTags")]
    public RankingFilteringBackupGenreTag[] HiddenTags { get; set; }

    [JsonPropertyName("FavoriteGenres")]
    public RankingFilteringBackupGenreTag[] FavoriteTags { get; set; }
}


public sealed class RankingFilteringBackupGenreTag
{
    [JsonPropertyName("label")]
    public string Label { get; set; }
    [JsonPropertyName("genre")]
    public string Genre { get; set; }
    [JsonPropertyName("tag")]
    public string Tag { get; set; }
}





public sealed class VideoFilteringBackupEntry
{
    [JsonPropertyName("filteringOwner_IsEnabled")]
    public bool IsEnabledFilteringOwners { get; set; }

    [JsonPropertyName("filteringOnwers")]
    public IdAndLabel[] OnwerIds { get; set; }

    [JsonPropertyName("filteringVideoId_IsEnabled")]
    public bool IsEnabledFilteringVideoIds { get; set; }

    [JsonPropertyName("filteringVideos")]
    public IdAndLabel[] VideoIds { get; set; }

    [JsonPropertyName("filteringVideoTitles_IsEnabled")]
    public bool IsEnabledFilteringTitles { get; set; }

    [JsonPropertyName("filteringVideoTitles")]
    public string[] Titles { get; set; }
}

public sealed class IdAndLabel
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; }
}


public sealed class AppearanceSettingsBackupEntry
{
    [JsonPropertyName("locale")]
    public string Locale { get; set; }
    [JsonPropertyName("firstAppearPageType")]
    public string FirstAppearPageType { get; set; }
    [JsonPropertyName("overrideInteractionMode")]
    public string OverrideInteractionMode { get; set; }
    [JsonPropertyName("applicationTheme")]
    public string ApplicationTheme { get; set; }
}


public sealed class NicoRepoSettingsBackupEntry
{
    [JsonPropertyName("displayNicoRepoMuteContextTriggers")]
    public string[] DisplayNicoRepoMuteContextTriggers { get; set; }
}


public sealed class PlayerSettingBackupEntry
{
    [JsonPropertyName("defaultQuality")]
    public string DefaultQuality { get; set; }

    [JsonPropertyName("defaultLiveQuality")]
    public string DefaultLiveQuality { get; set; }
    [JsonPropertyName("liveQualityLimit")]
    public string LiveQualityLimit { get; set; }
    [JsonPropertyName("liveWatchWithLowLatency")]
    public bool LiveWatchWithLowLatency { get; set; }


    #region Sound
    [JsonPropertyName("isMute")]
    public bool IsMute { get; set; }
    [JsonPropertyName("soundVolume")]
    public double SoundVolume { get; set; }



    [JsonPropertyName("soundVolumeChangeFrequency")]
    public double SoundVolumeChangeFrequency { get; set; }
    [JsonPropertyName("isLoudnessCorrectionEnabled")]
    public bool IsLoudnessCorrectionEnabled { get; set; }

    #endregion

    [JsonPropertyName("isCommentDisplay_Video")]
    public bool IsCommentDisplay_Video { get; set; }
    [JsonPropertyName("isCommentDisplay_Live")]
    public bool IsCommentDisplay_Live { get; set; }
    [JsonPropertyName("pauseWithCommentWriting")]
    public bool PauseWithCommentWriting { get; set; }
    [JsonPropertyName("commentDisplayDuration")]
    public double CommentDisplayDurationInMS { get; set; }
    [JsonPropertyName("defaultCommentFontScale")]
    public double DefaultCommentFontScale { get; set; }
    [JsonPropertyName("commentOpacity")]
    public double CommentOpacity { get; set; }



    [JsonPropertyName("isDefaultCommentWithAnonymous")]
    public bool IsDefaultCommentWithAnonymous { get; set; }
    [JsonPropertyName("commentColor")]
    public Color CommentColor { get; set; }

    [JsonPropertyName("isAutoHidePlayerControlUI")]
    public bool IsAutoHidePlayerControlUI { get; set; }
    [JsonPropertyName("autoHidePlayerControlUIPreventTime")]
    public double AutoHidePlayerControlUIPreventTimeInMS { get; set; }
    [JsonPropertyName("isForceLandscape")]
    public bool IsForceLandscape { get; set; }
    [JsonPropertyName("playbackRate")]
    public double PlaybackRate { get; set; }

    [JsonPropertyName("nicoScript_DisallowSeek_Enabled")]
    public bool NicoScript_DisallowSeek_Enabled { get; set; }
    [JsonPropertyName("nicoScript_Default_Enabled")]
    public bool NicoScript_Default_Enabled { get; set; }
    [JsonPropertyName("nicoScript_Jump_Enabled")]
    public bool NicoScript_Jump_Enabled { get; set; }
    [JsonPropertyName("nicoScript_DisallowComment_Enabled")]
    public bool NicoScript_DisallowComment_Enabled { get; set; }
    [JsonPropertyName("nicoScript_Replace_Enabled")]
    public bool NicoScript_Replace_Enabled { get; set; }




    #region Playlist

    [JsonPropertyName("isCurrentVideoLoopingEnabled")]
    public bool IsCurrentVideoLoopingEnabled { get; set; }
    [JsonPropertyName("isPlaylistLoopingEnabled")]
    public bool IsPlaylistLoopingEnabled { get; set; }
    [JsonPropertyName("isShuffleEnable")]
    public bool IsShuffleEnable { get; set; }

    [JsonPropertyName("isReverseModeEnable")]
    public bool IsReverseModeEnable { get; set; }

    [JsonPropertyName("playlistEndAction")]
    public string PlaylistEndAction { get; set; }

    [JsonPropertyName("autoMoveNextVideoOnPlaylistEmpty")]
    public bool AutoMoveNextVideoOnPlaylistEmpty { get; set; }
    #endregion
}


public sealed class CommentSettingsBackupEntry
{
    [JsonPropertyName("shareNGScore")]
    public int ShareNGScore { get; set; }

    [JsonPropertyName("isFilteringCommentOwnerIdEnabled")]
    public bool IsFilteringCommentOwnerIdEnabled { get; set; }

    [JsonPropertyName("commentFilteringOwnerIds")]
    public CommentFilteringOwnerIdBackup[] CommentFilteringOwnerIdBackups { get; set; }

    [JsonPropertyName("commentTextTransformConditions")]
    public CommentTextTransformConditionBackup[] CommentTextTransformConditionBackups { get; set; }

    [JsonPropertyName("isFilteringCommentTextEnabled")]
    public bool IsFilteringCommentTextEnabled { get; set; }
    [JsonPropertyName("commentFilteringTextKeywords")]
    public CommentFilteringTextKeywordBackup[] CommentFilteringTextKeywordBackups { get; set; }

}


public sealed class CommentTextTransformConditionBackup
{
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; }
    [JsonPropertyName("regexPattern")]
    public string RegexPattern { get; set; }
    [JsonPropertyName("replaceText")]
    public string ReplaceText { get; set; }
    [JsonPropertyName("description")]
    public string Description { get; set; }
}

public sealed class CommentFilteringOwnerIdBackup
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; }
}

public sealed class CommentFilteringTextKeywordBackup
{
    [JsonPropertyName("conditionRegex")]
    public string ConditionRegex { get; set; }
}

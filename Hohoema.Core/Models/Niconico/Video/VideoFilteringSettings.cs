#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Hohoema.Infra;
using LiteDB;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Hohoema.Models.Niconico.Video;


public class FilteredResult
{
    public FilteredReason FilteredReason { get; set; }
    public string FilterCondition { get; set; }
}

public enum FilteredReason
{
    VideoId,
    UserId,
    Keyword,
    Tag,
}

public sealed class VideoOwnerFilteringAddedMessage : ValueChangedMessage<VideoOwnerFilteringAddedMessagePayload>
{
    public VideoOwnerFilteringAddedMessage(VideoOwnerFilteringAddedMessagePayload value) : base(value)
    {
    }
}

public sealed class VideoOwnerFilteringRemovedMessage : ValueChangedMessage<VideoOwnerFilteringRemovedMessagePayload>
{
    public VideoOwnerFilteringRemovedMessage(VideoOwnerFilteringRemovedMessagePayload value) : base(value)
    {
    }
}

public sealed class VideoOwnerFilteringAddedMessagePayload
{
    public string OwnerId { get; set; }
}

public sealed class VideoOwnerFilteringRemovedMessagePayload
{
    public string OwnerId { get; set; }
}

public sealed class VideoFilteringSettings : FlagsRepositoryBase
{
    [System.Obsolete]
    public VideoFilteringSettings(
        IMessenger messenger,
        VideoIdFilteringRepository videoIdFilteringRepository,
        VideoOwnerIdFilteringRepository videoOwnerIdFilteringRepository,
        VideoTitleFilteringRepository videoTitleFilteringRepository
        )
    {
        _NGVideoIdEnable = Read(true, nameof(NGVideoIdEnable));
        _NGVideoOwnerUserIdEnable = Read(true, nameof(NGVideoOwnerUserIdEnable));
        _NGVideoTitleKeywordEnable = Read(true, nameof(NGVideoTitleKeywordEnable));
        _NGVideoTitleTestText = Read(string.Empty, nameof(NGVideoTitleTestText));
        _messenger = messenger;
        _videoIdFilteringRepository = videoIdFilteringRepository;
        _videoOwnerIdFilteringRepository = videoOwnerIdFilteringRepository;
        _videoTitleFilteringRepository = videoTitleFilteringRepository;
    }


    public bool TryGetHiddenReason(IVideoContent video, out FilteredResult result)
    {
        if (TryGetFilteredResultVideoId(video.VideoId, out VideoIdFilteringEntry condi))
        {
            result = new FilteredResult()
            {
                FilteredReason = FilteredReason.VideoId,
                FilterCondition = condi.Description,
            };

            return true;
        }
        else if (video is IVideoContentProvider provider && provider.ProviderId != null && TryGetFilteredResultVideoOwnerId(provider.ProviderId, out VideoOwnerIdFilteringEntry user))
        {
            result = new FilteredResult()
            {
                FilteredReason = FilteredReason.UserId,
                FilterCondition = user.Description,
            };

            return true;
        }
        else if (video.Title != null && TryGetHiddenVideoTitle(video.Title, out VideoTitleFilteringEntry title))
        {
            result = new FilteredResult()
            {
                FilteredReason = FilteredReason.Keyword,
                FilterCondition = title.Keyword,
            };

            return true;
        }

        result = null;
        return false;
    }


    #region Video Id Filtering

    private bool _NGVideoIdEnable;

    [System.Obsolete]
    public bool NGVideoIdEnable
    {
        get => _NGVideoIdEnable;
        set => SetProperty(ref _NGVideoIdEnable, value);
    }

    private readonly IMessenger _messenger;
    private readonly VideoIdFilteringRepository _videoIdFilteringRepository;


    public List<VideoIdFilteringEntry> GetVideoIdFilteringEntries()
    {
        return _videoIdFilteringRepository.ReadAllItems();
    }

    public bool IsHiddenVideoId(string id)
    {
        return _videoIdFilteringRepository.Exists(x => x.VideoId == id);
    }

    public void AddHiddenVideoId(string id, string label)
    {
        _ = _videoIdFilteringRepository.UpdateItem(new VideoIdFilteringEntry() { VideoId = id, Description = label });
    }

    public void RemoveHiddenVideoId(string id)
    {
        _ = _videoIdFilteringRepository.DeleteItem(id);
    }

    public bool TryGetFilteredResultVideoId(string id, out VideoIdFilteringEntry outEntry)
    {
        outEntry = _videoIdFilteringRepository.FindById(id);
        return outEntry != null;
    }

    public sealed class VideoIdFilteringRepository : LiteDBServiceBase<VideoIdFilteringEntry>
    {
        public VideoIdFilteringRepository(LiteDatabase liteDatabase) : base(liteDatabase)
        {
        }
    }


    #endregion

    #region Video Owner Id Filtering

    private bool _NGVideoOwnerUserIdEnable;

    [System.Obsolete]
    public bool NGVideoOwnerUserIdEnable
    {
        get => _NGVideoOwnerUserIdEnable;
        set => SetProperty(ref _NGVideoOwnerUserIdEnable, value);
    }

    private readonly VideoOwnerIdFilteringRepository _videoOwnerIdFilteringRepository;

    public List<VideoOwnerIdFilteringEntry> GetVideoOwnerIdFilteringEntries()
    {
        return _videoOwnerIdFilteringRepository.ReadAllItems();
    }

    public bool IsHiddenVideoOwnerId(string id)
    {
        return _videoOwnerIdFilteringRepository.Exists(x => x.UserId == id);
    }

    public void AddHiddenVideoOwnerId(string id, string label)
    {
        _ = _videoOwnerIdFilteringRepository.UpdateItem(new VideoOwnerIdFilteringEntry() { UserId = id, Description = label });

        VideoOwnerFilteringAddedMessage message = new(new() { OwnerId = id });
        _ = _messenger.Send(message);
        _ = _messenger.Send(message, id);
    }

    public void RemoveHiddenVideoOwnerId(string id)
    {
        _ = _videoOwnerIdFilteringRepository.DeleteItem(id);

        VideoOwnerFilteringRemovedMessage message = new(new() { OwnerId = id });
        _ = _messenger.Send(message);
        _ = _messenger.Send(message, id);
    }

    public bool TryGetFilteredResultVideoOwnerId(string id, out VideoOwnerIdFilteringEntry outEntry)
    {
        outEntry = _videoOwnerIdFilteringRepository.FindById(id);
        return outEntry != null;
    }


    public sealed class VideoOwnerIdFilteringRepository : LiteDBServiceBase<VideoOwnerIdFilteringEntry>
    {
        public VideoOwnerIdFilteringRepository(LiteDatabase liteDatabase) : base(liteDatabase)
        {
        }


    }

    #endregion


    #region Video Title Filtering

    private bool _NGVideoTitleKeywordEnable;

    [System.Obsolete]
    public bool NGVideoTitleKeywordEnable
    {
        get => _NGVideoTitleKeywordEnable;
        set => SetProperty(ref _NGVideoTitleKeywordEnable, value);
    }


    private string _NGVideoTitleTestText;

    [System.Obsolete]
    public string NGVideoTitleTestText
    {
        get => _NGVideoTitleTestText;
        set => SetProperty(ref _NGVideoTitleTestText, value);
    }



    private readonly VideoTitleFilteringRepository _videoTitleFilteringRepository;

    public List<VideoTitleFilteringEntry> GetVideoTitleFilteringEntries()
    {
        return _cacheTitleFilteringEntry ??= _videoTitleFilteringRepository.ReadAllItems();
    }

    public VideoTitleFilteringEntry CreateVideoTitleFiltering()
    {
        _cacheTitleFilteringEntry ??= _videoTitleFilteringRepository.ReadAllItems();
        VideoTitleFilteringEntry entry = new();
        _ = _videoTitleFilteringRepository.CreateItem(entry);
        _cacheTitleFilteringEntry.Add(entry);
        return entry;
    }

    public void UpdateVideoTitleFiltering(VideoTitleFilteringEntry entry)
    {
        _ = _videoTitleFilteringRepository.UpdateItem(entry);
    }

    public void RemoveVideoTitleFiltering(VideoTitleFilteringEntry entry)
    {
        _ = _videoTitleFilteringRepository.DeleteItem(entry.Id);
        _cacheTitleFilteringEntry ??= _videoTitleFilteringRepository.ReadAllItems();
        VideoTitleFilteringEntry removeTarget = _cacheTitleFilteringEntry.FirstOrDefault(x => x.Id == entry.Id);
        _ = _cacheTitleFilteringEntry.Remove(removeTarget);
    }

    private List<VideoTitleFilteringEntry> _cacheTitleFilteringEntry = null;
    public bool IsHiddenVideoTitle(string title)
    {
        return TryGetHiddenVideoTitle(title, out _);
    }

    public bool TryGetHiddenVideoTitle(string title, out VideoTitleFilteringEntry outEntry)
    {
        _cacheTitleFilteringEntry ??= _videoTitleFilteringRepository.ReadAllItems();

        outEntry = _cacheTitleFilteringEntry.FirstOrDefault(x => x.CheckNG(title));
        return outEntry != null;
    }

    public sealed class VideoTitleFilteringRepository : LiteDBServiceBase<VideoTitleFilteringEntry>
    {
        public VideoTitleFilteringRepository(LiteDatabase liteDatabase) : base(liteDatabase)
        {
            _ = _collection.EnsureIndex(x => x.Keyword);
        }
    }


    #endregion
}


public class VideoIdFilteringEntry
{
    [BsonId]
    public string VideoId { get; set; }
    public string Description { get; set; }
}

public class VideoOwnerIdFilteringEntry
{
    [BsonId]
    public string UserId { get; set; }
    public string Description { get; set; }
}

public class VideoTitleFilteringEntry
{
    [BsonId(autoId: true)]
    public int Id { get; set; }

    [BsonIgnore]
    private string _Keyword;

    [BsonField]
    public string Keyword
    {
        get => _Keyword;
        set
        {
            if (_Keyword != value)
            {
                _Keyword = value;
                _Regex = null;
            }
        }
    }

    [BsonIgnore]
    private Regex _Regex;

    public bool CheckNG(string target)
    {
        if (Keyword == null) { return false; }

        if (_Regex == null)
        {
            try
            {
                _Regex = new Regex(Keyword);
            }
            catch { }
        }

        return _Regex?.IsMatch(target) ?? false;
    }
}

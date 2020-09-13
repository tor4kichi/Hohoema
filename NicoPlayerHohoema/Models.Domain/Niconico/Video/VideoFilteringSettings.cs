using Hohoema.Models.Infrastructure;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Video
{

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


    public sealed class VideoOwnerFilteringAddedEventArgs
    {
        public string OwnerId { get; set; }
    }

    public sealed class VideoOwnerFilteringRemovedEventArgs
    {
        public string OwnerId { get; set; }
    }

    public sealed class VideoFilteringSettings : FlagsRepositoryBase
    {     
        public VideoFilteringSettings(
            VideoIdFilteringRepository videoIdFilteringRepository,
            VideoOwnerIdFilteringRepository videoOwnerIdFilteringRepository,
            VideoTitleFilteringRepository videoTitleFilteringRepository
            )
        {
            _NGVideoIdEnable = Read(true, nameof(NGVideoIdEnable));
            _NGVideoOwnerUserIdEnable = Read(true, nameof(NGVideoOwnerUserIdEnable));
            _NGVideoTitleKeywordEnable = Read(true, nameof(NGVideoTitleKeywordEnable));
            _NGVideoTitleTestText = Read(string.Empty, nameof(NGVideoTitleTestText));
            _videoIdFilteringRepository = videoIdFilteringRepository;
            _videoOwnerIdFilteringRepository = videoOwnerIdFilteringRepository;
            _videoTitleFilteringRepository = videoTitleFilteringRepository;
        }

        public event EventHandler<VideoOwnerFilteringAddedEventArgs> VideoOwnerFilterAdded;
        public event EventHandler<VideoOwnerFilteringRemovedEventArgs> VideoOwnerFilterRemoved;

        public bool TryGetHiddenReason(IVideoContent video, out FilteredResult result)
        {
            if (TryGetFilteredResultVideoId(video.Id, out var condi))
            {
                result = new FilteredResult()
                {
                    FilteredReason = FilteredReason.VideoId,
                    FilterCondition = condi.Description,
                };

                return true;
            }
            else if (video.ProviderId != null && TryGetFilteredResultVideoOwnerId(video.ProviderId, out var user))
            {
                result = new FilteredResult()
                {
                    FilteredReason = FilteredReason.UserId,
                    FilterCondition = user.Description,
                };

                return true;
            }
            else if (video.Label != null && TryGetHiddenVideoTitle(video.Label, out var title))
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
        public bool NGVideoIdEnable
        {
            get { return _NGVideoIdEnable; }
            set { SetProperty(ref _NGVideoIdEnable, value); }
        }

        private readonly VideoIdFilteringRepository _videoIdFilteringRepository;

        public bool IsHiddenVideoId(string id)
        {
            return _videoIdFilteringRepository.Exists(x => x.VideoId == id);
        }

        public void AddHiddenVideoId(string id, string label)
        {
            _videoIdFilteringRepository.UpdateItem(new VideoIdFilteringEntry() { VideoId = id, Description = label });
        }

        public void RemoveHiddenVideoId(string id)
        {
            _videoIdFilteringRepository.DeleteItem(id);
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
        public bool NGVideoOwnerUserIdEnable
        {
            get { return _NGVideoOwnerUserIdEnable; }
            set { SetProperty(ref _NGVideoOwnerUserIdEnable, value); }
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
            _videoOwnerIdFilteringRepository.UpdateItem(new VideoOwnerIdFilteringEntry() { UserId = id, Description = label });
            VideoOwnerFilterAdded?.Invoke(this, new VideoOwnerFilteringAddedEventArgs() { OwnerId = id });
        }

        public void RemoveHiddenVideoOwnerId(string id)
        {
            _videoOwnerIdFilteringRepository.DeleteItem(id);
            VideoOwnerFilterRemoved?.Invoke(this, new VideoOwnerFilteringRemovedEventArgs() { OwnerId = id });
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
        public bool NGVideoTitleKeywordEnable
        {
            get { return _NGVideoTitleKeywordEnable; }
            set { SetProperty(ref _NGVideoTitleKeywordEnable, value); }
        }


        private string _NGVideoTitleTestText;
        public string NGVideoTitleTestText
        {
            get { return _NGVideoTitleTestText; }
            set { SetProperty(ref _NGVideoTitleTestText, value); }
        }



        private readonly VideoTitleFilteringRepository _videoTitleFilteringRepository;

        public List<VideoTitleFilteringEntry> GetVideoTitleFilteringEntries()
        {
            return _cacheTitleFilteringEntry ??= _videoTitleFilteringRepository.ReadAllItems();
        }

        public VideoTitleFilteringEntry CreateVideoTitleFiltering()
        {
            _cacheTitleFilteringEntry ??= _videoTitleFilteringRepository.ReadAllItems();
            var entry = _videoTitleFilteringRepository.CreateItem(new VideoTitleFilteringEntry());
            _cacheTitleFilteringEntry.Add(entry);
            return entry;
        }

        public void UpdateVideoTitleFiltering(VideoTitleFilteringEntry entry)
        {
            _videoTitleFilteringRepository.UpdateItem(entry);
        }

        public void RemoveVideoTitleFiltering(VideoTitleFilteringEntry entry)
        {
            _videoTitleFilteringRepository.DeleteItem(entry.Id);
            _cacheTitleFilteringEntry ??= _videoTitleFilteringRepository.ReadAllItems();
            var removeTarget = _cacheTitleFilteringEntry.FirstOrDefault(x => x.Id == entry.Id);
            _cacheTitleFilteringEntry.Remove(removeTarget);
        }

        List<VideoTitleFilteringEntry> _cacheTitleFilteringEntry = null;
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
                _collection.EnsureIndex(x => x.Keyword);
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
        [BsonId(autoId:true)]
        public int Id { get; set; }

        [BsonIgnore]
        private string _Keyword;
        
        [BsonField]
        public string Keyword
        {
            get { return _Keyword; }
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
}

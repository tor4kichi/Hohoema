using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Infrastructure;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Subscriptions
{
    public sealed  class SubscFeedVideo
    {
        [BsonId(autoId:true)]
        public ObjectId Id { get; init; }

        public ObjectId SourceSubscId { get; init; }
        public string VideoId { get; init; }
        public DateTime PostAt { get; init; }
        public string Title { get; init; }

        public DateTime FeedUpdateAt { get; set; }
        public bool IsChecked { get; set; }
    }

    public sealed class SubscFeedVideoRepository
    {
        class SubscFeedVideoRepository_Internal : LiteDBServiceBase<SubscFeedVideo>
        {
            public SubscFeedVideoRepository_Internal(LiteDatabase liteDatabase) : base(liteDatabase)
            {
                _collection.EnsureIndex(x => x.SourceSubscId);
                _collection.EnsureIndex(x => x.PostAt);
                _collection.EnsureIndex(x => x.VideoId);
                _collection.EnsureIndex(x => x.IsChecked);
                _collection.EnsureIndex(x => x.FeedUpdateAt);
            }

            public DateTime GetLatestPostAt(ObjectId subscId)
            {
                return _collection.Find(x => x.SourceSubscId == subscId).Max(x => x.PostAt);
            }
        }

        private readonly SubscFeedVideoRepository_Internal _subscFeedVideoRepository;

        public SubscFeedVideoRepository(LiteDatabase liteDatabase)
        {
            _subscFeedVideoRepository = new SubscFeedVideoRepository_Internal(liteDatabase);
        }


        public bool DeleteSubsc(SubscriptionSourceEntity source)
        {
            return _subscFeedVideoRepository.DeleteMany(x => x.SourceSubscId == source.Id);
        }

        public IEnumerable<SubscFeedVideo> GetVideo(ObjectId subscId, int skip = 0, int limit = int.MaxValue)
        {
            return _subscFeedVideoRepository.Find(Query.All(nameof(SubscFeedVideo.PostAt), Query.Descending)).Where(x => x.SourceSubscId == subscId).Skip(skip).Take(limit);
        }


        public IEnumerable<SubscFeedVideo> GetVideos(int skip = 0, int limit = int.MaxValue)
        {
            return _subscFeedVideoRepository.Find(Query.All(nameof(SubscFeedVideo.PostAt), Query.Descending), skip, limit);
        }

        public IEnumerable<SubscFeedVideo> GetUncheckedVideos(int skip = 0, int limit = int.MaxValue)
        {
            return _subscFeedVideoRepository.Find(x => x.IsChecked == false).OrderByDescending(x => x.PostAt).Skip(skip).Take(limit);
        }

        public DateTime GetLatestTimeOnSubscVideo(ObjectId subscId)
        {
            return _subscFeedVideoRepository.GetLatestPostAt(subscId);
        }

        public void UpdateVideos(IEnumerable<SubscFeedVideo> videos)
        {
            _subscFeedVideoRepository.UpdateItem(videos);
        }

        public IEnumerable<NicoVideo> RegisteringVideosIfNotExist(ObjectId subscId, DateTime updateAt, IEnumerable<NicoVideo> videos)
        {
            foreach (var video in videos)
            {
                string videoId = video.VideoId;
                if (_subscFeedVideoRepository.Exists(x => x.SourceSubscId == subscId && x.VideoId == videoId) is false)
                {
                    _subscFeedVideoRepository.CreateItem(new SubscFeedVideo 
                    {
                        SourceSubscId = subscId,
                        Id = ObjectId.NewObjectId(),
                        VideoId = video.VideoId,
                        PostAt = video.PostedAt,
                        Title = video.Title,
                        FeedUpdateAt = updateAt,                        
                    });

                    yield return video;
                }
            }
        }

        public IEnumerable<SubscFeedVideo> RegisteringVideosIfNotExist(ObjectId subscId, DateTime updateAt, IEnumerable<SubscFeedVideo> videos)
        {
            foreach (var video in videos)
            {
                string videoId = video.VideoId;
                if (_subscFeedVideoRepository.Exists(x => x.SourceSubscId == subscId && x.VideoId == videoId) is false)
                {
                    video.FeedUpdateAt = updateAt;
                    _subscFeedVideoRepository.CreateItem(video);

                    yield return video;
                }
            }
        }
    }
}

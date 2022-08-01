using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
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
    }


    public sealed class SubscFeedVideoValueChangedMessage : ValueChangedMessage<SubscFeedVideo>
    {
        public SubscFeedVideoValueChangedMessage(SubscFeedVideo value) : base(value)
        {
            
        }
    }

    public sealed class NewSubscFeedVideoMessage : ValueChangedMessage<SubscFeedVideo>
    {
        public NewSubscFeedVideoMessage(SubscFeedVideo value) : base(value)
        {

        }
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
                _collection.EnsureIndex(x => x.FeedUpdateAt);
            }

            public DateTime GetLatestPostAt(ObjectId subscId)
            {
                return _collection.Find(x => x.SourceSubscId == subscId).Max(x => x.PostAt);
            }
        }

        private readonly SubscFeedVideoRepository_Internal _subscFeedVideoRepository;
        private readonly IMessenger _messenger;

        public SubscFeedVideoRepository(LiteDatabase liteDatabase,
            IMessenger messenger
            )
        {
            _subscFeedVideoRepository = new SubscFeedVideoRepository_Internal(liteDatabase);
            _messenger = messenger;
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

        public DateTime GetLatestTimeOnSubscVideo(ObjectId subscId)
        {
            return _subscFeedVideoRepository.GetLatestPostAt(subscId);
        }

        public void UpdateVideos(IEnumerable<SubscFeedVideo> videos)
        {
            _subscFeedVideoRepository.UpdateItem(videos);
            foreach (var video in videos)
            {
                _messenger.Send(new SubscFeedVideoValueChangedMessage(video));
            }            
        }

        public IEnumerable<NicoVideo> RegisteringVideosIfNotExist(ObjectId subscId, DateTime updateAt, IEnumerable<NicoVideo> videos)
        {
            foreach (var video in videos)
            {
                string videoId = video.VideoId;
                if (_subscFeedVideoRepository.Exists(x => x.SourceSubscId == subscId && x.VideoId == videoId) is false)
                {
                    var feed = new SubscFeedVideo
                    {
                        SourceSubscId = subscId,
                        Id = ObjectId.NewObjectId(),
                        VideoId = video.VideoId,
                        PostAt = video.PostedAt,
                        Title = video.Title,
                        FeedUpdateAt = updateAt,
                    };

                    _subscFeedVideoRepository.CreateItem(feed);
                    _messenger.Send(new NewSubscFeedVideoMessage(feed));

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

                    _messenger.Send(new NewSubscFeedVideoMessage(video));
                    yield return video;
                }
            }
        }
    }
}

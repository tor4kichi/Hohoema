using LiteDB;
using Mntone.Nico2;

using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Video
{
    public enum MovieType
    {
        Flv,
        Mp4,
        Swf,
    }

    public class NicoVideo : FixPrism.BindableBase, IVideoContent, IVideoContentWritable
    {
        [BsonId]
        public string RawVideoId { get; set; }
        public string VideoId { get; set; }

        public string ThreadId { get; set; }

        private string _title;
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }


        private string _ThumbnailUrl;
        public string ThumbnailUrl
        {
            get { return _ThumbnailUrl; }
            set { SetProperty(ref _ThumbnailUrl, value); }
        }

        private TimeSpan _Length;
        public TimeSpan Length
        {
            get { return _Length; }
            set { SetProperty(ref _Length, value); }
        }

        private DateTime _PostedAt;
        public DateTime PostedAt
        {
            get { return _PostedAt; }
            set { SetProperty(ref _PostedAt, value); }
        }


        private int _ViewCount;
        public int ViewCount
        {
            get { return _ViewCount; }
            set { SetProperty(ref _ViewCount, value); }
        }

        private int _CommentCount;
        public int CommentCount
        {
            get { return _CommentCount; }
            set { SetProperty(ref _CommentCount, value); }
        }

        private int _MylistCount;
        public int MylistCount
        {
            get { return _MylistCount; }
            set { SetProperty(ref _MylistCount, value); }
        }

        public string Description { get; set; }

        public double LoudnessCollectionValue { get; set; } = 1.0;

        [BsonRef]
        public NicoVideoOwner Owner { get; set; }

        /// <summary>
        /// [[deprecated]] ce.api経由では動画フォーマットが取得できない
        /// </summary>
        public MovieType MovieType { get; set; } = MovieType.Mp4;
        public List<NicoVideoTag> Tags { get; set; }

        public string DescriptionWithHtml { get; set; }

        public DateTime LastUpdated { get; set; }

        private bool _isDeleted;
        public bool IsDeleted
        {
            get { return _isDeleted; }
            set { SetProperty(ref _isDeleted, value); }
        }

        public PrivateReasonType PrivateReasonType { get; set; }

        [BsonIgnore]
        public string Id => VideoId ?? RawVideoId;

        [BsonIgnore]
        public string Label
        {
            get => Title;
            set => Title = value;
        }


        [BsonIgnore]
        public string ProviderId
        {
            get => Owner?.OwnerId;
            set 
            {
                if (value == null) { return; }
                if (Owner == null)
                {
                    Owner = new NicoVideoOwner()
                    {
                        OwnerId = value
                    };
                }
                else
                {
                    Owner.OwnerId = value;
                }
            }
        }

        [BsonIgnore]
        public NicoVideoUserType ProviderType
        {
            get => Owner?.UserType ?? NicoVideoUserType.User;
            set => Owner.UserType = value;
        }

        public bool Equals(IVideoContent other)
        {
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }




}

using LiteDB;
using Mntone.Nico2;
using NiconicoLiveToolkit.Video;
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

        private string _Description;
        public string Description
        {
            get { return _Description; }
            set { SetProperty(ref _Description, value); }
        }

        public double LoudnessCollectionValue { get; set; } = 1.0;

        [BsonRef]
        public NicoVideoOwner Owner { get; set; }

        /// <summary>
        /// [[deprecated]] ce.api経由では動画フォーマットが取得できない
        /// </summary>
        public MovieType MovieType { get; set; } = MovieType.Mp4;


        private List<NicoVideoTag> _tag;
        public List<NicoVideoTag> Tags
        {
            get { return _tag; }
            set { SetProperty(ref _tag, value); }
        }

        private string _DescriptionWithHtml;
        public string DescriptionWithHtml
        {
            get { return _DescriptionWithHtml; }
            set { SetProperty(ref _DescriptionWithHtml, value); }
        }

        public DateTime LastUpdated { get; set; }

        private bool _isDeleted;
        public bool IsDeleted
        {
            get { return _isDeleted; }
            set { SetProperty(ref _isDeleted, value); }
        }

        private PrivateReasonType _privateReasonType = PrivateReasonType.None;
        public PrivateReasonType PrivateReasonType
        {
            get { return _privateReasonType; }
            set { SetProperty(ref _privateReasonType, value); }
        }

        private VideoPermission _permission = VideoPermission.Unknown;
        public VideoPermission Permission
        {
            get { return _permission; }
            set { SetProperty(ref _permission, value); }
        }


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

        [BsonIgnore]
        public string ProviderName
        {
            get => Owner?.ScreenName;
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

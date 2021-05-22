using LiteDB;
using Mntone.Nico2;
using NiconicoToolkit.Video;
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

    public class NicoVideo : IVideoContent, IVideoContentProvider, IVideoDetail, IVideoDetailWritable
    {
        [BsonId]
        public string RawVideoId { get; set; }
        public string VideoId { get; set; }
        public string ThreadId { get; set; }

        public string Title { get; set; }
        public string ThumbnailUrl { get; set; }
        public TimeSpan Length { get; set; }
        public DateTime PostedAt { get; set; }
        public int ViewCount { get; set; }
        public int CommentCount { get; set; }
        public int MylistCount { get; set; }
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

        public bool IsDeleted { get; set; }


        public PrivateReasonType PrivateReasonType { get; set; } = PrivateReasonType.None;


        public VideoPermission Permission { get; set; } = VideoPermission.Unknown;


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

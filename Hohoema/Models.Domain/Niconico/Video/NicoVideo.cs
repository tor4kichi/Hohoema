using LiteDB;
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
    public class NicoVideo : IVideoContent, IVideoContentProvider
    {
        [BsonId]
        public string RawVideoId { get; set; }
        public string VideoId { get; set; }

        public string Title { get; set; }
        public string ThumbnailUrl { get; set; }
        public TimeSpan Length { get; set; }
        public DateTime PostedAt { get; set; }
        public string Description { get; set; }

        [BsonRef]
        public NicoVideoOwner Owner { get; set; }

        public DateTime LastUpdated { get; set; }


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
        public OwnerType ProviderType
        {
            get => Owner?.UserType ?? OwnerType.User;
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

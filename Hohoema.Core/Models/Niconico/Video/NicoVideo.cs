using LiteDB;
using NiconicoToolkit.Video;
using System;

#nullable enable

namespace Hohoema.Models.Niconico.Video;

public class NicoVideo : IVideoContent, IVideoContentProvider
{
    [BsonId]
    public string? Id { get; set; }

    private VideoId? _videoId;
    public VideoId VideoId => _videoId ??= Id;

    // VideoIdがシリアライズに対応していないのでVideoId型の使用を避けている
    [BsonIgnore]
    public string? VideoAliasId { get; set; }

    public string? Title { get; set; }
    public string? ThumbnailUrl { get; set; }
    public TimeSpan Length { get; set; }
    public DateTime PostedAt { get; set; }
    public string? Description { get; set; }

    [BsonRef]
    public NicoVideoOwner? Owner { get; set; }

    public DateTime LastUpdated { get; set; }


    [BsonIgnore]
    public string Label
    {
        get => Title;
        set => Title = value;
    }


    [BsonIgnore]
    public string? ProviderId
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
    public string? ProviderName => Owner?.ScreenName;


    public bool Equals(IVideoContent other)
    {
        return VideoId == other.VideoId;
    }

    public override int GetHashCode()
    {
        return VideoId.GetHashCode();
    }
}

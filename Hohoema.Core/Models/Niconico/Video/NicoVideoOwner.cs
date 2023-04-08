using Hohoema.Infra;
using Hohoema.Models.Niconico.Channel;
using LiteDB;
using NiconicoToolkit.Channels;
using NiconicoToolkit.User;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hohoema.Models.Niconico.Video;

public class NicoVideoOwner : IUser, IChannel
{
    [BsonId]
    public string OwnerId { get; set; }

    public string IconUrl { get; set; }

    public OwnerType UserType { get; set; }

    public string ScreenName { get; set; }

    string IUser.IconUrl => IconUrl;

    UserId IUser.UserId => UserType == OwnerType.User ? OwnerId : throw new InvalidOperationException();

    string IUser.Nickname => UserType == OwnerType.User ? ScreenName : throw new InvalidOperationException();

    ChannelId IChannel.ChannelId => UserType == OwnerType.Channel ? OwnerId : throw new InvalidOperationException();

    string IChannel.Name => UserType == OwnerType.Channel ? ScreenName : throw new InvalidOperationException();

    string INiconicoGroup.Name => UserType == OwnerType.Channel ? ScreenName : throw new InvalidOperationException();
}


public sealed class NicoVideoOwnerRepository : LiteDBServiceBase<NicoVideoOwner>
{
    public NicoVideoOwnerRepository(LiteDatabase liteDatabase) : base(liteDatabase)
    {
    }

    public NicoVideoOwner Get(string id)
    {
        return _collection.FindById(id);
    }

    public bool AddOrUpdate(NicoVideoOwner owner)
    {
        return _collection.Upsert(owner);
    }

    public List<NicoVideoOwner> SearchFromScreenName(string keyword)
    {
        return _collection.Find(x => x.ScreenName.Contains(keyword)).ToList();
    }

}

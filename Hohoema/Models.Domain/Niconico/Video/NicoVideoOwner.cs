using LiteDB;
using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Domain.Niconico.Channel;
using NiconicoToolkit.Video;
using NiconicoToolkit;
using NiconicoToolkit.User;

namespace Hohoema.Models.Domain.Niconico.Video
{
    public class NicoVideoOwner : IUser, IChannel
    {
        [BsonId]
        public string OwnerId { get; set; }

        public string IconUrl { get; set; }

        public OwnerType UserType { get; set; }

        public string ScreenName { get; set; }

        string IUser.IconUrl => this.IconUrl;

        UserId IUser.UserId => UserType == OwnerType.User ? OwnerId : throw new InvalidOperationException();

        string IUser.Nickname => UserType == OwnerType.User ? ScreenName : throw new InvalidOperationException();

        NiconicoId IChannel.ChannelId => UserType == OwnerType.Channel ? OwnerId : throw new InvalidOperationException();

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
}

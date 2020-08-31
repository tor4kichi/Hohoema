namespace NicoPlayerHohoema.Models.Niconico.User
{
    using LiteDB;
    using NiconicoLiveToolkit;
    using NicoPlayerHohoema.Repository;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Uno.Extensions;

    public sealed class UserNameEntity
    {
        [BsonId]
        public uint Id { get; set; }

        [BsonField]
        public string Name { get; set; }

        [BsonField]
        public DateTime UpdatedAt { get; set; }
    }

    public sealed class UserNameRepository
    {
        private readonly NiconicoContext _context;
        private readonly UserNameDb _userNameDb;


        public static TimeSpan UserNameExpire { get; set; } = TimeSpan.FromDays(1);

        public UserNameRepository(NiconicoSession session)
        {
            _userNameDb = new UserNameDb();
            _context = session.LiveContext;
        }

        public class UserNameDb : LocalLiteDBService<UserNameEntity>
        {
            public UserNameDb() : base()
            {
            }

            public UserNameEntity GetName(uint userId)
            {
                return _collection.FindById((long)userId);
            }
        }

        public bool TryResolveUserNameFromCache(uint userId, out string userName)
        {
            var cachedName = _userNameDb.GetName(userId);
            if (cachedName != null &&
                DateTime.Now.IsBefore(cachedName.UpdatedAt + UserNameExpire))
            {
                userName = cachedName.Name;
                return true;
            }
            else
            {
                userName = string.Empty;
                return false;
            }

        }


        public async ValueTask<string> ResolveUserNameAsync(uint userId)
        {
            var cachedName = _userNameDb.GetName(userId);
            if (cachedName != null &&
                DateTime.Now.IsBefore(cachedName.UpdatedAt + UserNameExpire))
            {
                return cachedName.Name;
            }

            var info = await _context.User.GetUserNicknameAsync(userId);
            if (info != null)
            {
                if (cachedName != null)
                {
                    cachedName.Name = info.Nickname;
                    cachedName.UpdatedAt = DateTime.Now;
                    _userNameDb.UpdateItem(cachedName);
                }
                else
                {
                    _userNameDb.CreateItem(new UserNameEntity()
                    {
                        Id = userId,
                        Name = info.Nickname,
                        UpdatedAt = DateTime.Now,
                    });
                }

                return info.Nickname;
            }
            else
            {
                return null;
            }
        }
    }
}

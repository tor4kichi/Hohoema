﻿#nullable enable
using Hohoema.Infra;
using LiteDB;
using NiconicoToolkit;
using System;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.User;

public sealed class UserNameEntity
{
    [BsonId]
    public uint Id { get; set; }

    [BsonField]
    public string Name { get; set; }

    [BsonField]
    public DateTime UpdatedAt { get; set; }
}

public sealed class UserNameProvider : ProviderBase
{
    private readonly NiconicoContext _context;
    private readonly UserNameRepository _userNameRepository;

    public static TimeSpan UserNameExpire { get; set; } = TimeSpan.FromDays(1);

    public UserNameProvider(NiconicoSession session, UserNameRepository userNameRepository)
        : base(session)
    {
        _context = session.ToolkitContext;
        _userNameRepository = userNameRepository;
    }

    public class UserNameRepository : LiteDBServiceBase<UserNameEntity>
    {
        public UserNameRepository(LiteDatabase database) : base(database)
        {
        }

        public UserNameEntity GetName(uint userId)
        {
            return _collection.FindById((long)userId);
        }
    }

    public bool TryResolveUserNameFromCache(uint userId, out string userName)
    {
        UserNameEntity cachedName = _userNameRepository.GetName(userId);
        if (cachedName != null &&
            DateTime.Now > cachedName.UpdatedAt + UserNameExpire)
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
        UserNameEntity cachedName = _userNameRepository.GetName(userId);
        if (cachedName != null &&
            DateTime.Now > cachedName.UpdatedAt + UserNameExpire)
        {
            return cachedName.Name;
        }

        NiconicoToolkit.User.UserNickname info = await _context.User.GetUserNicknameAsync(userId);
        if (info != null)
        {
            if (cachedName != null)
            {
                cachedName.Name = info.Nickname;
                cachedName.UpdatedAt = DateTime.Now;
                _ = _userNameRepository.UpdateItem(cachedName);
            }
            else
            {
                _ = _userNameRepository.CreateItem(new UserNameEntity()
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

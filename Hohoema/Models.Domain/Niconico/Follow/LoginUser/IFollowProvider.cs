using Mntone.Nico2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Follow.LoginUser
{
    public interface IFollowProvider<FollowableType> where FollowableType : IFollowable
    {
        Task<ContentManageResult> AddFollowAsync(FollowableType followable);
        Task<ContentManageResult> RemoveFollowAsync(FollowableType followable);
        Task<bool> IsFollowingAsync(FollowableType followable);
    }
}

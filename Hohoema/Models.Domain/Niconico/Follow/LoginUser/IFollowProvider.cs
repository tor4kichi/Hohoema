using Mntone.Nico2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Follow.LoginUser
{
    public interface IFollowProvider
    {
        Task<ContentManageResult> AddFollowAsync(string id);
        Task<ContentManageResult> RemoveFollowAsync(string id);
        Task<bool> IsFollowingAsync(string id);
    }
}

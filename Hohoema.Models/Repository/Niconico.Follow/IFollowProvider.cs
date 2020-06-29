using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Follow
{
    internal interface IFollowProvider
    {
        Task<ContentManageResult> AddFollowAsync(string id);
        Task<ContentManageResult> RemoveFollowAsync(string id);
    }
}

using Mntone.Nico2;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.LoginUser
{
    interface IFollowProvider
    {
        Task<ContentManageResult> AddFollowAsync(string id);
        Task<ContentManageResult> RemoveFollowAsync(string id);
    }

}

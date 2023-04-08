using NiconicoToolkit.Account;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Follow.LoginUser;

public interface IFollowProvider<FollowableType> where FollowableType : IFollowable
{
    Task<ContentManageResult> AddFollowAsync(FollowableType followable);
    Task<ContentManageResult> RemoveFollowAsync(FollowableType followable);
    Task<bool> IsFollowingAsync(FollowableType followable);
}

using System.Threading.Tasks;

namespace Hohoema.UseCase.Services
{
    public interface IConfirmCacheUsageDialogService
    {
        Task<bool> ShowAcceptCacheUsaseDialogAsync(bool showWithoutConfirmButton = false);
    }
}
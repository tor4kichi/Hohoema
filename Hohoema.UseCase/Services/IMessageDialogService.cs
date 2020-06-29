using System.Threading.Tasks;

namespace Hohoema.UseCase.Services
{
    public interface IMessageDialogService
    {
        Task<bool> ShowMessageDialog(string content, string title, string acceptButtonText = null, string cancelButtonText = null);
    }
}
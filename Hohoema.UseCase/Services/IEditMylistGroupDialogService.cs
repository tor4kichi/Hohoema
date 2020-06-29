using Hohoema.Models.Repository.Niconico.Mylist;
using System.Threading.Tasks;

namespace Hohoema.UseCase.Services
{
    public interface IEditMylistGroupDialogService
    {
        Task<bool> ShowCreateMylistGroupDialogAsync(MylistGroupEditData data);
        Task<bool> ShowEditMylistGroupDialogAsync(MylistGroupEditData data);
    }
}
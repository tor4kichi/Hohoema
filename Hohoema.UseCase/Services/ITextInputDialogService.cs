using System;
using System.Threading.Tasks;

namespace Hohoema.UseCase.Services
{
    public interface ITextInputDialogService
    {
        Task<string> GetTextAsync(string title, string placeholder, string defaultText = "", Func<string, bool> validater = null);
    }
}
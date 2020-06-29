using System.Threading.Tasks;

namespace Hohoema.UseCase.Services
{
    public interface INiconicoLoginDialogService
    {
        ValueTask<LoginInfoInputResult> ShowLoginInputDialogAsync(string mail, string password, bool isRemember, string warningText);
    }


    public sealed class LoginInfoInputResult
    {
        public string Mail { get; set; }
        public string Password { get; set; }
        public bool IsRememberPassword { get; set; }
    }
}
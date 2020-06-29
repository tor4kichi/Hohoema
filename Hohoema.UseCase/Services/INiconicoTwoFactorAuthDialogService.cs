using System.Threading.Tasks;

namespace Hohoema.UseCase.Services
{
    public interface INiconicoTwoFactorAuthDialogService
    {
        ValueTask<TwoFactorAuthInputResult> ShowNiconicoTwoFactorLoginDialog(bool defaultTrustedDevice, string defaultDeviceName);
    }


    public sealed class TwoFactorAuthInputResult
    {
        public bool IsTrustedDevice { get; set; }
        public string DeviceName { get; set; }
        public string Code { get; set; }
    }
}
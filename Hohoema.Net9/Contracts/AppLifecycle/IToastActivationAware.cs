#nullable enable
using Microsoft.Toolkit.Uwp.Notifications;
using System.Threading.Tasks;
using Windows.Foundation.Collections;

namespace Hohoema.Contracts.AppLifecycle;
public interface IToastActivationAware
{
    ValueTask<bool> TryHandleActivationAsync(ToastArguments arguments, ValueSet userInput);
}

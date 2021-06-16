using NiconicoToolkit;
using NiconicoToolkit.Live;

namespace Hohoema.Models.Domain.Niconico.Live
{
    public interface ILiveContent : INiconicoContent
    {
        LiveId LiveId { get; }

    }

    public interface ILiveContentProvider
    {
        string ProviderId { get; }
        string ProviderName { get; }
        ProviderType ProviderType { get; }
    }
}

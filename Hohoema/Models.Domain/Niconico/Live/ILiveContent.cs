using NiconicoToolkit.Live;

namespace Hohoema.Models.Domain.Niconico.Live
{
    public interface ILiveContent : INiconicoContent
    {
        string ProviderId { get; }
        string ProviderName { get; }
        ProviderType ProviderType { get; }
    }
}

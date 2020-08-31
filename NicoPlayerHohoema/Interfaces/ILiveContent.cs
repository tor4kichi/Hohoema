using NiconicoLiveToolkit.Live;

namespace NicoPlayerHohoema.Interfaces
{
    public interface ILiveContent : INiconicoContent
    {
        string ProviderId { get; }
        string ProviderName { get; }
        ProviderType ProviderType { get; }
    }
}

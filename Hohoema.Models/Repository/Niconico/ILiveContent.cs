using Hohoema.Models.Niconico.Live;

namespace Hohoema.Models.Repository
{
    public interface ILiveContent : INiconicoContent
    {
        string ProviderId { get; }
        string ProviderName { get; }
        LiveProviderType ProviderType { get; }
    }
}

#nullable enable
using NiconicoToolkit.Live;

namespace Hohoema.Models.Niconico.Live;

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

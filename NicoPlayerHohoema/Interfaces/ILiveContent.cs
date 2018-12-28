namespace NicoPlayerHohoema.Interfaces
{
    public interface ILiveContent : INiconicoContent
    {
        string ProviderId { get; }
        string ProviderName { get; }
        Mntone.Nico2.Live.CommunityType ProviderType { get; }

    }
}

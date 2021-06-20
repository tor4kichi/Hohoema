namespace Hohoema.Models.Domain.Niconico.Video.Series
{
    public interface ISeries
    {
        string Id { get; }
        string Title { get; }
        bool IsListed { get; }
        string Description { get; }
        string ThumbnailUrl { get; }
        int ItemsCount { get; }

        string ProviderType { get; }
        string ProviderId { get; }
    }
}

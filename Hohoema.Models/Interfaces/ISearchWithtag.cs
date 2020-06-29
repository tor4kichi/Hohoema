namespace Hohoema.Models.Repository.Niconico
{
    public interface ISearchWithtag : IFollowable
    {
        string Tag { get; }
    }
}
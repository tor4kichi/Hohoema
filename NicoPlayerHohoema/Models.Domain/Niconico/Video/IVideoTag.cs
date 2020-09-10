namespace Hohoema.Models.Domain.Niconico.Video
{
    public interface ITag
    {
        string Tag { get; }
        bool IsLocked { get; }
        bool IsCategoryTag { get; }
        bool IsDictionaryExists { get; }
    }
}

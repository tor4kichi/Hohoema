namespace Hohoema.Models.Repository
{
    public interface INiconicoObject
    {
        string Id { get; }
        string Label { get; }
    }

    public interface INiconicoContent : INiconicoObject
    {
    }

    public interface INiconicoGroup : INiconicoObject
    {
        
    }
}

namespace Hohoema.Models.Niconico;

public interface INiconicoObject
{
}

public interface INiconicoContent : INiconicoObject
{
    public string Title { get; }
}

public interface INiconicoGroup : INiconicoObject
{
    public string Name { get; }
}

namespace Hohoema.Services.Navigations;

public class MylistPagePayload : PagePayloadBase
{
    public string Id { get; set; }

    public MylistPagePayload() { }

    public MylistPagePayload(string id)
    {
        Id = id;
    }
}

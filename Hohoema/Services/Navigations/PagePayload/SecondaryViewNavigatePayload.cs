#nullable enable
namespace Hohoema.Services.Navigations;

public class SecondaryViewNavigatePayload : PagePayloadBase
{
    public string ContentId { get; set; }
    public string Title { get; set; }
    public string ContentType { get; set; }
}

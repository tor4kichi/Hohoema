#nullable enable
using System.Text.Json;

namespace Hohoema.Models.Live;


public class LiveVideoPagePayload
{
    public string LiveId { get; set; }

    public string LiveTitle { get; set; }
    public string CommunityId { get; set; }
    public string CommunityName { get; set; }

    public LiveVideoPagePayload(string liveId)
    {
        LiveId = liveId;
    }

    public string ToParameterString()
    {
        return JsonSerializer.Serialize(this);
    }

    public static LiveVideoPagePayload FromParameterString(string json)
    {
        return JsonSerializer.Deserialize<LiveVideoPagePayload>(json);
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Video.Watch
{
    public partial class DmcWatchApiEnvironment
    {
        [JsonPropertyName("baseURL")]
        public BaseUrl BaseUrl { get; set; }

        [JsonPropertyName("playlistToken")]
        public string PlaylistToken { get; set; }

        [JsonPropertyName("i18n")]
        public I18N I18N { get; set; }

        [JsonPropertyName("urls")]
        public Urls Urls { get; set; }

        [JsonPropertyName("isMonitoringLogUser")]
        public bool IsMonitoringLogUser { get; set; }

        [JsonPropertyName("frontendId")]
        public long FrontendId { get; set; }

        [JsonPropertyName("frontendVersion")]
        [JsonConverter(typeof(LongToStringConverter))]
        public long FrontendVersion { get; set; }

        [JsonPropertyName("newPlaylistRate")]
        public long NewPlaylistRate { get; set; }

        [JsonPropertyName("newRelatedVideos")]
        public bool NewRelatedVideos { get; set; }
    }

    public partial class BaseUrl
    {
        [JsonPropertyName("web")]
        public Uri Web { get; set; }

        [JsonPropertyName("res")]
        public Uri Res { get; set; }

        [JsonPropertyName("dic")]
        public Uri Dic { get; set; }

        [JsonPropertyName("flapi")]
        public Uri Flapi { get; set; }

        [JsonPropertyName("live")]
        public Uri Live { get; set; }

        [JsonPropertyName("live2")]
        public Uri Live2 { get; set; }

        [JsonPropertyName("com")]
        public Uri Com { get; set; }

        [JsonPropertyName("ch")]
        public Uri Ch { get; set; }

        [JsonPropertyName("chPublicAPI")]
        public Uri ChPublicApi { get; set; }

        [JsonPropertyName("secureCh")]
        public Uri SecureCh { get; set; }

        [JsonPropertyName("commons")]
        public Uri Commons { get; set; }

        [JsonPropertyName("commonsAPI")]
        public Uri CommonsApi { get; set; }

        [JsonPropertyName("embed")]
        public Uri Embed { get; set; }

        [JsonPropertyName("ext")]
        public Uri Ext { get; set; }

        [JsonPropertyName("nicoMs")]
        public Uri NicoMs { get; set; }

        [JsonPropertyName("ichiba")]
        public Uri Ichiba { get; set; }

        [JsonPropertyName("ads")]
        public Uri Ads { get; set; }

        [JsonPropertyName("account")]
        public Uri Account { get; set; }

        [JsonPropertyName("secure")]
        public Uri Secure { get; set; }

        [JsonPropertyName("premium")]
        public Uri Premium { get; set; }

        [JsonPropertyName("ex")]
        public Uri Ex { get; set; }

        [JsonPropertyName("qa")]
        public Uri Qa { get; set; }

        [JsonPropertyName("publicAPI")]
        public Uri PublicApi { get; set; }

        [JsonPropertyName("commonsPublicAPI")]
        public Uri CommonsPublicApi { get; set; }

        [JsonPropertyName("app")]
        public Uri App { get; set; }

        [JsonPropertyName("appClientAPI")]
        public Uri AppClientApi { get; set; }

        [JsonPropertyName("point")]
        public Uri Point { get; set; }

        [JsonPropertyName("enquete")]
        public Uri Enquete { get; set; }

        [JsonPropertyName("notification")]
        public Uri Notification { get; set; }

        [JsonPropertyName("upload")]
        public Uri Upload { get; set; }

        [JsonPropertyName("sugoiSearchSystem")]
        public Uri SugoiSearchSystem { get; set; }

        [JsonPropertyName("nicoad")]
        public Uri Nicoad { get; set; }

        [JsonPropertyName("nicoadAPI")]
        public Uri NicoadApi { get; set; }

        [JsonPropertyName("secureDCDN")]
        public Uri SecureDcdn { get; set; }

        [JsonPropertyName("seiga")]
        public Uri Seiga { get; set; }

        [JsonPropertyName("nvapi")]
        public Uri Nvapi { get; set; }
    }

    public partial class I18N
    {
        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("locale")]
        public string Locale { get; set; }

        [JsonPropertyName("area")]
        public string Area { get; set; }

        [JsonPropertyName("footer")]
        public object Footer { get; set; }
    }

    public partial class Urls
    {
        [JsonPropertyName("playerHelp")]
        public Uri PlayerHelp { get; set; }
    }

}

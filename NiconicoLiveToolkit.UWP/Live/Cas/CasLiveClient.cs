using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoLiveToolkit.Live.Cas
{
    public sealed class CasLiveClient
    {
        private readonly NiconicoContext _context;
        private readonly JsonSerializerOptions _jsonSerializeOptions;

        internal CasLiveClient(NiconicoContext context)
        {
            _context = context;

            _jsonSerializeOptions = new JsonSerializerOptions()
            {
                Converters =
                {
                    new JsonStringEnumMemberConverter(JsonSnakeCaseNamingPolicy.Instance),
                }
            };
        }

        public Task<LiveProgramResponse> GetLiveProgramAsync(string liveId)
        {
            const string NicocasLiveUrlFormat = @"https://api.cas.nicovideo.jp/v1/services/live/programs/{0}";
            return _context.GetJsonAsAsync<LiveProgramResponse>(string.Format(NicocasLiveUrlFormat, liveId), _jsonSerializeOptions);
        }
    }
}

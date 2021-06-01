using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Video.Watch.Dmc
{
    public class Protocol
    {

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("parameters")]
        public ProtocolParameters Parameters { get; set; }


        public class ProtocolParameters
        {

            [JsonPropertyName("http_parameters")]
            public HttpParameters HttpParameters { get; set; }
        }

        public class HttpParameters
        {

            [JsonPropertyName("method")]
            public string Method { get; set; }

            [JsonPropertyName("parameters")]
            public ParametersInfo Parameters { get; set; }
        }


        public class ParametersInfo
        {
            [JsonPropertyName("hls_parameters")]
            public HlsParameters HlsParameters { get; set; }

            [JsonPropertyName("http_output_download_parameters")]
            public HttpOutputDownloadParameters HttpOutputDownloadParameters { get; set; }
        }


        public class HlsParameters
        {

            [JsonPropertyName("use_well_known_port")]
            public string UseWellKnownPort { get; set; }

            [JsonPropertyName("use_ssl")]
            public string UseSsl { get; set; }

            [JsonPropertyName("transfer_preset")]
            public string TransferPreset { get; set; }

            [JsonPropertyName("segment_duration")]
            public int SegmentDuration { get; set; }

            [JsonPropertyName("encryption")]
            public Encryption Encryption { get; set; }



            [JsonPropertyName("total_duration")]
            public int? TotalDuration { get; set; }

            [JsonPropertyName("media_segment_format")]
            public string MediaSegmentFormat { get; set; }

        }


        public class Encryption
        {

            [JsonPropertyName("hls_encryption_v1")]
            public HlsEncryptionV1 HlsEncryptionV1 { get; set; }

            [JsonPropertyName("empty")]
            public Empty Empty { get; set; }
        }


        public class HlsEncryptionV1
        {

            [JsonPropertyName("encrypted_key")]
            public string EncryptedKey { get; set; }

            [JsonPropertyName("key_uri")]
            public string KeyUri { get; set; }
        }


        public class Empty
        {
        }



        public class HttpOutputDownloadParameters
        {
            // res
            [JsonPropertyName("file_extension")]
            public string FileExtension { get; set; }

            // res
            [JsonPropertyName("transfer_preset")]
            public string TransferPreset { get; set; } = "";

            // res req
            [JsonPropertyName("use_ssl")]
            public string UseSsl { get; set; } = "yes";

            // res req
            [JsonPropertyName("use_well_known_port")]
            public string UseWellKnownPort { get; set; } = "yes";
        }
    }



}

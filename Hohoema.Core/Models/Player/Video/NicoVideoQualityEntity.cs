#nullable enable
using Hohoema.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hohoema.Models.Niconico.Video;

public sealed class NicoVideoQualityEntity
{
    public NicoVideoQualityEntity(
        bool isAvailable, NicoVideoQuality quality, string qualityId,
        int? bitrate = null, int? width = null, int? height = null
        )
    {
        IsAvailable = isAvailable;
        Quality = quality;
        QualityId = qualityId;
        Bitrate = bitrate;
        Width = width;
        Height = height;
    }

    public bool IsAvailable { get; }

    public NicoVideoQuality Quality { get; }
    public string QualityId { get; }

    public int? Bitrate { get; }
    public int? Width { get; }
    public int? Height { get; }

    readonly static Dictionary<(string, int), string> _bitrateStringCached = new();

    private readonly static string[] _videoQualityPostfixItems = new[] 
    {
        "1080p",
        "720p",
        "480p",
        "360p",
        "low",
    };

    public override string ToString()
    {
        // キャッシュにヒットするかチェック
        // stringを出来るだけ比較しない
        foreach (var (key, value) in _bitrateStringCached)
        {
            if (Bitrate == key.Item2 && QualityId.EndsWith(key.Item1, StringComparison.Ordinal))
            {
                return value;
            }
        }

        // キャッシュヒットしなかった場合
        // 画質末尾に対する文字列分割を回避してキャッシュ生成を試行
        foreach (var q in _videoQualityPostfixItems)
        {
            if (QualityId.EndsWith(q, System.StringComparison.Ordinal))
            {
                var key = (q, Bitrate ?? 0);
                if (_bitrateStringCached.TryGetValue(key, out string cachedToString) is false)
                {
                    cachedToString = Bitrate.HasValue
                        ? $"{QualityId.Split('_').Last()} ({NumberToKMGTPEZYStringHelper.ToKMGTPEZY(Bitrate.Value)}bps)"
                        : $"{QualityId.Split('_').Last()}";
                    _bitrateStringCached.Add(key, cachedToString);
                }

                return cachedToString;
            }
        }

        // 不明な画質末尾文字列に対しては分割してキャッシュ生成       
        var newKey = (QualityId.Split('_').Last(), Bitrate ?? 0);
        var outValue = Bitrate.HasValue 
            ? $"{newKey.Item1} ({NumberToKMGTPEZYStringHelper.ToKMGTPEZY(newKey.Item2)}bps)"
            : newKey.Item1;
        _bitrateStringCached.Add(newKey, outValue);
        return outValue;
    }

    public bool IsSameQuality(string qualityId)
    {
        return qualityId == QualityId;
    }
}

using Hohoema.Models.Application;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Player;
using Hohoema.Models.Playlist;
using Hohoema.Models.Subscriptions;
using Hohoema.Models.VideoCache;
using NiconicoToolkit.Ranking.Video;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Hohoema.Models.PageNavigation.RestoreNavigationManager;
using static Hohoema.Models.Player.Comment.CommentFliteringRepository;
using static Hohoema.Models.Subscriptions.SubscriptionGroupPropsRespository;

namespace Hohoema.Core.Models;

[JsonSourceGenerationOptions()]
[JsonSerializable(typeof(AppearanceSettings))]
[JsonSerializable(typeof(VideoFilteringSettings))]
[JsonSerializable(typeof(HashSet<RankingGenre>))]
[JsonSerializable(typeof(ReadOnlyCollection<RankingGenreTag>))]
[JsonSerializable(typeof(List<RankingGenreTag>))]
[JsonSerializable(typeof(NavigationStackRepository))]
[JsonSerializable(typeof(CommentFilteringSettings))]
[JsonSerializable(typeof(PlayerSettings))]
[JsonSerializable(typeof(QueuePlaylistSetting))]
[JsonSerializable(typeof(SubscriptionGroupPropsForDefault))]
[JsonSerializable(typeof(SubscriptionSettings))]
[JsonSerializable(typeof(VideoCacheSettings))]
public partial class SourceGenerationContext : JsonSerializerContext
{
}

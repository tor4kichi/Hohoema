using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace NiconicoLiveToolkit.Live.Search
{
	/// <summary>
	/// niconico コンテンツ検索APIのフィールド
	/// see@ https://site.nicovideo.jp/search-api-docs/search.html
	/// </summary>
	[Flags]
	public enum SearchFieldType
	{
		/// <summary>
		/// コンテンツID。https://nico.ms/ の後に連結することでコンテンツへのURLになります。
		/// </summary>
		[Description("contentId")]
		ContentId = 0x0000_0001,

		/// <summary>
		/// タイトル
		/// </summary>
		[Description("title")]
		Title = 0x0000_0002,

		/// <summary>
		/// コンテンツの説明文
		/// </summary>
		[Description("description")]
		Description = 0x0000_0004,

		/// <summary>
		/// 投稿者のID
		/// </summary>
		[Description("userId")]
		UserId = 0x0000_0008,

		/// <summary>
		/// 再生数
		/// </summary>
		[Description("viewCounter")]
		ViewCounter = 0x0000_0010,

		/// <summary>
		/// マイリスト数
		/// </summary>
		[Description("mylistCounter")]
		MylistCounter = 0x0000_0020,

		/// <summary>
		/// 再生時間(秒)
		/// </summary>
		[Description("lengthSeconds")]
		LengthSeconds = 0x0000_0040,

		/// <summary>
		/// サムネイルのURL
		/// </summary>
		[Description("thumbnailUrl")]
		ThumbnailUrl = 0x0000_0080,

		/// <summary>
		/// 生放送の開場時間
		/// </summary>
		[Description("openTime")]
		OpenTime = 0x0000_0100,


		/// <summary>
		/// 動画の投稿時間 / 生放送の開始時間
		/// </summary>
		[Description("startTime")]
		StartTime = 0x0000_0200,

		/// <summary>
		/// 生放送の終了時間
		/// </summary>
		[Description("liveEndTime")]
		LiveEndTime = 0x0000_0400,

		/// <summary>
		/// タイムシフト視聴可能か
		/// </summary>
		[Description("timeshiftEnabled")]
		TimeshiftEnabled = 0x0000_0800,

		/// <summary>
		/// タイムシフト予約者数
		/// </summary>
		[Description("scoreTimeshiftReserved")]
		ScoreTimeshiftReserved = 0x0000_1000,

		/// <summary>
		/// コミュニティ名
		/// </summary>
		[Description("communityText")]
		CommunityText = 0x0000_2000,

		/// <summary>
		/// コミュニティアイコンのURL
		/// </summary>
		[Description("communityIcon")]
		CommunityIcon = 0x0000_4000,

		/// <summary>
		/// チャンネル・コミュニティ限定か
		/// </summary>
		[Description("memberOnly")]
		MemberOnly = 0x0000_8000,

		/// <summary>
		/// 放送ステータス（過去放送/生放送中/予約放送）
		/// </summary>
		[Description("liveStatus")]
		LiveStatus = 0x0001_0000,

		/// <summary>
		/// スレッドのID
		/// </summary>
		[Description("threadId")]
		ThreadId = 0x0002_0000,

		/// <summary>
		/// コメント数
		/// </summary>
		[Description("commentCounter")]
		CommentCounter = 0x0004_0000,

		/// <summary>
		/// 最終コメント時間
		/// </summary>
		[Description("lastCommentTime")]
		LastCommentTime = 0x0008_0000,

		/// <summary>
		/// カテゴリタグ
		/// </summary>
		[Description("categoryTags")]
		CategoryTags = 0x0010_0000,

		/// <summary>
		/// チャンネルのID
		/// </summary>
		[Description("channelId")]
		ChannelId = 0x0020_0000,

		/// <summary>
		/// コミュニティID
		/// </summary>
		[Description("communityId")]
		CommunityId = 0x0040_0000,

		/// <summary>
		/// 放送元種別
		/// </summary>
		[Description("providerType")]
		ProviderType = 0x0080_0000,

		/// <summary>
		/// タグ(空白区切り)
		/// </summary>
		[Description("tags")]
		Tags = 0x0100_0000,

		/// <summary>
		/// タグ完全一致(空白区切り)
		/// </summary>
		[Description("tagsExact")]
		TagsExact = 0x0200_0000,

		/// <summary>
		/// ロックされたタグ完全一致(空白区切り)
		/// </summary>
		[Description("lockTagsExact")]
		LockTagsExact = 0x0400_0000,

		/// <summary>
		/// ジャンル
		/// </summary>
		[Description("genre")]
		Genre = 0x0800_0000,

		/// <summary>
		/// ジャンル完全一致
		/// </summary>
		[Description("genre.keyword")]
		GenreKeyword = 0x1000_0000,
	}
}

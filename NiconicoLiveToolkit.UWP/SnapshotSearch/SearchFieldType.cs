using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace NiconicoToolkit.SnapshotSearch
{
	public class SearchFieldTypeAttribute : Attribute
	{
		public SearchFieldTypeAttribute(Type type) => Type = type;

        public Type Type { get; }
    }

	public class SearchFieldAttribute : Attribute
	{
		public SearchFieldAttribute() { }
	}

	public class SearchTargetAttribute : Attribute
	{
		public SearchTargetAttribute() { }
	}


	public class SearchSortAttribute : Attribute
	{
		public SearchSortAttribute() { }
	}

	public class SearchFilterAttribute : Attribute
	{
		public SearchFilterAttribute() { }
	}


	public static class SearchFieldTypeExtensions
    {
		static readonly Dictionary<SearchFieldType, Type> AcceptableTypeForField;

		public static ImmutableHashSet<SearchFieldType> FieldTypes { get; }
		public static ImmutableHashSet<SearchFieldType> TargetFieldTypes { get; }
		public static ImmutableHashSet<SearchFieldType> SortFieldTypes { get; }
		public static ImmutableHashSet<SearchFieldType> FilterFieldTypes { get; }


		static SearchFieldTypeExtensions()
        {
			var values = Enum.GetValues(typeof(SearchFieldType)).Cast<SearchFieldType>().ToArray();
			FieldTypes = values.Where(x => x.HasAttribute<SearchFieldAttribute>()).ToImmutableHashSet();
			TargetFieldTypes = values.Where(x => x.HasAttribute<SearchTargetAttribute>()).ToImmutableHashSet();
			SortFieldTypes = values.Where(x => x.HasAttribute<SearchSortAttribute>()).ToImmutableHashSet();
			FilterFieldTypes = values.Where(x => x.HasAttribute<SearchFilterAttribute>()).ToImmutableHashSet();

			AcceptableTypeForField = values.ToDictionary(x => x, x => x.GetAttrubute<SearchFieldTypeAttribute>().Type);
		}

		public static bool IsField(this SearchFieldType searchFieldType)
		{
			return FieldTypes.Contains(searchFieldType);
		}
		public static bool IsTargetField(this SearchFieldType searchFieldType)
		{
			return TargetFieldTypes.Contains(searchFieldType);
		}

		public static bool IsSortField(this SearchFieldType searchFieldType)
        {
			return SortFieldTypes.Contains(searchFieldType);
		}

		public static bool IsFilterField(this SearchFieldType searchFieldType)
        {
			return FilterFieldTypes.Contains(searchFieldType);
		}

		public static bool IsAcceptableTypeForFiled<T>(this SearchFieldType searchFieldType)
        {
			return IsAcceptableTypeForFiled(searchFieldType, typeof(T));
		}

		public static bool IsAcceptableTypeForFiled(this SearchFieldType searchFieldType, Type type)
		{
			return AcceptableTypeForField[searchFieldType] == type;
		}


		public static string ToQueryString(this SearchFieldType searchFieldType)
        {
			return searchFieldType.GetDescription();
		}

		public static string ToQueryString(this SearchFieldType[] searchFieldTypes)
		{
			return string.Join(SearchConstants.FiledQuerySeparator, searchFieldTypes.Select(x => x.GetDescription()));
		}
	}



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
		/// <remarks>string | SearchTarget | SearchField | SearchFilter</remarks>
		[Description("contentId")]
		[SearchFieldType(typeof(string))]
		[SearchField]
		[SearchFilter]
		ContentId = 0x0000_0001,

		/// <summary>
		/// タイトル
		/// </summary>
		/// <remarks>string | SearchTarget | SearchField</remarks>
		[Description("title")]
		[SearchFieldType(typeof(string))]
		[SearchTarget]
		[SearchField]
		Title = 0x0000_0002,

		/// <summary>
		/// コンテンツの説明文
		/// </summary>
		/// <remarks>string | SearchTarget | SearchField</remarks>
		[Description("description")]
		[SearchFieldType(typeof(string))]
		[SearchTarget]
		[SearchField]
		Description = 0x0000_0004,

		/// <summary>
		/// 投稿者のID
		/// </summary>
		/// <remarks>int | SearchField</remarks>
		[Description("userId")]
		[SearchFieldType(typeof(int))]
		[SearchField]
		UserId = 0x0000_0010,


		/// <summary>
		/// チャンネルのID
		/// </summary>
		/// <remarks>int | SearchField</remarks>
		[Description("channelId")]
		[SearchFieldType(typeof(int))]
		[SearchField]
		ChannelId = 0x0000_0020,

		/// <summary>
		/// 再生数
		/// </summary>
		/// <remarks>int | SearchField | SearchSort | SearchFilter</remarks>
		[Description("viewCounter")]
		[SearchFieldType(typeof(int))]
		[SearchField]
		[SearchSort]
		[SearchFilter]
		ViewCounter = 0x0000_0100,

		/// <summary>
		/// マイリスト数
		/// </summary>
		/// <remarks>int | SearchField | SearchSort | SearchFilter</remarks>
		[Description("mylistCounter")]
		[SearchFieldType(typeof(int))]
		[SearchField]
		[SearchSort]
		[SearchFilter]
		MylistCounter = 0x0000_0200,

		/// <summary>
		/// いいね！数
		/// </summary>
		/// <remarks>int | SearchField | SearchSort | SearchFilter</remarks>
		[Description("likeCounter")]
		[SearchFieldType(typeof(int))]
		[SearchField]
		[SearchSort]
		[SearchFilter]
		LikeCounter = 0x0000_0800,


		/// <summary>
		/// 再生時間(秒)
		/// </summary>
		/// <remarks>int | SearchField | SearchSort | SearchFilter</remarks>
		[Description("lengthSeconds")]
		[SearchFieldType(typeof(int))]
		[SearchField]
		[SearchSort]
		[SearchFilter]
		LengthSeconds = 0x0000_1000,

		/// <summary>
		/// サムネイルのURL
		/// </summary>
		/// <remarks>Uri | SearchField</remarks>
		[Description("thumbnailUrl")]
		[SearchFieldType(typeof(Uri))]
		[SearchField]
		ThumbnailUrl = 0x0000_2000,

		/// <summary>
		/// コンテンツの投稿時間。
		/// </summary>
		/// <remarks>DateTime | SearchField | SearchSort | SearchFilter</remarks>
		[Description("startTime")]
		[SearchFieldType(typeof(DateTime))]
		[SearchField]
		[SearchSort]
		[SearchFilter]
		StartTime = 0x0000_4000,

		/// <summary>
		/// 最新のコメント
		/// </summary>
		/// <remarks>string | SearchField</remarks>
		[Description("lastResBody")]
		[SearchFieldType(typeof(string))]
		[SearchField]
		LastResBody = 0x0001_0000,


		/// <summary>
		/// コメント数
		/// </summary>
		/// <remarks>int | SearchField | SearchSort | SearchFilter</remarks>
		[Description("commentCounter")]
		[SearchFieldType(typeof(int))]
		[SearchField]
		[SearchSort]
		[SearchFilter]
		CommentCounter = 0x0000_0400,

		/// <summary>
		/// 最終コメント時間
		/// </summary>
		/// <remarks>DateTime | SearchField | SearchSort | SearchFilter</remarks>
		[Description("lastCommentTime")]
		[SearchFieldType(typeof(DateTime))]
		[SearchField]
		[SearchSort]
		[SearchFilter]
		LastCommentTime = 0x0002_0000,

		/// <summary>
		/// カテゴリタグ
		/// </summary>
		/// <remarks>string | SearchField | SearchFilter</remarks>
		[Description("categoryTags")]
		[SearchFieldType(typeof(string))]
		[SearchField]
		[SearchFilter]
		CategoryTags = 0x0010_0000,

		/// <summary>
		/// タグ(空白区切り)
		/// </summary>
		/// <remarks>string | SearchField | SearchFilter</remarks>
		[Description("tags")]
		[SearchFieldType(typeof(string))]
		[SearchTarget]
		[SearchField]
		[SearchFilter]
		Tags = 0x0020_0000,

		/// <summary>
		/// タグ完全一致(空白区切り)
		/// </summary>
		/// <remarks>string | SearchFilter</remarks>
		[Description("tagsExact")]
		[SearchFieldType(typeof(string))]
		[SearchTarget]
		[SearchFilter]
		TagsExact = 0x0040_0000,

		/// <summary>
		/// ジャンル
		/// </summary>
		/// <remarks>string | SearchField | SearchFilter</remarks>
		[Description("genre")]
		[SearchFieldType(typeof(string))]
		[SearchField]
		[SearchFilter]
		Genre = 0x0100_0000,

		/// <summary>
		/// ジャンル完全一致
		/// </summary>
		/// <remarks>string | SearchFilter</remarks>
		[Description("genre.keyword")]
		[SearchFieldType(typeof(string))]
		[SearchFilter]
		GenreKeyword = 0x0200_0000,
	}
}

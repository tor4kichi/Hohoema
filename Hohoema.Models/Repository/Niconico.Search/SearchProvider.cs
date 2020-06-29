﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Repository.Niconico.Mylist;
using Hohoema.Models.Repository.Niconico.Search;
using Hohoema.Database;
using Hohoema.Models.Niconico;

namespace Hohoema.Models.Repository.Niconico
{
    public sealed class SearchProvider : ProviderBase
    {
        private readonly MylistProvider _mylistProvider;

        // TODO: タグによる生放送検索を別メソッドに分ける

        public SearchProvider(
            NiconicoSession niconicoSession,
            MylistProvider mylistProvider
            )
            : base(niconicoSession)
        {
            _mylistProvider = mylistProvider;
        }

        public async Task<VideoSearchResult> GetKeywordSearch(string keyword, uint from, uint limit, Sort sort = Sort.FirstRetrieve, Order order = Order.Descending)
        {
            var res =  await ContextActionAsync(async context =>
            {
                return await context.Search.VideoSearchWithKeywordAsync(keyword, from, limit, sort.ToInfrastructureSort(), order.ToInfrastructureOrder());
            });

            return new VideoSearchResult()
            {
                ItemsCount = (int)res.GetCount(),
                TotalCount = (int)res.GetTotalCount(),
                Tags = res.Tags?.TagItems.Select(x => new Models.Niconico.Video.NicoVideoTag(x.Name)).ToList(),
                VideoItems = res.VideoInfoItems?.Select(ToNicoVideo).ToList()
            };
        }

        public async Task<VideoSearchResult> GetTagSearch(string tag, uint from, uint limit, Sort sort = Sort.FirstRetrieve, Order order = Order.Descending)
        {
            var res = await ContextActionAsync(async context =>
            {
                return await context.Search.VideoSearchWithTagAsync(tag, from, limit, sort.ToInfrastructureSort(), order.ToInfrastructureOrder());
            });

            return new VideoSearchResult()
            {
                ItemsCount = (int)res.GetCount(),
                TotalCount = (int)res.GetTotalCount(),
                Tags = res.Tags?.TagItems.Select(x => new Models.Niconico.Video.NicoVideoTag(x.Name)).ToList(),
                VideoItems = res.VideoInfoItems?.Select(ToNicoVideo).ToList()
            };
        }

        private static Database.NicoVideo ToNicoVideo(Mntone.Nico2.Searches.Video.VideoInfo x)
        {
            var video = Database.NicoVideoDb.Get(x.Video.Id);
            video.IsDeleted = x.Video.IsDeleted;
            video.Title = x.Video.Title;
            video.ThumbnailUrl = x.Video.ThumbnailUrl.OriginalString;
            video.ThreadId = x.Video.DefaultThread;
            video.Description = x.Video.Description;
            video.CommentCount = (int)x.Thread.GetCommentCount();
            video.ViewCount = (int)x.Video.ViewCount;
            video.MylistCount = (int)x.Video.MylistCount;
            video.Length = x.Video.Length;
            video.PostedAt = x.Video.FirstRetrieve;
            video.Owner = video.Owner ?? x.Video.ProviderType switch
            {
                "channel" => new NicoVideoOwner() { OwnerId = x.Video.CommunityId, UserType = NicoVideoUserType.Channel },
                _ => new NicoVideoOwner() { OwnerId = x.Video.UserId, UserType = NicoVideoUserType.User }
            };

            Database.NicoVideoDb.AddOrUpdate(video);

            return video;
        }

        public async Task<Mntone.Nico2.Searches.Live.LiveSearchResponse> LiveSearchAsync(
            string q,
            int offset,
            int limit,
            SearchTargetType targets = SearchTargetType.All,
            LiveSearchFieldType fields = LiveSearchFieldType.All,
            LiveSearchSortType sortType = LiveSearchSortType.StartTime | LiveSearchSortType.SortDecsending,
            LiveProviderType liveProviderType = LiveProviderType.All
            )
        {
            return await ContextActionWithPageAccessWaitAsync(async context =>
            {
                Expression<Func<Mntone.Nico2.Searches.Live.SearchFilterField, bool>> expression = liveProviderType switch
                {
                    LiveProviderType.All => default,
                    LiveProviderType.Official => (Mntone.Nico2.Searches.Live.SearchFilterField x) => x.ProviderType == Mntone.Nico2.Searches.Live.SearchFilterField.ProviderTypeOfficial,
                    LiveProviderType.Channel => (Mntone.Nico2.Searches.Live.SearchFilterField x) => x.ProviderType == Mntone.Nico2.Searches.Live.SearchFilterField.ProviderTypeChannel,
                    LiveProviderType.Community => (Mntone.Nico2.Searches.Live.SearchFilterField x) => x.ProviderType == Mntone.Nico2.Searches.Live.SearchFilterField.ProviderTypeCommunity,
                    _ => throw new NotSupportedException(),
                };

                return await context.Search.LiveSearchAsync(
                    q, offset, limit,
                    targets.ToInfrastructureSearchTargetType(), 
                    fields.ToInfrastructureSearchFieldType(), 
                    sortType.ToInfrastructureLiveSearchSortType(),
                    expression
                );

            });
            
        }


        public enum LiveProviderType
        {
            All,
            Official,
            Channel,
            Community,
        }


        public async Task<Mntone.Nico2.Searches.Suggestion.SuggestionResponse> GetSearchSuggestKeyword(string keyword)
        {
            return await ContextActionAsync(async context =>
            {
                return await context.Search.GetSuggestionAsync(keyword);
            });
            
        }
    }
}

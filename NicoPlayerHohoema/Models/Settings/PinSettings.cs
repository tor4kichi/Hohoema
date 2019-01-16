using NicoPlayerHohoema.Services.Page;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
    [DataContract]
    public sealed class PinSettings : SettingsBase
    {
        [DataMember]
        public ObservableCollection<HohoemaPin> Pins { get; } = new ObservableCollection<HohoemaPin>();

        [DataMember]
        public bool IsMigrated_Prism6_to_Prism7 { get; private set; }


        public static void MigratePinParameter_Prism6_to_Prism7(PinSettings pinSettings)
        {
            if (pinSettings.IsMigrated_Prism6_to_Prism7) { return; }
            foreach (var pin in pinSettings.Pins)
            {
                // Mylist
                switch (pin.PageType)
                {
                    case Services.HohoemaPageType.RankingCategoryList:
                        break;
                    case Services.HohoemaPageType.RankingCategory:
                        pin.Parameter = $"category={pin.Parameter}";
                        break;
                    case Services.HohoemaPageType.UserMylist:
                        break;
                    case Services.HohoemaPageType.Mylist:
                        try
                        {
                            var mylistPayload = MylistPagePayload.FromParameterString<MylistPagePayload>(pin.Parameter);
                            if (mylistPayload != null)
                            {
                                if (mylistPayload.Origin.HasValue)
                                {
                                    pin.Parameter = $"id={mylistPayload.Id}&origin={mylistPayload.Origin}";
                                }
                                else
                                {
                                    pin.Parameter = $"id={mylistPayload.Id}";
                                }
                            }
                        }
                        catch { }

                        break;
                    case Services.HohoemaPageType.FollowManage:
                        break;
                    case Services.HohoemaPageType.WatchHistory:
                        break;
                    case Services.HohoemaPageType.Search:
                        break;
                    case Services.HohoemaPageType.SearchSummary:
                        break;
                    case Services.HohoemaPageType.SearchResultCommunity:
                        try
                        {
                            var content = SearchPagePayload.FromParameterString<CommunitySearchPagePayloadContent>(pin.Parameter);
                            if (content != null)
                            {
                                pin.Parameter = $"keyword={System.Net.WebUtility.UrlEncode(content.Keyword)}&target={content.SearchTarget}";
                            }
                        }
                        catch { }
                        break;
                    case Services.HohoemaPageType.SearchResultTag:
                        try
                        {
                            var content = SearchPagePayload.FromParameterString<TagSearchPagePayloadContent>(pin.Parameter);
                            if (content != null)
                            {
                                pin.Parameter = $"keyword={System.Net.WebUtility.UrlEncode(content.Keyword)}&target={content.SearchTarget}";
                            }
                        }
                        catch { }
                        break;
                    case Services.HohoemaPageType.SearchResultKeyword:
                        try
                        {
                            var content = SearchPagePayload.FromParameterString<KeywordSearchPagePayloadContent>(pin.Parameter);
                            if (content != null)
                            {
                                pin.Parameter = $"keyword={System.Net.WebUtility.UrlEncode(content.Keyword)}&target={content.SearchTarget}";
                            }
                        }
                        catch { }
                        break;
                    case Services.HohoemaPageType.SearchResultMylist:
                        try
                        {
                            var content = SearchPagePayload.FromParameterString<MylistSearchPagePayloadContent>(pin.Parameter);
                            if (content != null)
                            {
                                pin.Parameter = $"keyword={System.Net.WebUtility.UrlEncode(content.Keyword)}&target={content.SearchTarget}";
                            }
                        }
                        catch { }
                        break;
                    case Services.HohoemaPageType.SearchResultLive:
                        try
                        {
                            var content = SearchPagePayload.FromParameterString<LiveSearchPagePayloadContent>(pin.Parameter);
                            if (content != null)
                            {
                                pin.Parameter = $"keyword={System.Net.WebUtility.UrlEncode(content.Keyword)}&target={content.SearchTarget}";
                            }
                        }
                        catch { }
                        break;
                    case Services.HohoemaPageType.FeedGroupManage:
                        break;
                    case Services.HohoemaPageType.FeedGroup:
                        break;
                    case Services.HohoemaPageType.FeedVideoList:
                        break;
                    case Services.HohoemaPageType.UserInfo:
                        pin.Parameter = $"id={pin.Parameter}";
                        break;
                    case Services.HohoemaPageType.UserVideo:
                        pin.Parameter = $"id={pin.Parameter}";
                        break;
                    case Services.HohoemaPageType.Community:
                        pin.Parameter = $"id={pin.Parameter}";
                        break;
                    case Services.HohoemaPageType.CommunityVideo:
                        pin.Parameter = $"id={pin.Parameter}";
                        break;
                    case Services.HohoemaPageType.VideoInfomation:
                        pin.Parameter = $"id={pin.Parameter}";
                        break;
                    case Services.HohoemaPageType.CacheManagement:
                        break;
                    case Services.HohoemaPageType.Settings:
                        break;
                    case Services.HohoemaPageType.Splash:
                        break;
                    case Services.HohoemaPageType.VideoPlayer:
                        break;
                    case Services.HohoemaPageType.NicoRepo:
                        break;
                    case Services.HohoemaPageType.Recommend:
                        break;
                    case Services.HohoemaPageType.ChannelInfo:
                        pin.Parameter = $"id={pin.Parameter}";
                        break;
                    case Services.HohoemaPageType.ChannelVideo:
                        pin.Parameter = $"id={pin.Parameter}";
                        break;
                    case Services.HohoemaPageType.PrologueIntroduction:
                        break;
                    case Services.HohoemaPageType.NicoAccountIntroduction:
                        break;
                    case Services.HohoemaPageType.VideoCacheIntroduction:
                        break;
                    case Services.HohoemaPageType.EpilogueIntroduction:
                        break;
                    case Services.HohoemaPageType.LiveInfomation:
                        pin.Parameter = $"id={pin.Parameter}";
                        break;
                    case Services.HohoemaPageType.Timeshift:
                        break;
                    case Services.HohoemaPageType.Subscription:
                        break;
                    default:
                        break;
                }
            }

            pinSettings.IsMigrated_Prism6_to_Prism7 = true;
            _ = pinSettings.Save();
        }
    }
}

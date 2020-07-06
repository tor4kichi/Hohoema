using Hohoema.Models.Niconico;
using Mntone.Nico2.Videos.Recommend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.NicoVideo.Recommend
{
    public sealed class LoginUserRecommendProvider : ProviderBase
    {
        public LoginUserRecommendProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<RecommendResponse> GetRecommendFirstAsync()
        {
            var res = await ContextActionAsync(async context =>
            {
                return await context.Video.GetRecommendFirstAsync();
            });

            return new RecommendResponse(res);
            
        }

        public async Task<RecommendContent> GetRecommendAsync(RecommendResponse res, RecommendContent prevInfo = null)
        {
            var user_tags = res.UserTagParam;
            var seed = res.Seed;
            var page = prevInfo?.RecommendInfo.Page ?? res.Page;
            var content = await ContextActionAsync(async context =>
            {
                return await context.Video.GetRecommendAsync(user_tags, seed, page);
            });

            return new RecommendContent(content);
        }
    }



    public class RecommendInfo
    {
        private Mntone.Nico2.Videos.Recommend.RecommendInfo _recommendInfo;

        public RecommendInfo(Mntone.Nico2.Videos.Recommend.RecommendInfo recommendInfo)
        {
            _recommendInfo = recommendInfo;
        }

        public int Seed => _recommendInfo.Seed;

        public int Page => _recommendInfo.Page;

        public bool EndOfRecommend => _recommendInfo.EndOfRecommend;
    }

    public class Sherlock
    {
        private Mntone.Nico2.Videos.Recommend.Sherlock _sherlock;

        public Sherlock(Mntone.Nico2.Videos.Recommend.Sherlock sherlock)
        {
            _sherlock = sherlock;
        }

        public string Tag => _sherlock.Tag;
    }

    public class AdditionalInfo
    {
        private Mntone.Nico2.Videos.Recommend.AdditionalInfo _additionalInfo;

        public AdditionalInfo(Mntone.Nico2.Videos.Recommend.AdditionalInfo additionalInfo)
        {
            _additionalInfo = additionalInfo;
        }

        Sherlock _Sherlock;
        public Sherlock Sherlock => _Sherlock ??= new Sherlock(_additionalInfo.Sherlock);
    }

    public class RecommendItem
    {
        private Mntone.Nico2.Videos.Recommend.Item _item;

        public RecommendItem(Mntone.Nico2.Videos.Recommend.Item item)
        {
            _item = item;
        }

        public string ItemType => _item.ItemType;

        public string Id => _item.Id;

        public string ThumbnailUrl => _item.ThumbnailUrl;

        string _title;
        public string Title => _title ??= _item.ParseTitle();
        public string TitleShort => _item.TitleShort;

        public int ViewCounter => _item.ViewCounter;

        public int NumRes => _item.NumRes;

        public int MylistCounter => _item.MylistCounter;

        DateTime? _FirstRetrieve;
        public DateTime FirstRetrieve => _FirstRetrieve ??= _item.ParseForstRetroeveToDateTimeOffset().DateTime;



        TimeSpan? _length;
        public TimeSpan Length => _length ??= _item.ParseLengthToTimeSpan();

        public bool IsOriginalLanguage => _item.IsOriginalLanguage;

        public bool IsTranslated => _item.IsTranslated;

        AdditionalInfo _AdditionalInfo;
        public AdditionalInfo AdditionalInfo => _AdditionalInfo ??= new AdditionalInfo(_item.AdditionalInfo);
    }

    public class RecommendContent
    {
        private Mntone.Nico2.Videos.Recommend.RecommendContent _content;

        public RecommendContent(Mntone.Nico2.Videos.Recommend.RecommendContent content)
        {
            _content = content;
        }

        RecommendInfo _RecommendInfo;
        public RecommendInfo RecommendInfo => _RecommendInfo ??= new RecommendInfo(_content.RecommendInfo);

        IReadOnlyList<RecommendItem> _Items;
        public IReadOnlyList<RecommendItem> Items => _Items ??= _content.Items?.Select(item => new RecommendItem(item)).ToList();

        public string Status => _content.Status;

        public bool IsOK => Status == "ok";


    }

    public class RecommendResponse
    {
        private Mntone.Nico2.Videos.Recommend.RecommendResponse _res;

        public RecommendResponse(Mntone.Nico2.Videos.Recommend.RecommendResponse res)
        {
            _res = res;
        }

        public int Seed => _res.Seed;

        public int Page => _res.Page;

        public string UserTagParam => _res.UserTagParam;

        public object CompiledTpl => _res.CompiledTpl;

        RecommendContent _firstData;
        public RecommendContent FirstData => _firstData ??= new RecommendContent(_res.FirstData);
    }

}

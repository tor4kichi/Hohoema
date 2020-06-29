using Hohoema.Models.Niconico;
using Mntone.Nico2.Videos.Recommend;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico
{
    public sealed class LoginUserRecommendProvider : ProviderBase
    {
        public LoginUserRecommendProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<RecommendResponse> GetRecommendFirstAsync()
        {
            return await ContextActionAsync(async context =>
            {
                return await context.Video.GetRecommendFirstAsync();
            });
            
        }

        public async Task<RecommendContent> GetRecommendAsync(RecommendResponse res, RecommendContent prevInfo = null)
        {
            var user_tags = res.UserTagParam;
            var seed = res.Seed;
            var page = prevInfo?.RecommendInfo.Page ?? res.Page;
            return await ContextActionAsync(async context =>
            {
                return await context.Video.GetRecommendAsync(user_tags, seed, page);
            });
        }
    }

}

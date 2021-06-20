using NiconicoToolkit.Mylist.LoginUser;
using NiconicoToolkit.User;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
#if WINDOWS_UWP
using Windows.Web;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Windows.Web.Http.Headers;
#else
using System.Net;
using System.Net.Http;
#endif

namespace NiconicoToolkit.Mylist
{
    public sealed class MylistClient
    {
        private readonly NiconicoContext _context;
        private readonly JsonSerializerOptions _defaultOptions;

        public LoginUserMylistSubClient LoginUser { get; }

        public MylistClient(NiconicoContext context, JsonSerializerOptions defaultOptions)
        {
            _context = context;
            _defaultOptions = defaultOptions;
            LoginUser = new LoginUserMylistSubClient(context, defaultOptions);
        }

        internal static class Urls
        {
            public const string NvapiV2MylistApiUrl = $"{NiconicoUrls.NvApiV2Url}mylists/";
        }



        public Task<GetUserMylistGroupsResponse> GetUserMylistGroupsAsync(UserId userId, int sampleItemCount = 0)
        {
            return _context.GetJsonAsAsync<GetUserMylistGroupsResponse>($"{NiconicoUrls.NvApiV1Url}users/{userId}/mylists?sampleItemCount={sampleItemCount}", _defaultOptions);
        }


        public Task<GetMylistItemsResponse> GetMylistItemsAsync(string mylistId, int? page = null, int? pageSize = null, MylistSortKey? sortKey = null, MylistSortOrder? sortOrder = null)
        {
            var dict = new NameValueCollection();
            dict.AddIfNotNull<int>("page", page is null ? null : (page.Value + 1));
            dict.AddIfNotNull("pageSize", pageSize);
            dict.AddEnumIfNotNullWithDescription("sortKey", sortKey);
            dict.AddEnumIfNotNullWithDescription("sortOrder", sortOrder);

            var url = new StringBuilder(Urls.NvapiV2MylistApiUrl)
                .Append(mylistId)
                .AppendQueryString(dict)
                .ToString();

            return _context.GetJsonAsAsync<GetMylistItemsResponse>(url, _defaultOptions);
        }
    }
}

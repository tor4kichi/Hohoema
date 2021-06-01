using NiconicoToolkit.Mylist.LoginUser;
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

        public MylistClient(NiconicoContext context)
        {
            _context = context;

            _defaultOptions = new JsonSerializerOptions()
            { 
                Converters =
                {
                    new JsonStringEnumMemberConverter(),
                }
            };

            LoginUser = new LoginUserMylistSubClient(context);
        }

        
        public async Task<GetUserMylistGroupsResponse> GetUserMylistGroupsAsync(string userId, int sampleItemCount = 0)
        {
            var url = $"https://nvapi.nicovideo.jp/v1/users/{userId}/mylists?sampleItemCount={sampleItemCount}";

            return await _context.GetJsonAsAsync<GetUserMylistGroupsResponse>(url, _defaultOptions);
        }


        public Task<GetMylistItemsResponse> GetMylistItemsAsync(string mylistId, int? page = null, int? pageSize = null, MylistSortKey? sortKey = null, MylistSortOrder? sortOrder = null)
        {
            var dict = new NameValueCollection();
            if (page is not null) dict.Add("page", (page.Value + 1).ToString());
            if (pageSize is not null) dict.Add("pageSize", pageSize.Value.ToString());
            if (sortKey is not null) dict.Add("sortKey", sortKey.Value.GetDescription());
            if (sortOrder is not null) dict.Add("sortOrder", sortOrder.Value.GetDescription());

            var url = new StringBuilder(" https://nvapi.nicovideo.jp/v2/mylists/")
                .Append(mylistId)
                .AppendQueryString(dict)
                .ToString();

            return _context.GetJsonAsAsync<GetMylistItemsResponse>(url, _defaultOptions);
        }
    }
}

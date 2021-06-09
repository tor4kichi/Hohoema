using NiconicoToolkit.Account;
using NiconicoToolkit.Video;
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


namespace NiconicoToolkit.Mylist.LoginUser
{



    public sealed class LoginUserMylistSubClient
    {
        private readonly NiconicoContext _context;
        private readonly JsonSerializerOptions _defaultOptions;

        public LoginUserMylistSubClient(NiconicoContext context)
        {
            _context = context;

            _defaultOptions = new JsonSerializerOptions()
            {
                Converters =
                {
                    new JsonStringEnumMemberConverter(),
                }
            };

        }

        public Task<LoginUserMylistsResponse> GetMylistGroupsAsync(int sampleItemCount = 0)
        {
            return _context.GetJsonAsAsync<LoginUserMylistsResponse>($"https://nvapi.nicovideo.jp/v1/users/me/mylists?sampleItemCount={sampleItemCount}", _defaultOptions);
        }



        public async Task<CreateMylistResponse> CreateMylistAsync(string name, string description, bool isPublic, MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            return await _context.PostJsonAsAsync<CreateMylistResponse>("https://nvapi.nicovideo.jp/v1/users/me/mylists", new Dictionary<string, string>()
            {
                { "name", name },
                { "description", description },
                { "isPublic", isPublic.ToString() },
                { "defaultSortKey", sortKey.GetDescription() },
                { "defaultSortOrder", sortOrder.GetDescription() },
            });
        }

        public async Task<bool> UpdateMylistAsync(string mylistId, string name, string description, bool isPublic, MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            var dict = new Dictionary<string, string>()
            {
                { "name", name },
                { "description", description },
                { "isPublic", isPublic.ToString() },
                { "defaultSortKey", sortKey.GetDescription() },
                { "defaultSortOrder", sortOrder.GetDescription() },
            };
            var httpContent = new HttpFormUrlEncodedContent(dict);
            var res = await _context.SendAsync(HttpMethod.Put, $"https://nvapi.nicovideo.jp/v1/users/me/mylists/{mylistId}", httpContent);
            return res.IsSuccessStatusCode;
        }


        public async Task<bool> RemoveMylistAsync(string mylistId)
        {
            var res = await _context.SendAsync(HttpMethod.Delete, $"https://nvapi.nicovideo.jp/v1/users/me/mylists/{mylistId}");
            return res.IsSuccessStatusCode;
        }

        public async Task<ChangeMylistGroupsOrderResponse> ChangeMylistGroupsOrderAsync<T>(IEnumerable<T> orderedMylistIds)
        {
            var httpContent = new HttpFormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "order", string.Join(',', orderedMylistIds) }
            });

            var res = await _context.SendAsync(HttpMethod.Put, "https://nvapi.nicovideo.jp/v1/users/me/mylists/order", httpContent);
            return await res.Content.ReadAsAsync<ChangeMylistGroupsOrderResponse>(_defaultOptions);
        }


        public sealed class ChangeMylistGroupsOrderResponse : ResponseWithMeta
        {
            [JsonPropertyName("data")]
            public ChangeMylistGroupsOrderData Data { get; set; }
        }


        public sealed class ChangeMylistGroupsOrderData
        {
            [JsonPropertyName("mylistIds")]
            public string[] MylistIds { get; set; }
        }





        #region WatchAfter

        public Task<WatchAfterItemsResponse> GetWatchAfterItemsAsync(int? page = null, int? pageSize = null, MylistSortKey? sortKey = null, MylistSortOrder? sortOrder = null)
        {
            var dict = new NameValueCollection();
            if (page is not null) dict.Add("page", (page.Value + 1).ToString());
            if (pageSize is not null) dict.Add("pageSize", pageSize.Value.ToString());
            if (sortKey is not null) dict.Add("sortKey", sortKey.Value.GetDescription());
            if (sortOrder is not null) dict.Add("sortOrder", sortOrder.Value.GetDescription());

            var uri = new StringBuilder("https://nvapi.nicovideo.jp/v1/users/me/watch-later")
                .AppendQueryString(dict)
                .ToString();
            return _context.GetJsonAsAsync<WatchAfterItemsResponse>(uri, _defaultOptions);
        }

        public async Task<ContentManageResult> AddWatchAfterMylistItemAsync(string watchId, string memo)
        {
            var res = await _context.PostAsync("https://nvapi.nicovideo.jp/v1/users/me/watch-later", new Dictionary<string, string>
            {
                { "watchId", watchId },
                { "memo", memo }
            });

            return res.StatusCode.ToContentManagerResult();
        }

        public async Task<ContentManageResult> UpdateWatchAfterMylistItemMemoAsync(string itemId, string memo)
        {
            Dictionary<string, string> dict = new()
            {
                { "description", memo }
            };

            var url = new StringBuilder("https://nvapi.nicovideo.jp/v1/users/me/deflist/items/")
                .Append(itemId)
                .ToString();

            var res = await _context.PostAsync(url, dict);
            return res.StatusCode.ToContentManagerResult();
        }


        public async Task<ContentManageResult> RemoveWatchAfterItemsAsync(IEnumerable<string> itemIds)
        {
            NameValueCollection dict = new NameValueCollection()
            {
                { "itemIds", string.Join(',', itemIds) }
            };

            var url = new StringBuilder("https://nvapi.nicovideo.jp/v1/users/me/watch-later")
                .AppendQueryString(dict)
                .ToString();

            var res = await _context.SendAsync(HttpMethod.Delete, url);
            return res.StatusCode.ToContentManagerResult();
        }


        public async Task<MoveOrCopyMylistItemsResponse> MoveMylistItemsFromWatchAfterAsync(string mylistId, IEnumerable<string> itemIds)
        {
            var res = await MoveMylistItemsAsync_Internal(from: "deflist", to: mylistId, itemIds);
            return await res.Content.ReadAsAsync<MoveOrCopyMylistItemsResponse>();
        }

        public async Task<MoveOrCopyMylistItemsResponse> CopyMylistItemsFromWatchAfterAsync(string mylistId, IEnumerable<string> itemIds)
        {
            var res = await CopyMylistItemsAsync_Internal(from: "deflist", to: mylistId, itemIds);
            return await res.Content.ReadAsAsync<MoveOrCopyMylistItemsResponse>();
        }

        #endregion


        public Task<GetMylistItemsResponse> GetMylistItemsAsync(string mylistId, int? page = null, int? pageSize = null, MylistSortKey? sortKey = null, MylistSortOrder? sortOrder = null)
        {
            var dict = new NameValueCollection();
            if (page is not null) dict.Add("page", (page.Value + 1).ToString());
            if (pageSize is not null) dict.Add("pageSize", pageSize.Value.ToString());
            if (sortKey is not null) dict.Add("sortKey", sortKey.Value.GetDescription());
            if (sortOrder is not null) dict.Add("sortOrder", sortOrder.Value.GetDescription());

            var uri = new StringBuilder("https://nvapi.nicovideo.jp/v1/users/me/mylists/")
                .Append(mylistId)
                .AppendQueryString(dict)
                .ToString();
            return _context.GetJsonAsAsync<GetMylistItemsResponse>(uri, _defaultOptions);
        }




        public async Task<ContentManageResult> AddMylistItemAsync(string mylistId, string itemId, string memo)
        {
            NameValueCollection dict = new NameValueCollection()
            {
                { "itemId", itemId },
                { "description", memo }
            };

            var url = new StringBuilder("https://nvapi.nicovideo.jp/v1/users/me/mylists/")
                .Append(mylistId)
                .Append("/items")
                .AppendQueryString(dict)
                .ToString();

            var res = await _context.PostAsync(url);
            return res.StatusCode.ToContentManagerResult();
        }


        public async Task<ContentManageResult> UpdateMylistItemMemoAsync(string mylistId, string itemId, string memo)
        {
            NameValueCollection dict = new NameValueCollection()
            {
                { "description", memo }
            };

            var url = new StringBuilder("https://nvapi.nicovideo.jp/v1/users/me/mylists/")
                .Append(mylistId)
                .Append("/items/")
                .Append(itemId)
                .AppendQueryString(dict)
                .ToString();

            var res = await _context.PostAsync(url);
            return res.StatusCode.ToContentManagerResult();
        }

        public async Task<ContentManageResult> RemoveMylistItemsAsync(string mylistId, IEnumerable<string> itemIds)
        {
            NameValueCollection dict = new NameValueCollection()
            {
                { "itemIds", string.Join(',', itemIds) }
            };

            var url = new StringBuilder("https://nvapi.nicovideo.jp/v1/users/me/mylists/")
                .Append(mylistId)
                .Append("/items")
                .AppendQueryString(dict)
                .ToString();

            var res = await _context.SendAsync(HttpMethod.Delete, url);
            return res.StatusCode.ToContentManagerResult();
        }

        public async Task<MoveOrCopyMylistItemsResponse> MoveMylistItemsAsync(string fromMylistId, string toMylistId, IEnumerable<string> itemIds)
        {
            var res = await MoveMylistItemsAsync_Internal(from: fromMylistId, to: toMylistId, itemIds);
            return await res.Content.ReadAsAsync<MoveOrCopyMylistItemsResponse>();
        }

        public async Task<MoveOrCopyMylistItemsResponse> CopyMylistItemsAsync(string fromMylistId, string toMylistId, IEnumerable<string> itemIds)
        {
            var res = await CopyMylistItemsAsync_Internal(from: fromMylistId, to: toMylistId, itemIds);
            return await res.Content.ReadAsAsync<MoveOrCopyMylistItemsResponse>();
        }

        private Task<HttpResponseMessage> MoveMylistItemsAsync_Internal(string from, string to, IEnumerable<string> itemIds)
        {
            NameValueCollection dict = new NameValueCollection()
            {
                { "from", from },
                { "to", to },
                { "itemIds", string.Join(',', itemIds) }
            };

            var url = new StringBuilder("https://nvapi.nicovideo.jp/v1/users/me/move-mylist-items")
                .AppendQueryString(dict)
                .ToString();

            return _context.PostAsync(url);
        }

        private Task<HttpResponseMessage> CopyMylistItemsAsync_Internal(string from, string to, IEnumerable<string> itemIds)
        {
            NameValueCollection dict = new NameValueCollection()
            {
                { "from", from },
                { "to", to },
                { "itemIds", string.Join(',', itemIds) }
            };

            var url = new StringBuilder("https://nvapi.nicovideo.jp/v1/users/me/copy-mylist-items")
                .AppendQueryString(dict)
                .ToString();

            return _context.PostAsync(url);
        }

    }

}

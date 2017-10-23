using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Services.Twitter;
using Windows.Storage;

namespace NicoPlayerHohoema.Helpers
{
	public class TwitterHelper
	{
		public static Uri TwitterKeyAssetUri = new Uri("ms-appx:///Assets/OAuth/twitter.json");



		public static bool IsAvailableTwitterService = false;

		public static TwitterUser TwitterUser { get; private set; }

		public static bool IsLoggedIn => TwitterUser != null;



		public static async Task Initialize()
		{
			var key = await GetTwitterOAuthTokens();

			IsAvailableTwitterService = TwitterService.Instance.Initialize(key);
		}



		public static async Task<TwitterOAuthTokens> GetTwitterOAuthTokens()
		{
			var file = await StorageFile.GetFileFromApplicationUriAsync(TwitterKeyAssetUri);
			var jsonText = await FileIO.ReadTextAsync(file);
			var data = Newtonsoft.Json.JsonConvert.DeserializeObject<TwitterAuthData>(jsonText);


			return new TwitterOAuthTokens()
			{
				ConsumerKey = data.ConsumerKey,
				ConsumerSecret = data.ConsumerSecret,
				AccessToken = data.AccessToken,
				AccessTokenSecret = data.AccessTokenSecret,
				CallbackUri = data.CallbackUrl
			};
		}



		public static async Task<bool> LoginOrRefreshToken()
		{
			if (!IsAvailableTwitterService) { return false; }
			
			var result = false;
			if (result = await TwitterService.Instance.LoginAsync())
			{
				var user = await TwitterService.Instance.GetUserAsync();

				TwitterUser = user;
			}

			return result;
		}



		public static void Logout()
		{
			if (!IsAvailableTwitterService) { return; }

			TwitterUser = null;
			TwitterService.Instance.Logout();
		}



		public static Task<bool> SubmitTweet(string message/*, StorageFile[] imageFiles = null*/)
		{
			if (!IsAvailableTwitterService) { return Task.FromResult(false); }

			return TwitterService.Instance.TweetStatusAsync(message);
		}
	}

	[DataContract]
	public struct TwitterAuthData
	{
		[DataMember(Name = "consumer_key")]
		public string ConsumerKey { get; set; }

		[DataMember(Name = "consumer_secret")]
		public string ConsumerSecret { get; set; }

		[DataMember(Name = "access_token")]
		public string AccessToken { get; set; }

		[DataMember(Name = "access_token_secret")]
		public string AccessTokenSecret { get; set; }

		[DataMember(Name = "callback_url")]
		public string CallbackUrl { get; set; }
	}
}

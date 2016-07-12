using Mntone.Nico2;
using Mntone.Nico2.Videos.Thumbnail;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace NicoPlayerHohoema.Models
{
	public class HohoemaApp : BindableBase, IDisposable
	{
		public static async Task<HohoemaApp> Create(IEventAggregator ea)
		{
			var app = new HohoemaApp(ea);

			app.UserSettings = new HohoemaUserSettings();
			app.ContentFinder = new NiconicoContentFinder(app);

			return app;
		}


		private HohoemaApp(IEventAggregator ea)
		{
			EventAggregator = ea;
			
			FavFeedManager = null;
			CurrentAccount = new AccountSettings();

			LoadRecentLoginAccount();
		}

		public void LoadRecentLoginAccount()
		{
			if (ApplicationData.Current.LocalSettings.Containers.ContainsKey(RECENT_LOGIN_ACCOUNT))
			{
				// load
				var container = ApplicationData.Current.LocalSettings.Containers[RECENT_LOGIN_ACCOUNT];
				var prop = container.Values.FirstOrDefault();
				CurrentAccount.MailOrTelephone = prop.Key ?? "";

				CurrentAccount.Password = prop.Value as string ?? "";
			}
			else
			{
			}
		}

		public void SaveAccount(bool isRemenberPassword)
		{
			ApplicationDataContainer container = null;
			if (ApplicationData.Current.LocalSettings.Containers.ContainsKey(RECENT_LOGIN_ACCOUNT))
			{
				container = ApplicationData.Current.LocalSettings.Containers[RECENT_LOGIN_ACCOUNT];
			}
			else
			{
				container = ApplicationData.Current.LocalSettings.CreateContainer(RECENT_LOGIN_ACCOUNT, ApplicationDataCreateDisposition.Always);
			}

			var id = CurrentAccount.MailOrTelephone;
			var password = isRemenberPassword ? CurrentAccount.Password : "";
			container.Values[id] = password;

		}





		public async Task LoadUserSettings(string userId)
		{
			var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(userId, CreationCollisionOption.OpenIfExists);
			UserSettings = await HohoemaUserSettings.LoadSettings(folder);
		}

		public async Task SaveUserSettings()
		{
			await UserSettings?.Save();
		}

		public async Task<NiconicoSignInStatus> SignInFromUserSettings()
		{
			if (CurrentAccount == null)
			{
				return NiconicoSignInStatus.Failed;
			}

			if (CurrentAccount.IsValidMailOreTelephone && CurrentAccount.IsValidPassword)
			{
				return await SignIn(CurrentAccount.MailOrTelephone, CurrentAccount.Password);
			}
			else
			{
				return NiconicoSignInStatus.Failed;
			}
		}


		public async Task<NiconicoSignInStatus> SignIn(string mailOrTelephone, string password)
		{
			await SignOut();

			var context = new NiconicoContext(new NiconicoAuthenticationToken(mailOrTelephone, password));

			context.AdditionalUserAgent = HohoemaUserAgent;

			Debug.WriteLine("try login");

			var result = await context.SignInAsync();

			if (result == NiconicoSignInStatus.Success)
			{
				Debug.WriteLine("login success");

				NiconicoContext = context;

				Debug.WriteLine("start post login process....");

				Debug.WriteLine("getting UserInfo");
				var userInfo = await NiconicoContext.User.GetInfoAsync();
				LoginUserId = userInfo.Id;
				IsPremiumUser = userInfo.IsPremium;

				Debug.WriteLine("user id is : " + LoginUserId);
				Debug.WriteLine("initilize: user settings ");
				await LoadUserSettings(LoginUserId.ToString());

				Debug.WriteLine("initilize: local cache ");
				MediaManager = await NiconicoMediaManager.Create(this);

				Debug.WriteLine("initilize: fav");
				FavFeedManager = await FavFeedManager.Create(this, LoginUserId);

				Debug.WriteLine("Login done.");
				//				await MediaManager.Context.Resume();

				OnSignin?.Invoke();
			}
			else
			{
				Debug.WriteLine("login failed");
			}

			return result;
		}

		public async Task<NiconicoSignInStatus> SignOut()
		{
			if (NiconicoContext == null)
			{
				return NiconicoSignInStatus.Failed;
			}

			var result = await NiconicoContext.SignOutOffAsync();
			NiconicoContext.Dispose();

			NiconicoContext = null;
			FavFeedManager = null;
			await SaveUserSettings();
			UserSettings = null;
			LoginUserId = uint.MaxValue;

			OnSignout?.Invoke();

			return result;
		}

		public async Task<NiconicoSignInStatus> CheckSignedInStatus()
		{
			if (NiconicoContext != null)
			{
				return await NiconicoContext.GetIsSignedInAsync();
			}
			else
			{
				return NiconicoSignInStatus.Failed;
			}
		}

		private async Task<StorageFolder> GetCurrentUserFolder()
		{
			return await ApplicationData.Current.LocalFolder.CreateFolderAsync(LoginUserId.ToString(), CreationCollisionOption.OpenIfExists);
		}

		public async Task<StorageFolder> GetCurrentUserVideoFolder()
		{
			var userFolder = await GetCurrentUserFolder();
			return await userFolder.CreateFolderAsync("video", CreationCollisionOption.OpenIfExists);
		}

		public async Task<StorageFolder> GetCurrentUserFavFolder()
		{
			var userFolder = await GetCurrentUserFolder();
			return await userFolder.CreateFolderAsync("fav", CreationCollisionOption.OpenIfExists);
		}


		public void Dispose()
		{
			MediaManager?.Dispose();
		}

		public HohoemaUserSettings UserSettings { get; private set; }

		public uint LoginUserId { get; private set; }
		public bool IsPremiumUser { get; private set; }

		private NiconicoContext _NiconicoContext;
		public NiconicoContext NiconicoContext
		{
			get { return _NiconicoContext; }
			set { SetProperty(ref _NiconicoContext, value); }
		}

		public NiconicoMediaManager MediaManager { get; private set; }

		public NiconicoContentFinder ContentFinder { get; private set; }

		private FavFeedManager _FavFeedManager;
		public FavFeedManager FavFeedManager
		{
			get { return _FavFeedManager; }
			set { SetProperty(ref _FavFeedManager, value); }
		}

		public const string HohoemaUserAgent = "Hohoema_UWP";

		public IEventAggregator EventAggregator { get; private set; }


		const string RECENT_LOGIN_ACCOUNT = "recent_login_account";
		public AccountSettings CurrentAccount { get; private set; }


		public event Action OnSignout;
		public event Action OnSignin;



	}


	public class NGResult
	{
		public NGReason NGReason { get; set; }
		public string NGDescription { get; set; } = "";
		public string Content { get; set; }

		internal string GetReasonText()
		{
			switch (NGReason)
			{
				case NGReason.VideoId:
					return $"NG対象の動画ID : {Content}";
				case NGReason.UserId:
					return $"NG対象の投稿者ID : {Content}";
				case NGReason.Keyword:
					return $"NG対象のキーワード : {Content}";
				default:
					throw new NotSupportedException();
			}
		}
	}

	public enum NGReason
	{
		VideoId,
		UserId,
		Keyword,
	}
}

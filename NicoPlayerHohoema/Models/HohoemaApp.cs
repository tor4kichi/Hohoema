using Mntone.Nico2;
using Mntone.Nico2.Videos.Thumbnail;
using NicoPlayerHohoema.Util;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Diagnostics;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Models
{
	public class HohoemaApp : BindableBase, IDisposable
	{
		public static CoreDispatcher UIDispatcher { get; private set; }
		
		// v0.3.9 以前との互換性のために残しています
		const string RECENT_LOGIN_ACCOUNT = "recent_login_account";

		const string PRIMARY_ACCOUNT = "primary_account";


		public static HohoemaApp Create(IEventAggregator ea)
		{
			var app = new HohoemaApp(ea);

			app.UserSettings = new HohoemaUserSettings();
			app.ContentFinder = new NiconicoContentFinder(app);
			app.UserMylistManager = new UserMylistManager(app);

			UIDispatcher = Window.Current.CoreWindow.Dispatcher;
			return app;
		}

		public readonly static Guid HohoemaLoggerGroupGuid = Guid.NewGuid();


		private SemaphoreSlim _SigninLock;
		private const string ThumbnailLoadBackgroundTaskId = "ThumbnailLoader";

		private HohoemaApp(IEventAggregator ea)
		{
			EventAggregator = ea;
			LoginUserId = uint.MaxValue;
			LoggingChannel = new LoggingChannel("HohoemaLog", new LoggingChannelOptions(HohoemaLoggerGroupGuid));

			FavManager = null;

			LoadRecentLoginAccount();
			ThumbnailBackgroundLoader = new BackgroundUpdater(ThumbnailLoadBackgroundTaskId);
			_SigninLock = new SemaphoreSlim(1, 1);
		}

		public void LoadRecentLoginAccount()
		{
			var vault = new Windows.Security.Credentials.PasswordVault();

			// v0.3.9 以前との互換性
			if (ApplicationData.Current.LocalSettings.Containers.ContainsKey(RECENT_LOGIN_ACCOUNT))
			{
				var container = ApplicationData.Current.LocalSettings.Containers[RECENT_LOGIN_ACCOUNT];
				var prop = container.Values.FirstOrDefault();

				var id = prop.Key;
				var password = prop.Value as string ?? "";

				try
				{
					AddOrUpdateAccount(id, password);
				}
				catch { }

				ApplicationData.Current.LocalSettings.DeleteContainer(RECENT_LOGIN_ACCOUNT);

				SetPrimaryAccountId(id);
			}

			
		}

		public static void SetPrimaryAccountId(string mailAddress)
		{
			var container = ApplicationData.Current.LocalSettings.CreateContainer(PRIMARY_ACCOUNT, ApplicationDataCreateDisposition.Always);
			container.Values["primary_id"] = mailAddress;
		}

		public static string GetPrimaryAccountId()
		{
			var container = ApplicationData.Current.LocalSettings.CreateContainer(PRIMARY_ACCOUNT, ApplicationDataCreateDisposition.Always);
			return container.Values["primary_id"] as string;
		}

		public static bool HasPrimaryAccount()
		{
			var container = ApplicationData.Current.LocalSettings.CreateContainer(PRIMARY_ACCOUNT, ApplicationDataCreateDisposition.Always);
			return container.Values["primary_id"] as string != null;
		}

		public static void AddOrUpdateAccount(string mailAddress, string password)
		{
			var id = mailAddress;
			
			if (String.IsNullOrWhiteSpace(mailAddress) || String.IsNullOrWhiteSpace(password))
			{
				throw new Exception();
			}

			var vault = new Windows.Security.Credentials.PasswordVault();
			try
			{
				var credential = vault.Retrieve(nameof(HohoemaApp), id);
				credential.Password = password;
			}
			catch
			{
				var credential = new Windows.Security.Credentials.PasswordCredential(nameof(HohoemaApp), id, password);
				vault.Add(credential);
			}
		}
		

		public static Tuple<string, string> GetPrimaryAccount()
		{
			if (HasPrimaryAccount())
			{
				var vault = new Windows.Security.Credentials.PasswordVault();
				try
				{
					var primary_id = GetPrimaryAccountId();
					var credential = vault.Retrieve(nameof(HohoemaApp), primary_id);
					credential.RetrievePassword();
					return new Tuple<string, string>(credential.UserName, credential.Password);
				}
				catch { }
			}

			return null;
		}

		public static List<string> GetAccountIds()
		{
			try
			{
				var vault = new Windows.Security.Credentials.PasswordVault();

				var items = vault.FindAllByResource(nameof(HohoemaApp));

				return items.Select(x => x.UserName)
					.ToList();
			}
			catch
			{
				
			}

			return new List<string>();
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

		public async Task<NiconicoSignInStatus> SignInWithPrimaryAccount()
		{
			// 資格情報からログインパラメータを取得
			string primaryAccount_id = null;
			string primaryAccount_Password = null;

			var account = GetPrimaryAccount();
			if (account != null)
			{
				primaryAccount_id = account.Item1;
				primaryAccount_Password = account.Item2;
			}

			if (String.IsNullOrWhiteSpace(primaryAccount_id) || String.IsNullOrWhiteSpace(primaryAccount_Password))
			{
				return NiconicoSignInStatus.Failed;
			}

			
			return await SignIn(primaryAccount_id, primaryAccount_Password);
		}

		/// <summary>
		/// Appから呼び出します
		/// 他の場所からは呼ばないようにしてください
		/// </summary>
		public void Resumed()
		{
			OnResumed?.Invoke();
		}

		public async Task Relogin()
		{
			if (NiconicoContext != null)
			{
				var context = new NiconicoContext(NiconicoContext.AuthenticationToken);

				if (await context.SignInAsync() == NiconicoSignInStatus.Success)
				{
					NiconicoContext = context;
				}
			}
		}


		public IAsyncOperation<NiconicoSignInStatus> SignIn(string mailOrTelephone, string password)
		{
			return AsyncInfo.Run<NiconicoSignInStatus>(async (cancelToken) => 
			{
				if (NiconicoContext != null 
				&& NiconicoContext.AuthenticationToken.MailOrTelephone == mailOrTelephone 
				&& NiconicoContext.AuthenticationToken.Password == password)
				{
					return NiconicoSignInStatus.Success;
				}

				await SignOut();

				try
				{
					await _SigninLock.WaitAsync();

					var context = new NiconicoContext(new NiconicoAuthenticationToken(mailOrTelephone, password));

					context.AdditionalUserAgent = HohoemaUserAgent;

					LoginErrorText = "";

					Debug.WriteLine("try login");

					NiconicoSignInStatus result = NiconicoSignInStatus.Failed;
					try
					{
						result = await context.SignInAsync();
					}
					catch
					{
						LoginErrorText = "サインインに失敗、再起動をお試しください";
						context?.Dispose();
					}

					if (result == NiconicoSignInStatus.Success)
					{
						Debug.WriteLine("login success");

						NiconicoContext = context;

						// バックグラウンド処理機能を生成
						BackgroundUpdater = new BackgroundUpdater("HohoemaBG1");


						using (var loginActivityLogger = LoggingChannel.StartActivity("login process"))
						{

							loginActivityLogger.LogEvent("begin login process.");

							var fields = new LoggingFields();

							try
							{
								loginActivityLogger.LogEvent("getting UserInfo.");
								var userInfo = await NiconicoContext.User.GetInfoAsync();

								LoginUserId = userInfo.Id;
								IsPremiumUser = userInfo.IsPremium;
								LoginUserName = userInfo.Name;

								fields.AddString("user id", LoginUserId.ToString());
								fields.AddString("user name", LoginUserName);
								fields.AddBoolean("is premium", IsPremiumUser);

								loginActivityLogger.LogEvent("[Success]:get UserInfo.", fields, LoggingLevel.Information);
							}
							catch
							{
								LoginErrorText = "[Failed]:get UserInfo.";

								fields.AddString("mail", mailOrTelephone);
								loginActivityLogger.LogEvent(LoginErrorText, fields, LoggingLevel.Warning);

								NiconicoContext.Dispose();
								NiconicoContext = null;
								return NiconicoSignInStatus.Failed;
							}

							fields.Clear();




							Debug.WriteLine("user id is : " + LoginUserId);

							try
							{
								loginActivityLogger.LogEvent("initialize user settings");
								await LoadUserSettings(LoginUserId.ToString());
							}
							catch
							{
								LoginErrorText = "[Failed]: load user settings failed.";
								Debug.WriteLine(LoginErrorText);
								loginActivityLogger.LogEvent(LoginErrorText, fields, LoggingLevel.Error);
								NiconicoContext.Dispose();
								NiconicoContext = null;
								return NiconicoSignInStatus.Failed;
							}



							try
							{
								Debug.WriteLine("initilize: fav");
								loginActivityLogger.LogEvent("initialize user favorite");
								FavManager = await FavManager.Create(this, LoginUserId);
							}
							catch
							{
								LoginErrorText = "[Failed] user favorite initialize failed.";
								Debug.WriteLine(LoginErrorText);
								loginActivityLogger.LogEvent(LoginErrorText, fields, LoggingLevel.Error);
								NiconicoContext.Dispose();
								NiconicoContext = null;
								return NiconicoSignInStatus.Failed;
							}


							try
							{
								Debug.WriteLine("initilize: feed");
								loginActivityLogger.LogEvent("initialize feed");

								FeedManager = new FeedManager(this, LoginUserId);
								await FeedManager.Initialize();
							}
							catch
							{
								LoginErrorText = "[Failed] user FavFeedUpdater initialize failed.";
								Debug.WriteLine(LoginErrorText);
								loginActivityLogger.LogEvent(LoginErrorText, fields, LoggingLevel.Error);
								NiconicoContext.Dispose();
								NiconicoContext = null;
								return NiconicoSignInStatus.Failed;
							}

							try
							{
								Debug.WriteLine("initilize: mylist");
								loginActivityLogger.LogEvent(LoginErrorText);
								await UserMylistManager.UpdateUserMylists();
							}
							catch
							{
								Debug.WriteLine(LoginErrorText = "[Failed] user mylist");
								loginActivityLogger.LogEvent("[Failed] user mylist", fields, LoggingLevel.Error);
								NiconicoContext.Dispose();
								NiconicoContext = null;
								return NiconicoSignInStatus.Failed;
							}



							try
							{
								Debug.WriteLine("initilize: local cache ");
								loginActivityLogger.LogEvent("initialize user local cache");
								MediaManager = await NiconicoMediaManager.Create(this);
							}
							catch
							{
								LoginErrorText = "[Failed] local cache initialize failed.";
								Debug.WriteLine(LoginErrorText);
								loginActivityLogger.LogEvent(LoginErrorText, fields, LoggingLevel.Error);
								NiconicoContext.Dispose();
								NiconicoContext = null;
								return NiconicoSignInStatus.Failed;
							}

							Debug.WriteLine("Login done.");
							loginActivityLogger.LogEvent("[Success]: Login done");
						}

						OnSignin?.Invoke();

						MediaManager.Context.StartBackgroundDownload();
					}
					else
					{
						Debug.WriteLine("login failed");
						context?.Dispose();
					}

					return result;
				}
				finally
				{
					_SigninLock.Release();
				}
				
			});
			
		}

		public async Task<NiconicoSignInStatus> SignOut()
		{
			try
			{
				await _SigninLock.WaitAsync();

				NiconicoSignInStatus result = NiconicoSignInStatus.Failed;
				if (NiconicoContext == null)
				{
					return result;
				}

				try
				{
					result = await NiconicoContext.SignOutOffAsync();

					NiconicoContext.Dispose();

					if (MediaManager != null && MediaManager.Context != null)
					{
						await MediaManager.Context.Suspending();
					}

					await SaveUserSettings();
					if (MediaManager != null)
					{
						await MediaManager.DeleteUnrequestedVideos();
					}

				}
				finally
				{
					NiconicoContext = null;
					FavManager = null;
					UserSettings = null;
					LoginUserId = uint.MaxValue;
					MediaManager = null;
					BackgroundUpdater?.Dispose();
					BackgroundUpdater = null;
					ThumbnailBackgroundLoader?.Dispose();
					ThumbnailBackgroundLoader = new BackgroundUpdater(ThumbnailLoadBackgroundTaskId);
					
					FavManager = null;
					FeedManager = null;

					OnSignout?.Invoke();
				}

				return result;
			}
			finally
			{
				_SigninLock.Release();
			}
		}

		public async Task<NiconicoSignInStatus> CheckSignedInStatus()
		{
			try
			{
				await _SigninLock.WaitAsync();


				if (NiconicoContext != null)
				{
					return await NiconicoContext.GetIsSignedInAsync();
				}
				else
				{
					return NiconicoSignInStatus.Failed;
				}
			}
			finally
			{
				_SigninLock.Release();
			}
		}

		public async Task<StorageFolder> GetCurrentUserFolder()
		{
			return await ApplicationData.Current.LocalFolder.CreateFolderAsync(LoginUserId.ToString(), CreationCollisionOption.OpenIfExists);
		}

		

		StorageFolder _DownloadFolder;

		public async Task<bool> IsAvailableUserDataFolder()
		{
			var folder = await GetCurrentUserDataFolder();

			return folder != null;
		}


		
		private async Task CacheFolderMigration(StorageFolder newFolder)
		{
			if (_DownloadFolder?.Path != newFolder.Path)
			{
				// フォルダーの移行作業を開始

				// 現在あるダウンロードタスクは必ず終了させる必要があります
				if (MediaManager != null && MediaManager.Context != null)
				{
					await MediaManager?.Context?.Suspending();
				}

				if (_DownloadFolder != null)
				{
					var oldSaveFolder = _DownloadFolder;
					var oldVideoSaveFolder = (await oldSaveFolder.TryGetItemAsync("video")) as StorageFolder;
					if (oldVideoSaveFolder != null)
					{
						var newVideoSaveFolder = await newFolder.CreateFolderAsync("video", CreationCollisionOption.OpenIfExists);
						await MoveFiles(oldVideoSaveFolder, newVideoSaveFolder);
					}

					var oldFavSaveFolder = (await oldSaveFolder.TryGetItemAsync("fav")) as StorageFolder;
					if (oldFavSaveFolder != null)
					{
						var newFavSaveFolder = await newFolder.CreateFolderAsync("fav", CreationCollisionOption.OpenIfExists);
						await MoveFiles(oldFavSaveFolder, newFavSaveFolder);
					}
				}



				#region v0.3.3 からの移行処理


				// Note: v0.3.3以前にLocalFolderに作成されていたフォルダが存在する場合、そのフォルダの内容を
				// _DownlaodFolderに移動させます

				{
					var oldVersionDataFolder = await GetCurrentUserFolder();
					var oldVideoSaveFolder = (await oldVersionDataFolder.TryGetItemAsync("video")) as StorageFolder;
					if (oldVideoSaveFolder != null)
					{
						var newVideoSaveFolder = await newFolder.CreateFolderAsync("video", CreationCollisionOption.OpenIfExists);
						await MoveFiles(oldVideoSaveFolder, newVideoSaveFolder);
					}

					var oldFavSaveFolder = (await oldVersionDataFolder.TryGetItemAsync("fav")) as StorageFolder;
					if (oldFavSaveFolder != null)
					{
						var newFavSaveFolder = await newFolder.CreateFolderAsync("fav", CreationCollisionOption.OpenIfExists);
						await MoveFiles(oldFavSaveFolder, newFavSaveFolder);
					}
				}

				#endregion


				// ダウンロードタスクを再開
				if (MediaManager != null && MediaManager.Context != null)
				{
					await MediaManager.Context.Resume();
				}
			}

			_DownloadFolder = newFolder;
		}


		private async Task MoveFiles(StorageFolder source, StorageFolder dest)
		{
			try
			{
				// ファイルを全部移動させて
				var files = await source.GetFilesAsync();
				foreach (var file in files)
				{
					try
					{
						await file.MoveAsync(dest);
					}
					catch { }
				}

				// 古いフォルダを削除
				var remainSomething = await source.GetItemsAsync();
				if (remainSomething.Count == 0)
				{
					await source.DeleteAsync(StorageDeleteOption.PermanentDelete);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
			}
		}

		public async Task<StorageFolder> ResetDefaultUserDataFolder()
		{
			var folderAccessToken = LoginUserId.ToString();

			// ユーザー名とランダムな英数字文字列から新しいフォルダアクセストークンを作成
			var folderName = LoginUserName.ToSafeDirectoryPath() + "_" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
			var folder = await DownloadsFolder.CreateFolderAsync(folderName, CreationCollisionOption.FailIfExists);
			Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace(folderAccessToken, folder);

			try
			{
				await CacheFolderMigration(folder);
			}
			catch
			{
			}

			return _DownloadFolder;
		}

		/*
		public async Task<StorageFolder> ChangeUserDataFolder()
		{
			var folderPicker = new Windows.Storage.Pickers.FolderPicker();
			folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
			folderPicker.FileTypeFilter.Add("*");

			var folder = await folderPicker.PickSingleFolderAsync();
			if (folder != null && folder.Path != _DownloadFolder?.Path)
			{
				var loginUserId = LoginUserId.ToString();
				Windows.Storage.AccessCache.StorageApplicationPermissions.
				FutureAccessList.AddOrReplace(loginUserId, folder);

				await CacheFolderMigration(folder);

				await UserSettings.CacheSettings.UserSelectedCacheFolder();
			}

			return _DownloadFolder;
		}
		*/

		public async Task<StorageFolder> GetCurrentUserDataFolder()
		{
			if (_DownloadFolder == null)
			{
				var folderAccessToken = LoginUserId.ToString();
				try
				{
					// v0.3.4以前のバージョンでの動作との整合性を取るためのコード
					// 以前はログインユーザーIDで

					if (Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.ContainsItem(folderAccessToken))
					{
						// 既にフォルダを指定済みの場合
						_DownloadFolder = await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFolderAsync(folderAccessToken);
					}
				}
				catch (Exception ex)
				{
					Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Remove(folderAccessToken);

					Debug.WriteLine(ex.ToString());
				}

				// フォルダが無い場合、デフォルトフォルダに作成
				if (_DownloadFolder == null)
				{
					await ResetDefaultUserDataFolder();
				}
			}

			return _DownloadFolder;
		}

		public async Task<StorageFolder> GetCurrentUserVideoDataFolder()
		{
			var folder = await GetCurrentUserDataFolder();

			if (folder == null) { return null; }

			return await folder.CreateFolderAsync("video", CreationCollisionOption.OpenIfExists);
		}

		public async Task<StorageFolder> GetCurrentUserFavDataFolder()
		{
			var folder = await GetCurrentUserDataFolder();

			if (folder == null) { return null; }

			return await folder.CreateFolderAsync("fav", CreationCollisionOption.OpenIfExists);
		}


		public void Dispose()
		{
			MediaManager?.Dispose();
			LoggingChannel?.Dispose();
		}


		public bool IsLoggedIn
		{
			get
			{
				return LoginUserId != uint.MaxValue;
			}
		}


		public HohoemaUserSettings UserSettings { get; private set; }

		public uint LoginUserId { get; private set; }
		public bool IsPremiumUser { get; private set; }
		public string LoginUserName { get; private set; }

		private NiconicoContext _NiconicoContext;
		public NiconicoContext NiconicoContext
		{
			get { return _NiconicoContext; }
			set { SetProperty(ref _NiconicoContext, value); }
		}

		public NiconicoMediaManager MediaManager { get; private set; }

		public NiconicoContentFinder ContentFinder { get; private set; }

		private FavManager _FavManager;
		public FavManager FavManager
		{
			get { return _FavManager; }
			set { SetProperty(ref _FavManager, value); }
		}

		private FeedManager _FeedManager;
		public FeedManager FeedManager
		{
			get { return _FeedManager; }
			set { SetProperty(ref _FeedManager, value); }
		}

		


		public UserMylistManager UserMylistManager { get; private set; }


		public const string HohoemaUserAgent = "Hohoema_UWP";

		public IEventAggregator EventAggregator { get; private set; }


		public BackgroundUpdater BackgroundUpdater { get; private set; }
		public BackgroundUpdater ThumbnailBackgroundLoader { get; private set; }


		public LoggingChannel LoggingChannel { get; private set; }


		public string LoginErrorText { get; private set; }

		public event Action OnSignout;
		public event Action OnSignin;
		public event Action OnResumed;



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
		Tag,
	}
}

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


		public static async Task<HohoemaApp> Create(IEventAggregator ea)
		{
			var app = new HohoemaApp(ea);

			app.UserSettings = new HohoemaUserSettings();
			await app.LoadUserSettings();
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

		#region SignIn/Out 


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


		public async Task LoadUserSettings()
		{
			var folder = ApplicationData.Current.LocalFolder;
			UserSettings = await HohoemaUserSettings.LoadSettings(folder);
		}

		/// <summary>
		/// ユーザーIDに基づいたユーザー設定を0.4.0以降のユーザー設定として移行します。
		/// すでに0.4.0環境のユーザー設定が存在する場合や
		/// ユーザーIDに基づいたユーザー設定が存在しない場合は何もしません。
		/// 読み込みに成功するとUserSettingsが上書き更新されます。
		/// ユーザーIDに基づいたユーザー設定はフォルダごと削除されます。
		/// </summary>
		/// <param name="userId"></param>
		/// <returns></returns>
		public async Task MigrateLegacyUserSettings(string userId)
		{
			var folder = await ApplicationData.Current.LocalFolder.TryGetItemAsync(userId) as StorageFolder;
			if (folder != null)
			{
				var fileAccessor = new FileAccessor<CacheSettings>(ApplicationData.Current.LocalFolder, HohoemaUserSettings.CacheSettingsFileName);
				if (false == await fileAccessor.ExistFile())
				{
					await MoveFiles(folder, ApplicationData.Current.LocalFolder);

					await LoadUserSettings();
				}
			}
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


							// 0.4.0以前のバージョンからのログインユーザー情報の移行処理
							await MigrateLegacyUserSettings(LoginUserId.ToString());


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



		#endregion



		
		

		StorageFolder _DownloadFolder;

		public async Task<bool> IsAvailableUserDataFolder()
		{
			var folder = await GetCurrentUserDataFolder();

			return folder != null;
		}


		
		private async Task CacheFolderMigration(StorageFolder newVideoFolder)
		{
			if (_DownloadFolder?.Path != newVideoFolder.Path)
			{
				// フォルダーの移行作業を開始

				// 現在あるダウンロードタスクは必ず終了させる必要があります
				if (MediaManager != null && MediaManager.Context != null)
				{
					await MediaManager?.Context?.Suspending();
				}

				// v0.4.0以降の移行処理
				if (_DownloadFolder != null)
				{
					await MoveFiles(_DownloadFolder, newVideoFolder);
				}


				// v0.3.9以前 からの移行処理
				if (_DownloadFolder != null)
				{
					var oldSaveFolder = _DownloadFolder;
					var oldVideoSaveFolder = (await oldSaveFolder.TryGetItemAsync("video")) as StorageFolder;
					if (oldVideoSaveFolder != null)
					{
						var newVideoSaveFolder = newVideoFolder;
						await MoveFiles(oldVideoSaveFolder, newVideoSaveFolder);
					}

					// DL/Hohoema/{Userid}/Fav/Feed の内容を AppData/Hohoema/LocalState/Feedに移動
					var oldFavSaveFolder = (await oldSaveFolder.TryGetItemAsync("fav")) as StorageFolder;
					if (oldFavSaveFolder != null)
					{
						var oldFeedSaveFolder = (await oldFavSaveFolder.TryGetItemAsync("feed")) as StorageFolder;
						if (oldFeedSaveFolder != null)
						{
							await MoveFiles(oldFeedSaveFolder, await GetFeedDataFolder());
						}
					}
				}



				#region v0.3.3 からの移行処理


				// Note: v0.3.3以前にLocalFolderに作成されていたフォルダが存在する場合、そのフォルダの内容を
				// _DownlaodFolderに移動させます

				{
					var oldVersionDataFolder = await GetLegacyUserDataFolder(LoginUserId.ToString());
					if (oldVersionDataFolder != null)
					{
						var oldVideoSaveFolder = (await oldVersionDataFolder.TryGetItemAsync("video")) as StorageFolder;
						if (oldVideoSaveFolder != null)
						{
							//						var newVideoSaveFolder = await newVideoFolder.CreateFolderAsync("video", CreationCollisionOption.OpenIfExists);
							await MoveFiles(oldVideoSaveFolder, newVideoFolder);
						}
					}

					// favの保存は v0.3.10 or v0.4.0からは廃止しています
					//					var oldFavSaveFolder = (await oldVersionDataFolder.TryGetItemAsync("fav")) as StorageFolder;
					//					if (oldFavSaveFolder != null)
					{
//						var newFavSaveFolder = await newVideoFolder.CreateFolderAsync("fav", CreationCollisionOption.OpenIfExists);
//						await MoveFiles(oldFavSaveFolder, newFavSaveFolder);
					}
				}

				#endregion


				try
				{
					await MediaManager.CheckAllNicoVideoCacheState();
				}
				catch (Exception ex) { Debug.WriteLine(ex.ToString()); }

				// ダウンロードタスクを再開
				if (MediaManager != null && MediaManager.Context != null)
				{
					await MediaManager.Context.Resume();
				}
			}

			_DownloadFolder = newVideoFolder;
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

		


		
		public async Task<bool> ChangeUserDataFolder()
		{
			var folderPicker = new Windows.Storage.Pickers.FolderPicker();
			folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
			folderPicker.FileTypeFilter.Add("*");
			
			var folder = await folderPicker.PickSingleFolderAsync();
			if (folder != null && folder.Path != _DownloadFolder?.Path)
			{
				try
				{
					await CacheFolderMigration(folder);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.ToString());
				}

				if (false == String.IsNullOrWhiteSpace(CurrentFolderAccessToken))
				{
					Windows.Storage.AccessCache.StorageApplicationPermissions.
					FutureAccessList.Remove(CurrentFolderAccessToken);
					CurrentFolderAccessToken = null;
				}

				Windows.Storage.AccessCache.StorageApplicationPermissions.
				FutureAccessList.AddOrReplace(FolderAccessToken, folder);

				_DownloadFolder = folder;

				CurrentFolderAccessToken = FolderAccessToken;

				return true;
			}
			else
			{
				return false;
			}
		}
		

		
		


		#region lagacy user data folder access

		private static async Task<StorageFolder> GetLegacyUserDataFolder(string loginUserId)
		{
			var folderAccessToken = loginUserId;
			try
			{
				// v0.3.9以前のバージョンでの動作との整合性を取るためのコード
				// 以前はログインユーザーIDで

				if (Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.ContainsItem(folderAccessToken))
				{
					// 既にフォルダを指定済みの場合
					return await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFolderAsync(folderAccessToken);
				}
			}
			catch (Exception ex)
			{
				Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Remove(folderAccessToken);

				Debug.WriteLine(ex.ToString());
			}

			return null;
		}

		private static async Task<bool> RemoveLegacyUserDataFolder(string loginUserId)
		{
			var folderAccessToken = loginUserId;
			try
			{
				// v0.3.9以前のバージョンでの動作との整合性を取るためのコード
				if (Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.ContainsItem(folderAccessToken))
				{
					var folder = await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFolderAsync(folderAccessToken);

					await folder.DeleteAsync();

					Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Remove(folderAccessToken);
					
					return true;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
			}

			return false;
		}


		#endregion


		const string FolderAccessToken = "HohoemaVideoCache";

		// 旧バージョンで指定されたフォルダーでも動くようにするためにFolderAccessTokenを動的に扱う
		// 0.4.0以降はFolderAccessTokenで指定したトークンだが、
		// それ以前では ログインユーザーIDをトークンとして DL/Hohoema/ログインユーザーIDフォルダ/ をDLフォルダとして指定していた
		string CurrentFolderAccessToken = null;

		private async Task<StorageFolder> GetEnsureVideoFolder()
		{
			if (_DownloadFolder == null)
			{
				try
				{
					// 既にフォルダを指定済みの場合
					if (Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.ContainsItem(FolderAccessToken))
					{
						_DownloadFolder = await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFolderAsync(FolderAccessToken);
						CurrentFolderAccessToken = FolderAccessToken;
					}
				}
				catch (FileNotFoundException)
				{
					throw;
				}
				catch (Exception ex)
				{
					//					Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Remove(FolderAccessToken);
					Debug.WriteLine(ex.ToString());
				}
			}

			// 旧バージョン利用ユーザーが新バージョンを利用しても問題なくDLフォルダにアクセスできるようにする
			if (_DownloadFolder == null && Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.ContainsItem(LoginUserId.ToString()))
			{
				var token = LoginUserId.ToString();
				_DownloadFolder = await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);
				CurrentFolderAccessToken = token;
			}

			return _DownloadFolder;
		}


		public async Task<bool> CanAccessVideoCacheFolder()
		{
			return await GetVideoCacheFolderState() == CacheFolderAccessState.Exist;
		}

		public async Task<CacheFolderAccessState> GetVideoCacheFolderState()
		{
			if (false == UserSettings.CacheSettings.IsUserAcceptedCache)
			{
				return CacheFolderAccessState.NotAccepted;
			}

			if (false == UserSettings.CacheSettings.IsEnableCache)
			{
				return CacheFolderAccessState.NotEnabled;
			}

			try
			{
				var videoFolder = await GetEnsureVideoFolder();

				if (videoFolder == null)
				{
					return CacheFolderAccessState.NotSelected;
				}
				else
				{
					return CacheFolderAccessState.Exist;
				}
			}
			catch (FileNotFoundException)
			{
				return CacheFolderAccessState.SelectedButNotExist;
			}

		}

	

		public async Task<StorageFolder> GetVideoCacheFolder()
		{
			try
			{
				return await GetEnsureVideoFolder();
			}
			catch (FileNotFoundException)
			{
				return null;
			}
		}



		public Task<StorageFolder> GetApplicationLocalDataFolder()
		{
			return Task.FromResult(ApplicationData.Current.LocalFolder);
		}


		public Task<StorageFolder> GetCurrentUserDataFolder()
		{
			return ApplicationData.Current.LocalFolder.CreateFolderAsync(LoginUserId.ToString(), CreationCollisionOption.OpenIfExists).AsTask();
		}

		public async Task<StorageFolder> GetFeedDataFolder()
		{
			var folder = await GetApplicationLocalDataFolder();

			if (folder == null) { return null; }

			return await folder.CreateFolderAsync("feed", CreationCollisionOption.OpenIfExists);
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


	public enum CacheFolderAccessState
	{
		NotAccepted,
		NotEnabled,
		NotSelected,
		SelectedButNotExist,
		Exist
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

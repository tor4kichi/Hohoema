using Mntone.Nico2;
using Mntone.Nico2.Live;
using Mntone.Nico2.Live.PlayerStatus;
using Mntone.Nico2.Videos.Comment;
using NicoPlayerHohoema.Helpers;
using NicoVideoRtmpClient;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;

namespace NicoPlayerHohoema.Models.Live
{

	public delegate void CommentPostedEventHandler(NicoLiveVideo sender, bool postSuccess);

	public delegate void DetectNextLiveEventHandler(NicoLiveVideo sender, string liveId);

    public delegate void OpenLiveEventHandler(NicoLiveVideo sender);
    public delegate void FailedOpenLiveEventHandler(NicoLiveVideo sender, OpenLiveFailedReason reason);
    public delegate void CloseLiveEventHandler(NicoLiveVideo sender);

    public enum LivePlayerType
    {
        Aries,
        Leo,
    }



    public enum OpenLiveFailedReason
    {
        TimeOver,
        VideoQuoteIsNotSupported,
    }

	public class NicoLiveVideo : BindableBase, IDisposable
	{
		public static readonly TimeSpan DefaultNextLiveSubscribeDuration =
			TimeSpan.FromMinutes(3);

		public HohoemaApp HohoemaApp { get; }

        public MediaPlayer MediaPlayer { get; }

        public event OpenLiveEventHandler OpenLive;

        public event CloseLiveEventHandler CloseLive;

        public event FailedOpenLiveEventHandler FailedOpenLive;

        public event CommentPostedEventHandler PostCommentResult;

		public event DetectNextLiveEventHandler NextLive;


		/// <summary>
		/// 生放送コンテンツID
		/// </summary>
		public string LiveId { get; private set; }


		string _CommunityId;


		/// <summary>
		/// 生放送の配信・視聴のためのメタ情報
		/// </summary>
		public PlayerStatusResponse PlayerStatusResponse { get; private set; }


		private MediaStreamSource _VideoStreamSource;
		private AsyncLock _VideoStreamSrouceAssignLock = new AsyncLock();


		/// <summary>
		/// 生放送の動画ストリーム<br />
		/// 生放送によってはRTMPで流れてくる動画ソースの場合と、ニコニコ動画の任意動画をソースにする場合がある。
		/// </summary>
		public MediaStreamSource VideoStreamSource
		{
			get { return _VideoStreamSource; }
			set { SetProperty(ref _VideoStreamSource, value); }
		}



		private ObservableCollection<Chat> _LiveComments;

		/// <summary>
		/// 受信した生放送コメント<br />
		/// </summary>
		/// <remarks>NicoLiveCommentClient.CommentRecieved</remarks>
		public ReadOnlyObservableCollection<Chat> LiveComments { get; private set; }



        private string _PermanentDisplayText;
		public string PermanentDisplayText
		{
			get { return _PermanentDisplayText; }
			private set { SetProperty(ref _PermanentDisplayText, value); }
		}


		private uint _CommentCount;
		public uint CommentCount
		{
			get { return _CommentCount; }
			private set { SetProperty(ref _CommentCount, value); }
		}

		private uint _WatchCount;
		public uint WatchCount
		{
			get { return _WatchCount; }
			private set { SetProperty(ref _WatchCount, value); }
		}

        private LiveStatusType? _LiveStatusType;
        public LiveStatusType? LiveStatusType
        {
            get { return _LiveStatusType; }
            private set { SetProperty(ref _LiveStatusType, value); }
        }


        private LivePlayerType? _LivePlayerType;
		public LivePlayerType? LivePlayerType
        {
			get { return _LivePlayerType; }
			private set { SetProperty(ref _LivePlayerType, value); }
		}





		public string NextLiveId { get; private set; }

		Timer _NextLiveSubscriveTimer;
		AsyncLock _NextLiveSubscriveLock = new AsyncLock();

		/// <summary>
		/// 生放送動画をRTMPで受け取るための通信クライアント<br />
		/// RTMPで正常に動画が受信できる状態になった場合 VideoStreamSource にインスタンスが渡される
		/// </summary>
		NicovideoRtmpClient _RtmpClient;
		AsyncLock _RtmpClientAssignLock = new AsyncLock();



		Timer _EnsureStartLiveTimer;
		AsyncLock _EnsureStartLiveTimerLock = new AsyncLock();

        public Live2WebSocket Live2WebSocket { get; private set; }

        FFmpegInterop.FFmpegInteropMSS _Mss;
        MediaSource _MediaSource;




        /// <summary>
        /// 生放送コメント関連の通信クライアント<br />
        /// 生放送コメントの受信と送信<br />
        /// 接続を維持して有効なコメント送信を行うためのハートビートタスクの実行
        /// </summary>
        NicoLiveCommentClient _NicoLiveCommentClient;



		AsyncLock _LiveSubscribeLock = new AsyncLock();
		

		public NicoLiveVideo(string liveId, MediaPlayer mediaPlayer, HohoemaApp hohoemaApp, string communityId = null)
		{
			LiveId = liveId;
			_CommunityId = communityId;
            MediaPlayer = mediaPlayer;
            HohoemaApp = hohoemaApp;

			_LiveComments = new ObservableCollection<Chat>();
			LiveComments = new ReadOnlyObservableCollection<Chat>(_LiveComments);
        }

		public void Dispose()
		{
            _Mss?.Dispose();
            _MediaSource?.Dispose();

            // 次枠検出を終了
            StopNextLiveSubscribe().ConfigureAwait(false);

			EndLiveSubscribe().ConfigureAwait(false);

            Live2WebSocket?.Dispose();
            Live2WebSocket = null;
        }

		public async Task<LiveStatusType?> UpdateLiveStatus()
		{
			LiveStatusType = null;

			try
			{
				PlayerStatusResponse = await HohoemaApp.NiconicoContext.Live.GetPlayerStatusAsync(LiveId);

				_CommunityId = PlayerStatusResponse.Program.CommunityId;

				Debug.WriteLine(PlayerStatusResponse.Stream.RtmpUrl);
				Debug.WriteLine(PlayerStatusResponse.Stream.Contents.Count);
            }
			catch (Exception ex)
			{
				if (ex.HResult == NiconicoHResult.ELiveNotFound)
				{
					LiveStatusType = Live.LiveStatusType.NotFound;

                    PermanentDisplayText = "*放送が見つかりませんでした";
                }
				else if (ex.HResult == NiconicoHResult.ELiveClosed)
				{
					LiveStatusType = Live.LiveStatusType.Closed;
                    PermanentDisplayText = "*放送は終了しています";
                }
				else if (ex.HResult == NiconicoHResult.ELiveComingSoon)
				{
					LiveStatusType = Live.LiveStatusType.ComingSoon;
                    PermanentDisplayText = "*放送開始までお待ち下さい";
                }
				else if (ex.HResult == NiconicoHResult.EMaintenance)
				{
					LiveStatusType = Live.LiveStatusType.Maintenance;
                    PermanentDisplayText = "*メンテナンス中です";
                }
				else if (ex.HResult == NiconicoHResult.ELiveCommunityMemberOnly)
				{
					LiveStatusType = Live.LiveStatusType.CommunityMemberOnly;
                    PermanentDisplayText = "*コミュニティ限定放送です";
                }
				else if (ex.HResult == NiconicoHResult.ELiveFull)
				{
					LiveStatusType = Live.LiveStatusType.Full;
                    PermanentDisplayText = "*満員です";
                }
				else if (ex.HResult == NiconicoHResult.ELivePremiumOnly)
				{
					LiveStatusType = Live.LiveStatusType.PremiumOnly;
                    PermanentDisplayText = "*プレミアム会員限定放送です";
                }
				else if (ex.HResult == NiconicoHResult.ENotSigningIn)
				{
					LiveStatusType = Live.LiveStatusType.NotLogin;
                    PermanentDisplayText = "*ログインしていません";
                }
			}

			if (LiveStatusType != null)
			{
				await EndLiveSubscribe();
			}

			return LiveStatusType;
		}


		public async Task<LiveStatusType?> SetupLive()
		{
            if (PlayerStatusResponse != null)
            {
                PlayerStatusResponse = null;

                await EndLiveSubscribe();

                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            LiveStatusType = await UpdateLiveStatus();

            await Task.Delay(500);

            Mntone.Nico2.Live.Watch.LeoPlayerProps leoPlayerProps = null;
            try
            {
                leoPlayerProps = await HohoemaApp.NiconicoContext.Live.GetLeoPlayerPropsAsync(LiveId);
            }
            catch { }

            if (leoPlayerProps != null)
            {
                Debug.WriteLine(leoPlayerProps.BroadcastId);

                LivePlayerType = Live.LivePlayerType.Leo;

                if (Live2WebSocket == null)
                {
                    Live2WebSocket = new Live2WebSocket(leoPlayerProps);
                    Live2WebSocket.RecieveCurrentStream += Live2WebSocket_RecieveCurrentStream;
                    Live2WebSocket.RecieveStatistics += Live2WebSocket_RecieveStatistics;
                    Live2WebSocket.RecieveDisconnect += Live2WebSocket_RecieveDisconnect;

                    var quality = HohoemaApp.UserSettings.PlayerSettings.DefaultLiveQuality;
                    if (BroadcasterCommunityType != CommunityType.Community)
                    {
                        quality = "high";
                    }

                    _IsLowLatency = HohoemaApp.UserSettings.PlayerSettings.LiveWatchWithLowLatency;
                    await Live2WebSocket.StartAsync(quality, _IsLowLatency);
                }

                await StartLiveSubscribe();

                return LiveStatusType;
            }
            else
            {
                if (PlayerStatusResponse != null && LiveStatusType == null)
                {
                    LivePlayerType = Live.LivePlayerType.Aries;

                    await StartEnsureOpenRtmpConnection();

                    await StartLiveSubscribe();
                }

                return LiveStatusType;
            }
		}

        

        private async Task StartLiveSubscribe()
		{
			using (var releaser = await _LiveSubscribeLock.LockAsync())
			{
				await StartCommentClientConnection();

                // Display表示の維持リクエスト
                Helpers.DisplayRequestHelper.RequestKeepDisplay();
			}
		}

		/// <summary>
		/// ニコ生からの離脱処理<br />
		/// HeartbeatAPIへの定期アクセスの停止、及びLeaveAPIへのアクセス
		/// </summary>
		/// <returns></returns>
		public async Task EndLiveSubscribe()
		{
			using (var releaser = await _LiveSubscribeLock.LockAsync())
			{
                // Display表示の維持リクエストを停止
                Helpers.DisplayRequestHelper.StopKeepDisplay();

                // 放送接続の確実化処理を終了
                await ExitEnsureOpenRtmpConnection();

				// ニコ生サーバーから切断
				await CloseRtmpConnection();

				// HeartbeatAPIへのアクセスを停止
				await EndCommentClientConnection();

				// 放送からの離脱APIを叩く
				await HohoemaApp.NiconicoContext.Live.LeaveAsync(LiveId);
			}
		}



		// 
		public async Task<Uri> MakeLiveSummaryHtmlUri()
		{
			if (PlayerStatusResponse == null) { return null; }

			var desc = PlayerStatusResponse.Program.Description;

			return await HtmlFileHelper.PartHtmlOutputToCompletlyHtml(LiveId, desc);
		}

        #region Live2WebSocket Event Handling


        string _HLSUri;

        private string _RequestQuality;
        public string RequestQuality
        {
            get { return _RequestQuality; }
            private set { SetProperty(ref _RequestQuality, value); }
        }

        private string _CurrentQuality;
        public string CurrentQuality
        {
            get { return _CurrentQuality; }
            private set { SetProperty(ref _CurrentQuality, value); }
        }

        public string[] Qualities { get; private set; }

        bool _IsLowLatency;

        public async Task ChangeQualityRequest(string quality, bool isLowLatency)
        {
            if (this.LivePlayerType == Live.LivePlayerType.Leo)
            {
                if (CurrentQuality == quality && _IsLowLatency == isLowLatency) { return; }

                MediaPlayer.Source = null;

                RequestQuality = quality;
                _IsLowLatency = isLowLatency;
                await Live2WebSocket.SendChangeQualityMessageAsync(quality, isLowLatency);
            }
        }



        private async void Live2WebSocket_RecieveCurrentStream(Live2CurrentStreamEventArgs e)
        {
            Debug.WriteLine(e.Uri);

            await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => 
            {
                _HLSUri = e.Uri;
                
                // Note: Hohoemaでは画質の自動設定 abr は扱いません
                Qualities = e.QualityTypes.Where(x => x != "abr").ToArray();
                RaisePropertyChanged(nameof(Qualities));
                CurrentQuality = e.Quality;

                Debug.WriteLine(e.Quality);
                
                await RefreshLeoPlayer();

                OpenLive?.Invoke(this);
            });
        }


        private async Task RefreshLeoPlayer()
        {
            if (_HLSUri == null) { return; }

            await ClearLeoPlayer();

            await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => 
            {
//                var streamAsyncUri = _HLSUri.Replace("master.m3u8", "stream_sync.json");

//                var playSetupRes = await HohoemaApp.NiconicoContext.HttpClient.GetAsync(new Uri(streamAsyncUri));

                try
                {
                    _Mss = FFmpegInterop.FFmpegInteropMSS.CreateFFmpegInteropMSSFromUri(_HLSUri, false, false);
                    
                    _MediaSource = MediaSource.CreateFromMediaStreamSource(_Mss.GetMediaStreamSource());

                    MediaPlayer.Source = _MediaSource;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            });
        }

        private async Task ClearLeoPlayer()
        {
            await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (MediaPlayer.Source == _MediaSource)
                {
                    MediaPlayer.Source = null;

                    CloseLive?.Invoke(this);
                }

                _Mss?.Dispose();
                _Mss = null;
                _MediaSource?.Dispose();
                _MediaSource = null;
            });
        }


        private void Live2WebSocket_RecieveDisconnect()
        {
            StartNextLiveSubscribe(TimeSpan.FromMinutes(2)).ConfigureAwait(false);
        }

        private async void Live2WebSocket_RecieveStatistics(Live2StatisticsEventArgs e)
        {
            await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                WatchCount = (uint)e.ViewCount;
                CommentCount = (uint)e.CommentCount;
            });
        }

        #endregion



        #region PlayerStatusResponse projection Properties


        public string LiveTitle => PlayerStatusResponse?.Program.Title;

		private string _BroadcasterId;
		public string BroadcasterId
		{
			get
			{
				return _BroadcasterId ??
					(_BroadcasterId = PlayerStatusResponse?.Program.BroadcasterId.ToString());
			}
		}

		public string BroadcasterName => PlayerStatusResponse?.Program.BroadcasterName;

		public CommunityType? BroadcasterCommunityType => PlayerStatusResponse?.Program.CommunityType;

		public Uri BroadcasterCommunityImageUri => PlayerStatusResponse?.Program.CommunityImageUrl;

		public string BroadcasterCommunityId => _CommunityId;


		

		#endregion



        public Task Refresh()
        {
            
            if (LivePlayerType == Live.LivePlayerType.Aries)
            {
                return RetryRtmpConnection();
            }
            else if (LivePlayerType == Live.LivePlayerType.Leo)
            {
                return RefreshLeoPlayer();
            }
            else
            {
                return Task.CompletedTask;
            }
        }



		#region LiveVideo RTMP

		public async Task RetryRtmpConnection()
		{
			if (PlayerStatusResponse == null)
			{
				if (await UpdateLiveStatus() != null)
				{
					return;
				}
			}

			using (var releaser = await _VideoStreamSrouceAssignLock.LockAsync())
			{
                MediaPlayer.Source = null;
                VideoStreamSource = null;
                _MediaSource?.Dispose();
                _MediaSource = null;

            }

			await StartEnsureOpenRtmpConnection();
		}

		TimeSpan EnsureOpenRtmpTryDuration = TimeSpan.FromMinutes(1);
		DateTime EnsureOpenRtmpStartTime;

		public async Task StartEnsureOpenRtmpConnection()
		{
			if (PlayerStatusResponse == null) { return; }


			using (var releaser = await _EnsureStartLiveTimerLock.LockAsync())
			{
				if (_EnsureStartLiveTimer == null)
				{
					_EnsureStartLiveTimer = new Timer(
						EnsureOpenRtmpConnectionInntenal,
						this,
						TimeSpan.FromSeconds(0),
						TimeSpan.FromSeconds(5)
						);
					EnsureOpenRtmpStartTime = DateTime.Now;

					Debug.WriteLine("START ensure open rtmp connection");
				}
			}
		}

		public async Task ExitEnsureOpenRtmpConnection()
		{
			using (var releaser = await _EnsureStartLiveTimerLock.LockAsync())
			{
				if (_EnsureStartLiveTimer != null)
				{
					_EnsureStartLiveTimer.Dispose();
					_EnsureStartLiveTimer = null;

					Debug.WriteLine("EXIT ensure open rtmp connection ");
				}
			}
		}

		private async void EnsureOpenRtmpConnectionInntenal(object state = null)
		{
			if (DateTime.Now > EnsureOpenRtmpStartTime + EnsureOpenRtmpTryDuration)
			{
				await ExitEnsureOpenRtmpConnection();

                FailedOpenLive?.Invoke(this, OpenLiveFailedReason.TimeOver);

                return;
			}

			Debug.WriteLine("TRY ensure open rtmp connection ");


			bool isDone = false;
			using (var releaser = await _VideoStreamSrouceAssignLock.LockAsync())
			{
				isDone = VideoStreamSource != null;
			}


			if (!isDone)
			{
				Debug.WriteLine("AGEIN ensure open rtmp connection ");

				await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
				{
					await CloseRtmpConnection();

					await Task.Delay(1000);

					await OpenRtmpConnection(PlayerStatusResponse);
				});
			}
			else
			{
				Debug.WriteLine("DETECT ensure open rtmp connection");

				await ExitEnsureOpenRtmpConnection();
			}
		}

		private async Task OpenRtmpConnection(PlayerStatusResponse res)
		{
			await CloseRtmpConnection();

			using (var releaser = await _RtmpClientAssignLock.LockAsync())
			{
				if (_RtmpClient == null)
				{
					_RtmpClient = new NicovideoRtmpClient();

					_RtmpClient.Started += _RtmpClient_Started;
					_RtmpClient.Stopped += _RtmpClient_Stopped;

                    try
                    {
                        await _RtmpClient.ConnectAsync(res);
                    }
                    catch
                    {
                        _RtmpClient.Started -= _RtmpClient_Started;
                        _RtmpClient.Stopped -= _RtmpClient_Stopped;
                        _RtmpClient.Dispose();
                        _RtmpClient = null;
                        _EnsureStartLiveTimer?.Dispose();
                        _EnsureStartLiveTimer = null;

                        Debug.WriteLine("CAN NOT play Rtmp, Stop ensure open timer. : " + res.Stream.RtmpUrl);

                        PermanentDisplayText = "*動画引用放送には対応していません";

                        FailedOpenLive?.Invoke(this, OpenLiveFailedReason.VideoQuoteIsNotSupported);
                    }

                }
			}
		}

		private async Task CloseRtmpConnection()
		{
			using (var releaser = await _RtmpClientAssignLock.LockAsync())
			{
				if (_RtmpClient != null)
				{
					_RtmpClient.Started -= _RtmpClient_Started;
					_RtmpClient.Stopped -= _RtmpClient_Stopped;

					_RtmpClient?.Dispose();
					_RtmpClient = null;

					await Task.Delay(500);
				}
			}
		}


		private async void _RtmpClient_Started(NicovideoRtmpClientStartedEventArgs args)
		{
			await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			{
                if (_MediaSource == null)
                {
                    VideoStreamSource = args.MediaStreamSource;
                    _MediaSource = MediaSource.CreateFromMediaStreamSource(args.MediaStreamSource);
                    MediaPlayer.Source = _MediaSource;

                    Debug.WriteLine("recieve start live stream: " + LiveId);

                    OpenLive?.Invoke(this);
                }
			});
		}

		private async void _RtmpClient_Stopped(NicovideoRtmpClientStoppedEventArgs args)
		{
			await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
			{
				using (var releaser = await _VideoStreamSrouceAssignLock.LockAsync())
				{
                    MediaPlayer.Source = null;

                    VideoStreamSource = null;
                    _MediaSource?.Dispose();
                    _MediaSource = null;
                }

                Debug.WriteLine("recieve exit live stream: " + LiveId);

                CloseLive?.Invoke(this);

                await StartNextLiveSubscribe(DefaultNextLiveSubscribeDuration);
			});
		}


		#endregion


		#region Live Comment 

		public bool CanPostComment => !(PlayerStatusResponse?.Program.IsArchive ?? true);

		private string _LastCommentText;

		public async Task PostComment(string message, string command, TimeSpan elapsedTime)
		{
			if (!CanPostComment)
			{
				PostCommentResult?.Invoke(this, false);
				return;
			}

			if (_NicoLiveCommentClient != null)
			{
				var userId = PlayerStatusResponse.User.Id;
				_LastCommentText = message;
				await _NicoLiveCommentClient.PostComment(message, userId, command, elapsedTime);
			}
		}

		private async Task StartCommentClientConnection()
		{
			await EndCommentClientConnection();

			var baseTime = PlayerStatusResponse.Program.BaseAt;
			_NicoLiveCommentClient = new NicoLiveCommentClient(LiveId, PlayerStatusResponse.Program.CommentCount, baseTime, PlayerStatusResponse.Comment.Server, HohoemaApp.NiconicoContext);
			_NicoLiveCommentClient.CommentServerConnected += _NicoLiveCommentReciever_CommentServerConnected;
			_NicoLiveCommentClient.Heartbeat += _NicoLiveCommentClient_Heartbeat;
			_NicoLiveCommentClient.EndConnect += _NicoLiveCommentClient_EndConnect;

			_NicoLiveCommentClient.CommentRecieved += _NicoLiveCommentReciever_CommentRecieved;
			_NicoLiveCommentClient.OperationCommandRecieved += _NicoLiveCommentClient_OperationCommandRecieved;

			await _NicoLiveCommentClient.Start();
		}

		private async void _NicoLiveCommentClient_EndConnect(NicoLiveDisconnectReason reason)
		{
			await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
			{
                if (LivePlayerType == Live.LivePlayerType.Aries)
                {
                    await CloseRtmpConnection();
                }
                else
                {
                    await ClearLeoPlayer();
                }

                if (reason == NicoLiveDisconnectReason.Close)
                {
                    await StartNextLiveSubscribe(DefaultNextLiveSubscribeDuration);
                }
            });
		}

		private async Task EndCommentClientConnection()
		{
			if (_NicoLiveCommentClient != null)
			{
				await _NicoLiveCommentClient.Stop();

				_NicoLiveCommentClient.CommentServerConnected -= _NicoLiveCommentReciever_CommentServerConnected;
				_NicoLiveCommentClient.CommentPosted -= _NicoLiveCommentReciever_CommentPosted;

				_NicoLiveCommentClient.Dispose();

				_NicoLiveCommentClient = null;

				
			}
		}

		private void _NicoLiveCommentReciever_CommentServerConnected()
		{
			_NicoLiveCommentClient.CommentPosted += _NicoLiveCommentReciever_CommentPosted;
		}

		private async void _NicoLiveCommentReciever_CommentPosted(bool isSuccess, PostChat chat)
		{
			Debug.WriteLine("コメント投稿結果：+" + isSuccess);

			await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => 
			{
				PostCommentResult?.Invoke(this, isSuccess);

				if (isSuccess)
				{
					_LiveComments.Add(new Chat()
					{
						User_id = chat.UserId,
						Vpos = chat.Vpos,
						No = "",
						Mail = chat.Mail,
						Text = chat.Comment,
						Anonymity = (chat.Mail?.Contains("184") ?? false) ? "true" : null
					});
				}
			});
		}

		private async void _NicoLiveCommentClient_Heartbeat(uint commentCount, uint watchCount)
		{
			await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			{
				CommentCount = commentCount;
				WatchCount = watchCount;
			});
        }


		private async void _NicoLiveCommentReciever_CommentRecieved(Chat chat)
		{
			await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			{
				_LiveComments.Insert(0, chat);
			});

			if (chat.User_id == BroadcasterId)
			{
				if (chat.Text.Contains("href"))
				{
					var root = XDocument.Parse(chat.Text);
					var anchor = root.Element("a");
					if (anchor != null)
					{
						var href = anchor.Attribute("href");
						var link = href.Value;

						if (chat.Text.Contains("次"))
						{
							var liveId = link.Split('/').LastOrDefault();
							if (NiconicoRegex.IsLiveId(liveId))
							{
								// TODO: liveIdの放送情報を取得して、配信者が同一ユーザーかチェックする
								using (var releaser = await _NextLiveSubscriveLock.LockAsync())
								{
									await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
									{
										NextLiveId = liveId;
										NextLive?.Invoke(this, NextLiveId);
									});

								}
							}
						}

						// TODO: linkをブラウザで開けるようにする
					}
				}
			}
			
		}

		private async void _NicoLiveCommentClient_OperationCommandRecieved(NicoLiveCommentClient sender, NicoLiveOperationCommandEventArgs args)
		{
			await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
			{
				switch (args.CommandType)
				{
					case NicoLiveOperationCommandType.Play:
						break;
					case NicoLiveOperationCommandType.PlaySound:
						break;
					case NicoLiveOperationCommandType.PermanentDisplay:
						if (args.Arguments.Length > 0)
						{
							PermanentDisplayText = args.Arguments[0];
						}
						break;
					case NicoLiveOperationCommandType.ClearPermanentDisplay:
						PermanentDisplayText = null;
						break;
					case NicoLiveOperationCommandType.Vote:
						break;
					case NicoLiveOperationCommandType.CommentMode:
						break;
					case NicoLiveOperationCommandType.Call:
						break;
					case NicoLiveOperationCommandType.Free:
						break;
					case NicoLiveOperationCommandType.Reset:

						// 動画接続のリセット
						await RetryRtmpConnection();

						break;
					case NicoLiveOperationCommandType.Info:

						// 1:市場登録　2:コミュニティ参加　3:延長　4,5:未確認　6,7:地震速報　8:現在の放送ランキングの順位
						// /info 数字 "表示内容"
						
						if (args.Arguments.Length >= 2)
						{
							int infoType;
							if (int.TryParse(args.Arguments[0], out infoType))
							{
								var nicoLiveInfoType = (NicoLiveInfoType)infoType;

								args.Chat.Text = args.Arguments[1];
								args.Chat.Mail = "shita";
								
								_LiveComments.Add(args.Chat);
							}
						}
						break;
					case NicoLiveOperationCommandType.Press:

						// http://dic.nicovideo.jp/a/%E3%83%90%E3%83%83%E3%82%AF%E3%82%B9%E3%83%86%E3%83%BC%E3%82%B8%E3%83%91%E3%82%B9
						// TODO: BSPユーザーによるコメントに対応
						// BSPコメへの風当たりはやや強いのでオプションでON/OFF切り替え対応必要かも
						if (args.Arguments.Length >= 4)
						{
							args.Chat.Mail = args.Arguments[1];
							args.Chat.Text = args.Arguments[2];
                            //var name = args.Arguments[3];

                            _LiveComments.Add(args.Chat);
						}
						break;
					case NicoLiveOperationCommandType.Disconnect:

						// 放送者側からの切断要請

						// Note: RTMPによる動画受信の停止はDisconnect後の
						// RtmpClient.Closedイベントによって処理されます。
						// また、RtmpClientがクローズ中にここでRtmpClient.Close()を行うと
						// スレッドセーフではないためか、例外が発生します。
						
						await CloseRtmpConnection();

						await Task.Delay(500);

						// 次枠の自動巡回を開始
//						await StartNextLiveSubscribe(DefaultNextLiveSubscribeDuration);

						break;
					case NicoLiveOperationCommandType.Koukoku:
						break;
					case NicoLiveOperationCommandType.Telop:

						/*
							on ニコ生クルーズ(リンク付き)/ニコニコ実況コメント
							show クルーズが到着/実況に接続
							show0 実際に流れているコメント
							perm ニコ生クルーズが去って行きました＜改行＞(降りた人の名前、人数)
							off (プレイヤー下部のテロップを消去) 
						*/

						if (args.Arguments.Length >= 2)
						{
							// TODO: 
							
						}

						break;
					case NicoLiveOperationCommandType.Hidden:
						break;
					case NicoLiveOperationCommandType.CommentLock:
						break;
					default:
						break;
				}
			});
		}




		#endregion



		#region Next Live Detection

		TimeSpan NextLiveSubscribeDuration;
		private DateTime NextLiveSubscribeStartTime;


		public async Task StartNextLiveSubscribe(TimeSpan duration)
		{
			using (var releaser = await _NextLiveSubscriveLock.LockAsync())
			{
                // コミュニティ以外の動画には現状対応していない
                if (BroadcasterCommunityType != CommunityType.Community)
                {
                    return;
                }

				if (NextLiveId != null)
				{
					return;
				}

				if (_NextLiveSubscriveTimer == null)
				{
					_NextLiveSubscriveTimer = new Timer(
						NextLiveSubscribe,
						null,
						TimeSpan.FromSeconds(3),
						TimeSpan.FromSeconds(10)
						);
					NextLiveSubscribeStartTime = DateTime.Now;
				}

				NextLiveSubscribeDuration = duration;

				Debug.WriteLine("start detect next live.");

				await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
				{
					PermanentDisplayText = "*次枠を探しています...";
				});
			}
		}

		private async Task StopNextLiveSubscribe()
		{
			using (var releaser = await _NextLiveSubscriveLock.LockAsync())
			{
				if (_NextLiveSubscriveTimer != null)
				{
					_NextLiveSubscriveTimer.Dispose();
					_NextLiveSubscriveTimer = null;

					Debug.WriteLine("stop detect next live.");
				}
			}
		}

		private async void NextLiveSubscribe(object state = null)
		{
			await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
			{

				bool isDone = false;
				using (var releaser = await _NextLiveSubscriveLock.LockAsync())
				{
					isDone = NextLiveId != null;

				}


				if (isDone)
				{
					Debug.WriteLine("exit detect next live. (success with operation comment) : " + NextLiveId);
					await StopNextLiveSubscribe();
					return;
				}

				// コミュニティページを取得して、放送中のLiveIdを取得する
				try
				{
					var commuDetail = await HohoemaApp.ContentProvider.GetCommunityDetail(BroadcasterCommunityId);

					// this.LiveIdと異なるLiveIdが一つだけの場合はそのIDを次の枠として処理
					var liveIds = commuDetail.CommunitySammary.CommunityDetail.CurrentLiveList.Select(x => x.LiveId);
					foreach (var nextLiveId in liveIds)
					{
						if (nextLiveId != LiveId)
						{
							using (var releaser = await _NextLiveSubscriveLock.LockAsync())
							{
								NextLiveId = nextLiveId;

								PermanentDisplayText = "*次枠を検出しました → " + NextLiveId;

								NextLive?.Invoke(this, NextLiveId);
								Debug.WriteLine("exit detect next live. (success) : " + NextLiveId);


								isDone = true;
							}

							break;
						}
					}
				}
				catch
				{
					Debug.WriteLine("exit detect next live. (failed community page access)");

					await StopNextLiveSubscribe();

					await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
					{
						PermanentDisplayText = "コミュニティ情報が取得できませんでした";
					});

					return;
				}

				// this.LiveIdと異なるLiveIdが複数ある場合は、それぞれの放送情報を取得して、
				// 放送主のIDがBroadcasterIdと一致する方を次の枠として選択する
				// （配信タイトルの似てる方で選択してもよさそう？）



				// 定期チェックの終了時刻
				using (var releaser = await _NextLiveSubscriveLock.LockAsync())
				{
					if (NextLiveSubscribeStartTime + NextLiveSubscribeDuration < DateTime.Now)
					{
						isDone = true;

						PermanentDisplayText = "コミュニティ情報が取得できませんでした";

						Debug.WriteLine("detect next live time over");
					}
				}

				if (isDone)
				{
					await StopNextLiveSubscribe();

					return;
				}
			});
		}

		#endregion
	}



}

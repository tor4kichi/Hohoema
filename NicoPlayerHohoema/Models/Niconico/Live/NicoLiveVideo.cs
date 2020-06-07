using I18NPortable;
using Mntone.Nico2;
using Mntone.Nico2.Live;
using Mntone.Nico2.Live.Video;
using Mntone.Nico2.Live.Watch;
using Mntone.Nico2.Videos.Comment;
using MvvmHelpers;
using NicoPlayerHohoema.Models.Niconico;
using NicoPlayerHohoema.Models.Provider;
using Prism.Mvvm;
using Prism.Unity;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;
using Windows.UI.Xaml;
using WinRTXamlToolkit.IO.Serialization;

namespace NicoPlayerHohoema.Models.Live
{

    public struct FailedOpenLiveEventArgs
    {
        public Exception Exception { get; set; }
        public string Message { get; set; }
    }

    public struct StartNextLiveDetectionEventArgs
    {
        public TimeSpan Duration { get; set; }
    }

    public struct CompleteNextLiveDetectionEventArgs
    {
        public bool IsSuccess => !string.IsNullOrEmpty(NextLiveId);
        public string NextLiveId { get; set; }
    }

    public delegate void CommentPostedEventHandler(NicoLiveVideo sender, bool postSuccess);

    public delegate void OpenLiveEventHandler(NicoLiveVideo sender);
    public delegate void FailedOpenLiveEventHandler(NicoLiveVideo sender, FailedOpenLiveEventArgs args);
    public delegate void CloseLiveEventHandler(NicoLiveVideo sender);


    public struct OperationCommandRecievedEventArgs
    {
        public LiveComment Comment { get; set; }

        public string CommandType => Comment.OperatorCommandType;
        public string[] CommandParameter => Comment.OperatorCommandParameters;
    }

    public enum LivePlayerType
    {
        Aries,
        Leo,
    }



    public enum OpenLiveFailedReason
    {
        TimeOver,
        VideoQuoteIsNotSupported,
        ServiceTemporarilyUnavailable,
    }

    public class NicoLiveVideo : FixPrism.BindableBase, IDisposable
    {
        public NicoLiveVideo(
            string liveId,
            MediaPlayer mediaPlayer,
            NiconicoSession niconicoSession,
            NicoLiveProvider nicoLiveProvider,
            LoginUserLiveReservationProvider loginUserLiveReservationProvider,
            PlayerSettings playerSettings,
            AppearanceSettings appearanceSettings,
            IScheduler scheduler,
            string communityId = null
            )
        {
            LiveId = liveId;
            _CommunityId = communityId;
            MediaPlayer = mediaPlayer;
            NiconicoSession = niconicoSession;
            NicoLiveProvider = nicoLiveProvider;
            LoginUserLiveReservationProvider = loginUserLiveReservationProvider;
            PlayerSettings = playerSettings;
            _appearanceSettings = appearanceSettings;
            _UIScheduler = scheduler;

            _LiveComments = new ObservableRangeCollection<LiveComment>();
            LiveComments = new ReadOnlyObservableCollection<LiveComment>(_LiveComments);


            LiveComments.ObserveAddChanged()
                .Where(x => x.IsOperationCommand)
                .SubscribeOn(_UIScheduler)
                .Subscribe(chat =>
                {
                    OperationCommandRecieved?.Invoke(this, new OperationCommandRecievedEventArgs() { Comment = chat });
                });
        }

        public static readonly TimeSpan JapanTimeZoneOffset = +TimeSpan.FromHours(9);
        public static readonly TimeSpan DefaultNextLiveSubscribeDuration =
            TimeSpan.FromMinutes(3);

        public MediaPlayer MediaPlayer { get; }
        public NiconicoSession NiconicoSession { get; }
        public NicoLiveProvider NicoLiveProvider { get; }
        public LoginUserLiveReservationProvider LoginUserLiveReservationProvider { get; }
        public PlayerSettings PlayerSettings { get; }

        public event OpenLiveEventHandler OpenLive;
        public event CloseLiveEventHandler CloseLive;
        public event FailedOpenLiveEventHandler FailedOpenLive;
        public event CommentPostedEventHandler PostCommentResult;

        public event EventHandler<TimeSpan> LiveElapsed;

        public event EventHandler<OperationCommandRecievedEventArgs> OperationCommandRecieved;

        /// <summary>
        /// 生放送コンテンツID
        /// </summary>
        public string LiveId { get; private set; }


        string _CommunityId;


        public string RoomName { get; private set; }

        Mntone.Nico2.Live.ReservationsInDetail.Program _TimeshiftProgram;

        public bool IsWatchWithTimeshift
        {
            get
            {
                var reservtionStatus = _TimeshiftProgram?.GetReservationStatus();

                return reservtionStatus != null &&
                    (reservtionStatus == Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.FIRST_WATCH ||
                    reservtionStatus == Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.WATCH ||
                    reservtionStatus == Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.PRODUCT_ARCHIVE_WATCH ||
                    reservtionStatus == Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.TSARCHIVE
                    );
            }
        }



        private MediaStreamSource _VideoStreamSource;
        private Models.Helpers.AsyncLock _VideoStreamSrouceAssignLock = new Models.Helpers.AsyncLock();


        /// <summary>
        /// 生放送の動画ストリーム<br />
        /// 生放送によってはRTMPで流れてくる動画ソースの場合と、ニコニコ動画の任意動画をソースにする場合がある。
        /// </summary>
        public MediaStreamSource VideoStreamSource
        {
            get { return _VideoStreamSource; }
            set { SetProperty(ref _VideoStreamSource, value); }
        }


        /// <summary>
        /// 受信した生放送コメント<br />
        /// </summary>
        /// <remarks>NicoLiveCommentClient.CommentRecieved</remarks>
        public ReadOnlyObservableCollection<LiveComment> LiveComments { get; private set; }

        private ObservableRangeCollection<LiveComment> _LiveComments;

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

        private StatusType _LiveStatus;
        public StatusType LiveStatus
        {
            get { return _LiveStatus; }
            private set { SetProperty(ref _LiveStatus, value); }
        }


        private LivePlayerType? _LivePlayerType;
        public LivePlayerType? LivePlayerType
        {
            get { return _LivePlayerType; }
            private set { SetProperty(ref _LivePlayerType, value); }
        }


        DateTimeOffset? _OpenTime;
        public DateTimeOffset OpenTime => (_OpenTime ?? (_OpenTime = DateTimeOffset.FromUnixTimeSeconds(_PlayerProps?.Program.OpenTime ?? 0))).Value;
        DateTimeOffset? _StartTime;
        public DateTimeOffset StartTime => (_StartTime ?? (_StartTime = DateTimeOffset.FromUnixTimeSeconds(_PlayerProps?.Program.BeginTime ?? 0))).Value;
        DateTimeOffset? _EndTime;
        public DateTimeOffset EndTime => (_EndTime ?? (_EndTime = DateTimeOffset.FromUnixTimeSeconds(_PlayerProps?.Program.EndTime ?? 0))).Value;


        Mntone.Nico2.Live.Watch.Crescendo.CrescendoLeoProps _PlayerProps;


        public string NextLiveId { get; private set; }


        Timer _EnsureStartLiveTimer;
        Models.Helpers.AsyncLock _EnsureStartLiveTimerLock = new Models.Helpers.AsyncLock();

        public Live2WebSocket Live2WebSocket { get; private set; }

        FFmpegInterop.FFmpegInteropMSS _Mss;
        MediaSource _MediaSource;
        AdaptiveMediaSource _AdaptiveMediaSource;



        /// <summary>
        /// 生放送コメント関連の通信クライアント<br />
        /// 生放送コメントの受信と送信<br />
        /// 接続を維持して有効なコメント送信を行うためのハートビートタスクの実行
        /// </summary>
        INicoLiveCommentClient _NicoLiveCommentClient;



        Models.Helpers.AsyncLock _LiveSubscribeLock = new Models.Helpers.AsyncLock();



        public class OperatorCommand
        {
            public LiveChatData Chat { get; set; }
            public string CommandType { get; set; }
            public string CommandParameter { get; set; }
        }

        private readonly AppearanceSettings _appearanceSettings;
        IScheduler _UIScheduler;

       
        public void Dispose()
        {
            _Mss?.Dispose();
            _MediaSource?.Dispose();

            ExitLiveViewing().ConfigureAwait(false);

            Live2WebSocket?.Dispose();
            Live2WebSocket = null;
        }

        public async Task UpdateLiveStatus(Mntone.Nico2.Live.Watch.Crescendo.CrescendoLeoProps props)
        {
            LiveStatus = props.Program.Status switch
            {
                "ON_AIR" => StatusType.OnAir,
                "RELEASED" => StatusType.ComingSoon,
                "ENDED" => StatusType.Closed,
                var x => throw new NotSupportedException(x)
            };

            if (LiveStatus != StatusType.OnAir || LiveStatus != StatusType.ComingSoon)
            {
                await ExitLiveViewing();
            }

            if (props != null)
            {
                // Official だと info.Communityはnullになる
                _CommunityId = props.SocialGroup.Id;

                LiveTitle = props.Program.Title;
                RaisePropertyChanged(nameof(LiveTitle));
                BroadcasterId = props.Program.BroadcastId ?? props.SocialGroup.Id;
                BroadcasterName = props.SocialGroup.Name;
                BroadcasterCommunityType = props.SocialGroup.Type switch 
                {
                    "channel" => CommunityType.Channel,
                    "official" => CommunityType.Official,
                    "community" => CommunityType.Community,
                    var x => throw new NotSupportedException(x)
                };
                BroadcasterCommunityId = props.SocialGroup.Id;
            }
        }


        public async Task<LiveStatusType> GetLiveViewingFailedReason()
        {
            LiveStatusType type = LiveStatusType.Unknown;
            try
            {
                var playerStatus = await NicoLiveProvider.GetPlayerStatusAsync(LiveId);

                if (playerStatus.Program.IsLive)
                {
                    type = Live.LiveStatusType.OnAir;
                }
            }
            catch (Exception ex)
            {
                if (ex.HResult == NiconicoHResult.ELiveNotFound)
                {
                    type = Live.LiveStatusType.NotFound;
                }
                else if (ex.HResult == NiconicoHResult.ELiveClosed)
                {
                    type = Live.LiveStatusType.Closed;
                }
                else if (ex.HResult == NiconicoHResult.ELiveComingSoon)
                {
                    type = Live.LiveStatusType.ComingSoon;
                }
                else if (ex.HResult == NiconicoHResult.EMaintenance)
                {
                    type = Live.LiveStatusType.Maintenance;
                }
                else if (ex.HResult == NiconicoHResult.ELiveCommunityMemberOnly)
                {
                    type = Live.LiveStatusType.CommunityMemberOnly;
                }
                else if (ex.HResult == NiconicoHResult.ELiveFull)
                {
                    type = Live.LiveStatusType.Full;
                }
                else if (ex.HResult == NiconicoHResult.ELivePremiumOnly)
                {
                    type = Live.LiveStatusType.PremiumOnly;
                }
                else if (ex.HResult == NiconicoHResult.ENotSigningIn)
                {
                    type = Live.LiveStatusType.NotLogin;
                }
                else
                {
                    type = LiveStatusType.Unknown;
                }
            }

            return type;
        }

        private async Task RefreshTimeshiftProgram()
        {
            if (NiconicoSession.IsLoggedIn)
            {
                var timeshiftDetailsRes = await LoginUserLiveReservationProvider.GetReservtionsAsync();
                foreach (var timeshift in timeshiftDetailsRes.ReservedProgram)
                {
                    if (LiveId.EndsWith(timeshift.Id))
                    {
                        _TimeshiftProgram = timeshift;
                    }
                }
            }
            else
            {
                _TimeshiftProgram = null;
            }
        }

        public async Task StartLiveWatchingSessionAsync()
        {
            try
            {
                _PlayerProps = await NicoLiveProvider.GetLeoPlayerPropsAsync(LiveId);
                await UpdateLiveStatus(_PlayerProps);
            }
            catch (Exception ex)
            {
                FailedOpenLive?.Invoke(this, new FailedOpenLiveEventArgs()
                {
                    Exception = ex,
                    Message = "FailedWatchLive".Translate()
                }) ;

                LiveStatus = StatusType.Invalid;
            }

            if (_PlayerProps != null && 
                (LiveStatus == StatusType.OnAir || LiveStatus == StatusType.ComingSoon)
                )
            {
                Debug.WriteLine(_PlayerProps.Program.BroadcastId);
                
                _StartTimeOffset = TimeSpan.Zero;

                LiveElapsedTimeFromOpen = DateTimeOffset.Now.ToOffset(JapanTimeZoneOffset) - _StartTimeOffset - OpenTime;
                LiveElapsedTime = DateTimeOffset.Now.ToOffset(JapanTimeZoneOffset) - _StartTimeOffset - StartTime;


                LivePlayerType = Live.LivePlayerType.Leo;

                if (Live2WebSocket == null)
                {
                    Live2WebSocket = new Live2WebSocket(_PlayerProps);
                    Live2WebSocket.RecieveCurrentStream += Live2WebSocket_RecieveCurrentStream;
                    Live2WebSocket.RecieveStatistics += Live2WebSocket_RecieveStatistics;
                    Live2WebSocket.RecieveDisconnect += Live2WebSocket_RecieveDisconnect;
                    Live2WebSocket.RecieveCurrentRoom += Live2WebSocket_RecieveCurrentRoom;
                    var quality = PlayerSettings.DefaultLiveQuality;

                    _IsLowLatency = PlayerSettings.LiveWatchWithLowLatency;
                    await Live2WebSocket.StartAsync(quality, _IsLowLatency);
                }

                await StartLiveViewing();
            }
            else if (_PlayerProps != null &&
                (LiveStatus == StatusType.Closed)
                )
            {
                // タイムシフト視聴
                await RefreshTimeshiftProgram();

                TimeshiftPosition = StartTime - OpenTime;

                _StartTimeOffset = DateTimeOffset.Now.ToOffset(JapanTimeZoneOffset) - OpenTime;

                // タイムシフトでの視聴かつタイムシフトの視聴予約済みかつ視聴権が未取得の場合は
                // 視聴権の使用を確認する
                if (_TimeshiftProgram?.GetReservationStatus() == Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.FIRST_WATCH)
                {
                    var dialog = App.Current.Container.Resolve<Services.DialogService>();


                    // 視聴権に関する詳細な情報提示

                    // 視聴権の利用期限は 24H＋放送時間 まで
                    // ただし公開期限がそれより先に来る場合には公開期限が視聴期限となる
                    var outdatedTime = DateTimeOffset.Now.ToOffset(JapanTimeZoneOffset) + (EndTime - StartTime) + TimeSpan.FromHours(24);
                    string desc = string.Empty;
                    if (outdatedTime > _TimeshiftProgram.ExpiredAt)
                    {
                        outdatedTime = _TimeshiftProgram.ExpiredAt.LocalDateTime;
                        desc = "Dialog_ConfirmTimeshiftWatch_WatchLimitByDate".Translate(_TimeshiftProgram.ExpiredAt.ToString("g"));
                    }
                    else
                    {
                        desc = "Dialog_ConfirmTimeshiftWatch_WatchLimitByDuration".Translate(outdatedTime.ToString("g"));
                    }
                    var result = await dialog.ShowMessageDialog(
                        desc
                        , _TimeshiftProgram.Title, "WatchLiveStreaming".Translate(), "Cancel".Translate()
                        );

                    if (result)
                    {
                        var token = await LoginUserLiveReservationProvider.GetReservationTokenAsync();

                        await Task.Delay(500);

                        await LoginUserLiveReservationProvider.UseReservationAsync(_TimeshiftProgram.Id, token);

                        await Task.Delay(3000);

                        // タイムシフト予約一覧を更新
                        // 視聴権利用を開始したアイテムがFIRST_WATCH以外の視聴可能を示すステータスになっているはず
                        await RefreshTimeshiftProgram();

                        Debug.WriteLine("use reservation after status: " + _TimeshiftProgram.Status);
                    }
                }

            }
            else
            {
                throw new NotSupportedException("RTMP による放送は対応していません。");
            }
        }

        #region ElapsedTime and Buffered comment 

        /// <summary>
        /// 生放送の再生時間をローカルで更新する頻度
        /// </summary>
        /// <remarks>コメント描画を120fpsで行えるように0.008秒で更新しています</remarks>
        public static TimeSpan LiveElapsedTimeUpdateInterval { get; private set; }
            = TimeSpan.FromSeconds(1);

        private TimeSpan? _LiveElapsedTime;
        public TimeSpan LiveElapsedTime
        {
            get { return _LiveElapsedTime ?? TimeSpan.Zero; }
            set { SetProperty(ref _LiveElapsedTime, value); }
        }

        private TimeSpan? _LiveElapsedTimeFromOpen;
        public TimeSpan LiveElapsedTimeFromOpen
        {
            get { return _LiveElapsedTimeFromOpen ?? TimeSpan.Zero; }
            set { SetProperty(ref _LiveElapsedTimeFromOpen, value); }
        }

        private TimeSpan? _DurationFromStart;
        public TimeSpan DurationFromStart
        {
            get { return (_DurationFromStart ?? (_DurationFromStart = (EndTime - StartTime))).Value; }
        }

        private TimeSpan? _DurationFromOpen;
        public TimeSpan DurationFromOpen
        {
            get { return (_DurationFromOpen ?? (_DurationFromOpen = (EndTime - OpenTime))).Value; }
        }

        private TimeSpan? _DurationBtwOpenToStart;
        public TimeSpan DurationBtwOpenToStart
        {
            get { return (_DurationBtwOpenToStart ?? (_DurationBtwOpenToStart = (StartTime - OpenTime))).Value; }
        }



        Models.Helpers.AsyncLock _LiveElapsedTimeUpdateTimerLock = new Models.Helpers.AsyncLock();

        private TimeSpan _StartTimeOffset;

        IDisposable _ElapsedTimerDisposer;
        private async Task StartLiveElapsedTimer()
        {
            await StopLiveElapsedTimer();

            using (var releaser = await _LiveElapsedTimeUpdateTimerLock.LockAsync())
            {
                _ElapsedTimerDisposer = Observable.Interval(TimeSpan.FromSeconds(1))
                    .SubscribeOnUIDispatcher()
                    .Subscribe(_ =>
                    {
                        _UIScheduler.Schedule(async () =>
                        {
                            await UpdateLiveElapsedTimeAsync();

                            var time = LiveElapsedTimeFromOpen ;
                            await ProcessingBufferedCommentsAsync(time);

                            using (await _LiveElapsedTimeUpdateTimerLock.LockAsync())
                            using (await _CommentRecievingLock.LockAsync())
                            {
                                // 表示向けコメントを表示区間を過ぎたら削除する
                                var positionFromOpenVpos = (int)(LiveElapsedTimeFromOpen.TotalMilliseconds * 0.1);
                                var commentDisplayDurationVpos = (int)(PlayerSettings.CommentDisplayDuration.TotalMilliseconds * 0.1);
                                var commentHiddenPositionVpos = positionFromOpenVpos - (commentDisplayDurationVpos + 500);
                                var removeComments = DisplayingComments.Where(x => x.VideoPosition < commentHiddenPositionVpos).ToArray();
                                if (removeComments.Any())
                                {
                                    DisplayingComments.RemoveRange(removeComments, System.Collections.Specialized.NotifyCollectionChangedAction.Remove);
                                }
                            }
                        });
                    });
            }

            Debug.WriteLine("live elapsed timer started.");
        }

        private async Task StopLiveElapsedTimer()
        {
            using (var releaser = await _LiveElapsedTimeUpdateTimerLock.LockAsync())
            {
                _ElapsedTimerDisposer?.Dispose();
                _ElapsedTimerDisposer = null;

                Debug.WriteLine("live elapsed timer stoped.");
            }
        }


        bool _IsEndMarked;

        /// <summary>
        /// 放送開始からの経過時間を更新します
        /// </summary>
        /// <param name="state">Timerオブジェクトのコールバックとして登録できるようにするためのダミー</param>
        private async Task UpdateLiveElapsedTimeAsync()
        {
            using (var releaser = await _LiveElapsedTimeUpdateTimerLock.LockAsync())
            {
                // ローカルの現在時刻から放送開始のベース時間を引いて
                // 放送経過時間の絶対値を求める
                if (IsWatchWithTimeshift)
                {
                    LiveElapsedTimeFromOpen = MediaPlayer.PlaybackSession.Position + (TimeshiftPosition ?? TimeSpan.Zero);
                    LiveElapsedTime = LiveElapsedTimeFromOpen - DurationBtwOpenToStart;

                }
                else
                {
                    LiveElapsedTimeFromOpen = DateTimeOffset.Now.ToOffset(JapanTimeZoneOffset) - _StartTimeOffset - OpenTime;
                    LiveElapsedTime = DateTimeOffset.Now.ToOffset(JapanTimeZoneOffset) - _StartTimeOffset - StartTime;
                }

                // 生放送の時間経過を通知
                LiveElapsed?.Invoke(this, LiveElapsedTime);


                var liveDuration = EndTime - StartTime;

                // TODO: 終了時刻を過ぎたら生放送情報を更新する
                if (!_IsEndMarked && liveDuration <= LiveElapsedTime)
                {
                    _IsEndMarked = true;
                }
            }

        }



        #endregion

        private async Task StartLiveViewing()
        {
            using (var releaser = await _LiveSubscribeLock.LockAsync())
            {
                // Display表示の維持リクエスト
                Services.Helpers.DisplayRequestHelper.RequestKeepDisplay();

                // 経過時間の更新とバッファしたコメントを送るタイマーを開始
                await StartLiveElapsedTimer();
            }
        }

        /// <summary>
        /// ニコ生からの離脱処理<br />
        /// HeartbeatAPIへの定期アクセスの停止、及びLeaveAPIへのアクセス
        /// </summary>
        /// <returns></returns>
        private async Task ExitLiveViewing()
        {
            using (var releaser = await _LiveSubscribeLock.LockAsync())
            {
                // Display表示の維持リクエストを停止
                Services.Helpers.DisplayRequestHelper.StopKeepDisplay();

                // HeartbeatAPIへのアクセスを停止
                EndCommentClientConnection();

                await StopLiveElapsedTimer();
            }
        }



        // 
        public async Task<Uri> MakeLiveSummaryHtmlUri()
        {
            if (_PlayerProps?.Program?.Description == null) { return null; }

            var desc = _PlayerProps.Program.Description;

            ApplicationTheme appTheme;
            if (_appearanceSettings.Theme == ElementTheme.Dark)
            {
                appTheme = ApplicationTheme.Dark;
            }
            else if (_appearanceSettings.Theme == ElementTheme.Light)
            {
                appTheme = ApplicationTheme.Light;
            }
            else
            {
                appTheme = Views.Helpers.SystemThemeHelper.GetSystemTheme();
            }

            return await Helpers.HtmlFileHelper.PartHtmlOutputToCompletlyHtml(LiveId, desc, appTheme);
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


        private TimeSpan? _TimeshiftPosition;
        public TimeSpan? TimeshiftPosition
        {
            get { return _TimeshiftPosition; }
            set { SetProperty(ref _TimeshiftPosition, value); }
        }


        public string[] Qualities { get; private set; }

        bool _IsLowLatency;

        public async Task ChangeQualityRequest(string quality, bool isLowLatency)
        {
            if (this.LivePlayerType == Live.LivePlayerType.Leo)
            {
                if (CurrentQuality == quality && _IsLowLatency == isLowLatency) { return; }

                if (IsWatchWithTimeshift)
                {
                    _LiveComments.Clear();
                    TimeshiftPosition = (TimeshiftPosition ?? TimeSpan.Zero) + MediaPlayer.PlaybackSession.Position;
                }

                MediaPlayer.Source = null;

                RequestQuality = quality;
                _IsLowLatency = isLowLatency;

                await Live2WebSocket.SendChangeQualityMessageAsync(quality, isLowLatency);
            }
        }


        private bool _IsFirstRecieveCurrentStream = true;
        private void Live2WebSocket_RecieveCurrentStream(Live2CurrentStreamEventArgs e)
        {
            Debug.WriteLine(e.Uri);

            _UIScheduler.Schedule(async () =>
            {
                _HLSUri = e.Uri;

                // Note: Hohoemaでは画質の自動設定 abr は扱いません
                Qualities = e.AvailableQualities.Where(x => x != "abr").ToArray();
                RaisePropertyChanged(nameof(Qualities));
                CurrentQuality = e.Quality;

                Debug.WriteLine(e.Quality);

                if (_IsFirstRecieveCurrentStream)
                {
                    _IsFirstRecieveCurrentStream = false;

                    OpenLive?.Invoke(this);
                }

                await RefreshLeoPlayer();
            });


        }


        private static string MakeSeekedHLSUri(string hlsUri, TimeSpan position)
        {
            if (position > TimeSpan.FromSeconds(1))
            {
                return hlsUri += $"&start={position.TotalSeconds.ToString("F2")}";
            }
            else
            {
                return hlsUri;
            }
        }


        private async Task RefreshLeoPlayer()
        {
            if (_HLSUri == null) { return; }

            await ClearLeoPlayer();


            _UIScheduler.Schedule(async () =>
            {
                try
                {
                    // 視聴開始後にスタート時間に自動シーク
                    string hlsUri = _HLSUri;
                    if (IsWatchWithTimeshift && TimeshiftPosition != null)
                    {
                        hlsUri = MakeSeekedHLSUri(_HLSUri, TimeshiftPosition.Value);
#if DEBUG
                        Debug.WriteLine(hlsUri);
#endif
                    }

                    var amsCreateResult = await AdaptiveMediaSource.CreateFromUriAsync(new Uri(hlsUri), NiconicoSession.Context.HttpClient);
                    if (amsCreateResult.Status == AdaptiveMediaSourceCreationStatus.Success)
                    {
                        var ams = amsCreateResult.MediaSource;
                        _MediaSource = MediaSource.CreateFromAdaptiveMediaSource(ams);
                        _AdaptiveMediaSource = ams;


                        ams.Diagnostics.DiagnosticAvailable += Diagnostics_DiagnosticAvailable;
                    }

                    MediaPlayer.Source = _MediaSource;

                    // タイムシフトで見ている場合はコメントのシークも行う
                    if (IsWatchWithTimeshift)
                    {
                        await ClearCommentsCacheAsync();
                        _LiveComments.Clear();
                        _NicoLiveCommentClient.Seek(TimeshiftPosition.Value);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            });
        }

        private void Diagnostics_DiagnosticAvailable(AdaptiveMediaSourceDiagnostics sender, AdaptiveMediaSourceDiagnosticAvailableEventArgs args)
        {
            Debug.WriteLine(args.DiagnosticType);
            if (args.ExtendedError != null)
            {
                Debug.WriteLine(args.ExtendedError.ToString());
            }
        }

        private async Task ClearLeoPlayer()
        {
            _UIScheduler.Schedule(() =>
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
                _AdaptiveMediaSource?.Dispose();
                _AdaptiveMediaSource = null;
            });

            await Task.CompletedTask;
        }


        private void Live2WebSocket_RecieveDisconnect()
        {

        }

        private void Live2WebSocket_RecieveStatistics(Live2StatisticsEventArgs e)
        {
            _UIScheduler.Schedule(() =>
            {
                WatchCount = (uint)e.ViewCount;
                CommentCount = (uint)e.CommentCount;
            });
        }


        private async void Live2WebSocket_RecieveCurrentRoom(Live2CurrentRoomEventArgs e)
        {
            RoomName = e.RoomName;

            if (e.MessageServerType == "niwavided")
            {
                if (IsWatchWithTimeshift)
                {
                    string waybackKey = await NicoLiveProvider.GetWaybackKeyAsync(e.ThreadId);

                    _NicoLiveCommentClient = new Niwavided.NiwavidedNicoTimeshiftCommentClient(
                        e.MessageServerUrl,
                        e.ThreadId,
                        NiconicoSession.UserIdString,
                        waybackKey,
                        OpenTime
                        );
                }
                else
                {
                    _NicoLiveCommentClient = new Niwavided.NiwavidedNicoLiveCommentClient(
                        e.MessageServerUrl,
                        e.ThreadId,
                        NiconicoSession.UserIdString,
                        NiconicoSession.Context.HttpClient
                        );
                }

                _NicoLiveCommentClient.Connected += _NicoLiveCommentClient_Connected;
                _NicoLiveCommentClient.Disconnected += _NicoLiveCommentClient_Disconnected;
                _NicoLiveCommentClient.CommentRecieved += _NicoLiveCommentClient_CommentRecieved;
                _NicoLiveCommentClient.CommentPosted += _NicoLiveCommentClient_CommentPosted;

                // コメントの受信処理と映像のオープンが被ると
                // 自動再生が失敗する？ので回避のため数秒遅らせる
                // タイムシフトコメントはStartTimeへのシーク後に取得されることを想定して
                if (!(_NicoLiveCommentClient is Niwavided.NiwavidedNicoTimeshiftCommentClient))
                {
                    _NicoLiveCommentClient.Open();

                    await Task.Delay(Services.Helpers.DeviceTypeHelper.IsMobile ? 3000 : 500);
                }
            }
        }

        #endregion



        #region PlayerStatusResponse projection Properties


        public string LiveTitle { get; private set; }
        public string BroadcasterId { get; private set; }
        public string BroadcasterName { get; private set; }
        public CommunityType? BroadcasterCommunityType { get; private set; }
        public Uri BroadcasterCommunityImageUri { get; private set; }
        public string BroadcasterCommunityId { get; private set; }




        #endregion



        public Task Refresh()
        {
            if (LivePlayerType == Live.LivePlayerType.Leo)
            {
                return RefreshLeoPlayer();
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        #region Live Comment 

        public bool CanPostComment => (LiveStatus == StatusType.OnAir || LiveStatus == StatusType.ComingSoon);

        private string _LastCommentText;
        private string _PostKey;

        public async Task PostComment(string message, string command, TimeSpan elapsedTime)
        {
            if (!CanPostComment)
            {
                PostCommentResult?.Invoke(this, false);
                return;
            }

            if (_NicoLiveCommentClient != null)
            {
                _LastCommentText = message;

                await UpdatePostKey();

                if (_PostKey == null)
                {
                    throw new Exception("failed post comment, postkey update failed, " + LiveId);
                }

                _NicoLiveCommentClient.PostComment(message, command, _PostKey, elapsedTime);
            }
        }

        private async Task UpdatePostKey()
        {
            if (_NicoLiveCommentClient is Niwavided.NiwavidedNicoLiveCommentClient)
            {
                var client = _NicoLiveCommentClient as Niwavided.NiwavidedNicoLiveCommentClient;
                _PostKey = await this.Live2WebSocket?.GetPostkeyAsync(client.CommentSessionInfo.ThreadId);
            }
        }

        Helpers.AsyncLock _CommentRecievingLock = new Helpers.AsyncLock();
        Dictionary<int, List<LiveChatData>> _TimeToCommentsDict = new Dictionary<int, List<LiveChatData>>();


        private static int VposToCacheDictTime(TimeSpan position)
        {
            return (int)position.TotalMilliseconds / 1000;
        }

        private static int VposToCacheDictTime(int vpos)
        {
            return vpos / 100;
        }

        private async Task AddCommentToCacheAsync(LiveChatData chat)
        {
            using (var releaser = await _CommentRecievingLock.LockAsync())
            {
                var keyVpos = VposToCacheDictTime(chat.Vpos);
                if (_TimeToCommentsDict.TryGetValue(keyVpos, out var alreadList))
                {
                    alreadList.Add(chat);
                }
                else
                {
                    var list = new List<LiveChatData>() { chat };
                    _TimeToCommentsDict.Add(keyVpos, list);
                }
            }
        }

        private async Task<IList<LiveChatData>> GetCommentsFromCacheAsync(TimeSpan positionFromOpen)
        {
            using (var releaser = await _CommentRecievingLock.LockAsync())
            {
                var keyVpos = VposToCacheDictTime(positionFromOpen);
                if (_TimeToCommentsDict.TryGetValue(keyVpos, out var list))
                {
                    return list;
                }
                else
                {
                    return new List<LiveChatData>();
                }
            }
        }

        private async Task RemoveCommentsFromCache(TimeSpan position)
        {
            using (var releaser = await _CommentRecievingLock.LockAsync())
            {
                var keyVpos = VposToCacheDictTime(position);
                foreach (var removeKey in _TimeToCommentsDict.Keys.Where(x => x < keyVpos).ToArray())
                {
                    _TimeToCommentsDict.Remove(removeKey);
                }
            }
        }

        private async Task ClearCommentsCacheAsync()
        {
            using (var releaser = await _CommentRecievingLock.LockAsync())
            {
                _TimeToCommentsDict.Clear();
            }
        }


        public ObservableRangeCollection<LiveComment> DisplayingComments { get; } = new ObservableRangeCollection<LiveComment>();

        private const int PreviousProcessingRangeCommentsTimeInSeconds = 3;

        private async Task ProcessingBufferedCommentsAsync(TimeSpan positionFromOpen)
        {
            var range = (int)PlayerSettings.CommentDisplayDuration.TotalSeconds + PreviousProcessingRangeCommentsTimeInSeconds;
            List<LiveChatData> candidateChatItems = new List<LiveChatData>();
            using (var releaser = await _CommentRecievingLock.LockAsync())
            {
                var keyVpos = VposToCacheDictTime(positionFromOpen);
                foreach (var key in Enumerable.Range(keyVpos - range, range))
                {
                    if (_TimeToCommentsDict.TryGetValue(key, out var list))
                    {
                        candidateChatItems.AddRange(list);
                    }
                }
            }

            var addComments = candidateChatItems.Select(x => ChatToComment(x)).ToArray();
            if (addComments.Any())
            {
                _LiveComments.AddRange(addComments);
            }

            await RemoveCommentsFromCache(positionFromOpen);
        }


        LiveComment ChatToComment(LiveChatData x)
        {
            var comment = new LiveComment();

            comment.VideoPosition = x.Vpos;

            comment.CommentText = x.Content;
            comment.CommentId = (uint)x.No;
            comment.IsAnonimity = x.IsAnonymity;
            comment.UserId = x.UserId;
            comment.IsOwnerComment = x.UserId == this.BroadcasterId;
            comment.IsOperationCommand = x.IsOperater && x.HasOperatorCommand;
            if (comment.IsOperationCommand)
            {
                comment.OperatorCommandType = x.OperatorCommandType;
                comment.OperatorCommandParameters = x.OperatorCommandParameters;
            }
            return comment;
        }

        private void _NicoLiveCommentClient_CommentPosted(object sender, CommentPostedEventArgs e)
        {
            _UIScheduler.Schedule(() =>
            {
                if (e.ChatResult == ChatResult.InvalidPostkey)
                {
                    _PostKey = null;
                }

                PostCommentResult?.Invoke(this, e.ChatResult == ChatResult.Success);
            });
        }

        private async void _NicoLiveCommentClient_CommentRecieved(object sender, CommentRecievedEventArgs e)
        {
            if (IsWatchWithTimeshift)
            {
                await AddCommentToCacheAsync(e.Chat);
            }
            else
            {
                var comment = ChatToComment(e.Chat);
                _LiveComments.Add(comment);

                if (LiveElapsedTime - TimeSpan.FromMilliseconds(e.Chat.Vpos * 100) < PlayerSettings.CommentDisplayDuration)
                {
                    if (!PlayerSettings.IsLiveNGComment(comment.UserId) && !comment.IsOperationCommand)
                    {
                        using (await _CommentRecievingLock.LockAsync())
                        {
                            DisplayingComments.Add(comment);
                        }
                    }
                }
            }
        }

        private void _NicoLiveCommentClient_Disconnected(object sender, CommentServerDisconnectedEventArgs e)
        {
        }


        private void _NicoLiveCommentClient_Connected(object sender, CommentServerConnectedEventArgs e)
        {

        }

        private void EndCommentClientConnection()
        {
            if (_NicoLiveCommentClient != null)
            {
                _NicoLiveCommentClient.Connected -= _NicoLiveCommentClient_Connected;
                _NicoLiveCommentClient.Disconnected -= _NicoLiveCommentClient_Disconnected;
                _NicoLiveCommentClient.CommentRecieved -= _NicoLiveCommentClient_CommentRecieved;
                _NicoLiveCommentClient.CommentPosted -= _NicoLiveCommentClient_CommentPosted;

                (_NicoLiveCommentClient as IDisposable)?.Dispose();

                _NicoLiveCommentClient = null;
            }
        }


        #endregion



    }



}
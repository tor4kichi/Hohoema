using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mntone.Nico2;
using Mntone.Nico2.Embed.Ichiba;
using Mntone.Nico2.Live.Recommend;
using Mntone.Nico2.Live.Video;
using NicoPlayerHohoema.Helpers;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Services;
using Prism.Commands;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Microsoft.Practices.Unity;

namespace NicoPlayerHohoema.ViewModels
{
    public sealed class LiveInfomationPageViewModel : HohoemaViewModelBase, Interfaces.ILiveContent
    {
        // TODO: 視聴開始（会場後のみ、チャンネル会員限定やチケット必要な場合あり）
        // TODO: タイムシフト予約（tsがある場合のみ、会場前のみ、プレミアムの場合は会場後でも可）
        // TODO: 後からタイムシフト予約（プレミアムの場合のみ）
        // TODO: 配信説明
        // TODO: タグ
        // TODO: 配信者（チャンネルやコミュニティ）の概要説明
        // TODO: 配信者（チャンネルやコミュニティ）のフォロー
        // TODO: オススメ生放送（放送中、放送予定を明示）
        // TODO: ニコニコ市場
        // TODO: SNS共有


        // gateとwatchをどちらも扱った上でその差を意識させないように表現する

        // LiveInfo.Video.CurrentStatusは開演前、放送中は時間の経過によって変化する可能性がある

        #region Interfaces.ILiveContent

        string Interfaces.ILiveContent.BroadcasterId => LiveInfo.Community?.GlobalId;

        string Interfaces.INiconicoContent.Id => LiveInfo.Video.Id;

        string Interfaces.INiconicoContent.Label => LiveInfo.Video.Title;

        #endregion

        public ReactiveProperty<bool> IsLoadFailed { get; }
        public ReactiveProperty<string> LoadFailedMessage { get; }

        private string _LiveId;
        public string LiveId
        {
            get { return _LiveId; }
            private set { SetProperty(ref _LiveId, value); }
        }

        public IReadOnlyReactiveProperty<bool> IsLiveIdAvairable { get; }

        private VideoInfo _LiveInfo;
        public VideoInfo LiveInfo
        {
            get { return _LiveInfo; }
            private set { SetProperty(ref _LiveInfo, value); }
        }


        // ログイン状態
        public IReadOnlyReactiveProperty<bool> IsPremiumAccount { get; }
        public IReadOnlyReactiveProperty<bool> IsLoggedIn { get; }


        // タイムシフト
        private ReactiveProperty<bool> _IsTsPreserved { get; }
        public IReadOnlyReactiveProperty<bool> IsTsPreserved => _IsTsPreserved;



        public IReadOnlyReactiveProperty<bool> IsShowOpenLiveContentButton { get; }
        public IReadOnlyReactiveProperty<bool> IsShowAddTimeshiftButton { get; }
        public IReadOnlyReactiveProperty<bool> IsShowDeleteTimeshiftButton { get; }


        private ObservableCollection<LiveTagViewModel> _LiveTags { get; } = new ObservableCollection<LiveTagViewModel>();
        public ReadOnlyObservableCollection<LiveTagViewModel> LiveTags { get; }


        private ObservableCollection<IchibaItem> _IchibaItems = new ObservableCollection<IchibaItem>();
        public ReadOnlyObservableCollection<IchibaItem> IchibaItems { get; }


        private ObservableCollection<LiveRecommendData> _ReccomendItems = new ObservableCollection<LiveRecommendData>();
        public ReadOnlyReactiveCollection<LiveInfoViewModel> ReccomendItems { get; }


        #region Commands

        private DelegateCommand _TogglePreserveTimeshift;
        public DelegateCommand TogglePreserveTimeshift
        {
            get
            {
                return _TogglePreserveTimeshift
                    ?? (_TogglePreserveTimeshift = new DelegateCommand(async () => 
                    {
                        if (!HohoemaApp.IsLoggedIn) { return; }

                        var reservations = await HohoemaApp.NiconicoContext.Live.GetReservationsAsync();
                        
                        if (reservations.Any(x => LiveId.EndsWith(x)))
                        {
                            var result = await DeleteReservation(LiveId, LiveInfo.Video.Title);
                            if (result)
                            {
                                await RefreshLiveInfoAsync();
                            }
                        }
                        else
                        {
                            var result = await AddReservation(LiveId, LiveInfo.Video.Title);
                            if (result)
                            {
                                await RefreshLiveInfoAsync();
                            }
                        }

                    }));
            }
        }

        private static async Task<bool> DeleteReservation(string liveId, string liveTitle)
        {
            if (string.IsNullOrEmpty(liveId)) { throw new ArgumentException(nameof(liveId)); }

            bool isDeleted = false;

            var hohoemaApp = App.Current.Container.Resolve<HohoemaApp>();
            var hohoemaDialogService = App.Current.Container.Resolve<HohoemaDialogService>();

            var token = await hohoemaApp.NiconicoContext.Live.GetReservationDeleteTokenAsync();

            if (token == null) { return isDeleted; }

            if (await hohoemaDialogService.ShowMessageDialog(
                $"{liveTitle}",
                "タイムシフト予約を削除しますか？"
                , "予約を削除"
                , "キャンセル"
                )
                )
            {
                await hohoemaApp.NiconicoContext.Live.DeleteReservationAsync(liveId, token);

                var deleteAfterReservations = await hohoemaApp.NiconicoContext.Live.GetReservationsAsync();

                isDeleted = !deleteAfterReservations.Any(x => liveId.EndsWith(x));
                if (isDeleted)
                {
                    // 削除成功
                    (App.Current as App).PublishInAppNotification(new InAppNotificationPayload()
                    {
                        Content = $"タイムシフト予約を削除しました。\r削除後の予約数は {deleteAfterReservations.Count}件 です。",
                        IsShowDismissButton = true,
                    });
                }
                else
                {
                    // まだ存在するゾイ
                    (App.Current as App).PublishInAppNotification(new InAppNotificationPayload()
                    {
                        Content = $"【失敗】タイムシフト予約を削除できませんでした。",
                        IsShowDismissButton = true,
                    });

                    Debug.Fail("タイムシフト削除に失敗しました: " + liveId);
                }
            }

            return isDeleted;

        }
        private static async Task<bool> AddReservation(string liveId, string liveTitle)
        {
            var hohoemaApp = App.Current.Container.Resolve<HohoemaApp>();
            var hohoemaDialogService = App.Current.Container.Resolve<HohoemaDialogService>();

            var result = await hohoemaApp.NiconicoContext.Live.ReservationAsync(liveId);

            bool isAdded = false;
            if (result.IsCanOverwrite)
            {
                // 予約数が上限到達、他のタイムシフトを削除すれば予約可能
                // いずれかの予約を削除するよう選択してもらう
                if (await hohoemaDialogService.ShowMessageDialog(
                       $"『{result.Data.Overwrite.Title}』を『{liveTitle}』のタイムシフト予約で上書きすると予約できます。\r（他のタイムシフトを削除したい場合はキャンセルしてタイムシフト一覧ページから操作してください。）",
                       "予約枠に空きがありません。古いタイムシフト予約を上書きしますか？"
                       , "予約を上書きする"
                       , "キャンセル"
                    ))
                {
                    result = await hohoemaApp.NiconicoContext.Live.ReservationAsync(liveId, isOverwrite: true);
                }
            }

            if (result.IsOK)
            {
                // 予約できてるはず
                // LiveInfoのタイムシフト周りの情報と共に通知
                (App.Current as App).PublishInAppNotification(new InAppNotificationPayload()
                {
                    Content = $"『{liveTitle}』のタイムシフトを予約しました。",
                });

                isAdded = true;
            }
            else if (result.IsCanOverwrite)
            {
                // 一つ前のダイアログで明示的的にキャンセルしてるはずなので特に通知を表示しない
            }
            else if (result.IsReservationDeuplicated)
            {
                (App.Current as App).PublishInAppNotification(new InAppNotificationPayload()
                {
                    Content = $"指定された放送は既にタイムシフト予約しています。",
                });
            }
            else if (result.IsReservationExpired)
            {
                (App.Current as App).PublishInAppNotification(new InAppNotificationPayload()
                {
                    Content = $"指定された放送はタイムシフト予約の期限を過ぎているため予約できませんでした。",
                });
            }

            return isAdded;
        }

        #endregion


        HohoemaDialogService _HohoemaDialogService;

        public LiveInfomationPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager, NiconicoContentProvider contentProvider, HohoemaDialogService dialogService)
            : base(hohoemaApp, pageManager)
        {
            _HohoemaDialogService = dialogService;

            IsLoadFailed = new ReactiveProperty<bool>(false)
               .AddTo(_CompositeDisposable);
            LoadFailedMessage = new ReactiveProperty<string>()
                .AddTo(_CompositeDisposable);



            IsLiveIdAvairable = this.ObserveProperty(x => x.LiveId)
                .Select(x => x != null ? NiconicoRegex.IsLiveId(x) : false)
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);



            IsPremiumAccount = Observable.CombineLatest(
                HohoemaApp.ObserveProperty(x => x.IsLoggedIn),
                HohoemaApp.ObserveProperty(x => x.IsPremiumUser)
                )
                .Select(x => x.All(y => y))
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);

            IsLoggedIn = HohoemaApp.ObserveProperty(x => x.IsLoggedIn)
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);


            _IsTsPreserved = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);

            LiveTags = new ReadOnlyObservableCollection<LiveTagViewModel>(_LiveTags);

            IchibaItems = new ReadOnlyObservableCollection<IchibaItem>(_IchibaItems);

            ReccomendItems = _ReccomendItems.ToReadOnlyReactiveCollection(x => new LiveInfoViewModel(x))
                .AddTo(_CompositeDisposable);

            IsShowOpenLiveContentButton = this.ObserveProperty(x => LiveInfo)
                .Select(x => 
                {
                    if (LiveInfo == null) { return false; }

                    if (HohoemaApp.IsPremiumUser)
                    {
                        if (LiveInfo.Video.OpenTime > DateTime.Now) { return false; }

                        return LiveInfo.Video.TimeshiftEnabled;
                    }
                    else 
                    {
                        // 一般アカウントは放送中のみ
                        if (LiveInfo.Video.OpenTime > DateTime.Now) { return false; }

                        if (_IsTsPreserved.Value) { return true; }

                        if (LiveInfo.Video.EndTime < DateTime.Now) { return false; }

                        return true;
                    }
                })
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);

            IsShowAddTimeshiftButton = this.ObserveProperty(x => LiveInfo)
                .Select(x =>
                {
                    if (LiveInfo == null) { return false; }
                    if (!LiveInfo.Video.TimeshiftEnabled) { return false; }
                    if (!HohoemaApp.IsLoggedIn) { return false; }

                    if (!HohoemaApp.IsPremiumUser)
                    {
                        // 一般アカウントは放送開始の30分前からタイムシフトの登録はできなくなる
                        if ((LiveInfo.Video.StartTime - TimeSpan.FromMinutes(30)) < DateTime.Now) { return false; }
                    }

                    if (LiveInfo.Video.TsArchiveEndTime != null
                        && LiveInfo.Video.TsArchiveEndTime > DateTime.Now) { return false; }

                    if (_IsTsPreserved.Value) { return false; }

                    return true;
                })
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);

            IsShowDeleteTimeshiftButton = _IsTsPreserved;

        }

        
        protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            LiveId = null;

            if (e.Parameter is string maybeLiveId)
            {
                if (NiconicoRegex.IsLiveId(maybeLiveId))
                {
                    LiveId = maybeLiveId;
                }
            }

            await RefreshLiveInfoAsync();

            await base.NavigatedToAsync(cancelToken, e, viewModelState);
        }

        private async Task RefreshLiveInfoAsync()
        {
            IsLoadFailed.Value = false;
            LoadFailedMessage.Value = string.Empty;

            try
            {
                if (LiveId == null) { throw new Exception("Require LiveId in LiveInfomationPage navigation with (e.Parameter as string)"); }

                var liveInfoResponse = await HohoemaApp.NiconicoContext.Live.GetLiveVideoInfoAsync(LiveId);

                if (!liveInfoResponse.IsOK)
                {
                    throw new Exception("Live not found. LiveId is " + LiveId);
                }

                LiveInfo = liveInfoResponse.VideoInfo;

                {
                    _LiveTags.Clear();

                    Func<string, LiveTagType, LiveTagViewModel> ConvertToLiveTagVM =
                        (x, type) => new LiveTagViewModel() { Tag = x, Type = type };

                    var tags = new[] {
                        LiveInfo.Livetags.Category?.Tags.Select(x => ConvertToLiveTagVM(x, LiveTagType.Category)),
                        LiveInfo.Livetags.Locked?.Tags.Select(x => ConvertToLiveTagVM(x, LiveTagType.Locked)),
                        LiveInfo.Livetags.Free?.Tags.Select(x => ConvertToLiveTagVM(x, LiveTagType.Free)),
                    }
                    .SelectMany(x => x ?? Enumerable.Empty<LiveTagViewModel>());

                    foreach (var tag in tags)
                    {
                        _LiveTags.Add(tag);
                    }

                    RaisePropertyChanged(nameof(LiveTags));
                }

                var reseevations = await HohoemaApp.NiconicoContext.Live.GetReservationsAsync();
                _IsTsPreserved.Value = reseevations.Any(x => LiveId.EndsWith(x));
            }
            catch (Exception ex)
            {
                IsLoadFailed.Value = true;
                LoadFailedMessage.Value = ex.Message;
            }



        }

        AsyncLock _IchibaUpdateLock = new AsyncLock();
        public bool IsIchibaInitialized { get; private set; } = false;
        public bool IsEmptyIchibaItems { get; private set; } = true;
        public async void InitializeIchibaItems()
        {
            using (var releaser = await _IchibaUpdateLock.LockAsync())
            {
                if (LiveInfo == null) { return; }

                if (IsIchibaInitialized) { return; }

                try
                {
                    var ichibaResponse = await HohoemaApp.NiconicoContext.Embed.GetIchiba(LiveInfo.Video.Id);
                    if (ichibaResponse != null)
                    {
                        var mainIchiba = ichibaResponse.GetMainIchibaItems() ?? new List<IchibaItem>();

                        foreach (var ichibaItem in mainIchiba)
                        {
                            _IchibaItems.Add(ichibaItem);
                        }

                        var pickupIchiba = ichibaResponse.GetPickupIchibaItems() ?? new List<IchibaItem>();
                        foreach (var ichibaItem in mainIchiba)
                        {
                            _IchibaItems.Add(ichibaItem);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
                finally
                {
                    IsEmptyIchibaItems = !_IchibaItems.Any();
                    RaisePropertyChanged(nameof(IsEmptyIchibaItems));

                    IsIchibaInitialized = true;
                    RaisePropertyChanged(nameof(IsIchibaInitialized));
                }
            }
        }


        AsyncLock _LiveRecommendLock = new AsyncLock();
        public bool IsLiveRecommendInitialized { get; private set; } = false;
        public bool IsEmptyLiveRecommendItems { get; private set; } = false;
        public async void InitializeLiveRecommend()
        {
            using (var releaser = await _LiveRecommendLock.LockAsync())
            {
                if (LiveInfo == null) { return; }

                if (IsLiveRecommendInitialized) { return; }

                try
                {
                    LiveRecommendResponse recommendResponse = null;
                    if (LiveInfo.Community?.GlobalId.StartsWith("co") ?? false)
                    {
                        recommendResponse = await HohoemaApp.NiconicoContext.Live.GetCommunityRecommendAsync(LiveInfo.Video.Id, LiveInfo.Community.GlobalId);
                    }
                    else
                    {
                        recommendResponse = await HohoemaApp.NiconicoContext.Live.GetOfficialOrChannelLiveRecommendAsync(LiveInfo.Video.Id);
                    }

                    foreach (var recommendItem in recommendResponse.RecommendItems)
                    {
                        _ReccomendItems.Add(recommendItem);
                    }

                    
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
                finally
                {
                    IsLiveRecommendInitialized = true;
                    RaisePropertyChanged(nameof(IsLiveRecommendInitialized));

                    IsEmptyLiveRecommendItems = !_ReccomendItems.Any();
                    RaisePropertyChanged(nameof(IsEmptyLiveRecommendItems));
                }
            }
        }
    }

    public enum LiveTagType
    {
        Category,
        Locked,
        Free,
    }

    public sealed class LiveTagViewModel
    {
        public string Tag { get; set; }
        public LiveTagType Type { get; set; }
    }



}

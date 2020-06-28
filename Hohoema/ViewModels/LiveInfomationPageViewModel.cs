﻿using I18NPortable;
using Mntone.Nico2;
using Mntone.Nico2.Embed.Ichiba;
using Mntone.Nico2.Live;
using Mntone.Nico2.Live.Recommend;
using Mntone.Nico2.Live.Video;
using Mntone.Nico2.Nicocas.Live;
using Hohoema.Interfaces;
using Hohoema.Models;
using Hohoema.Models.Helpers;
using Hohoema.Models.Provider;
using Hohoema.Services;
using Hohoema.Services.Page;
using Hohoema.UseCase;
using Hohoema.UseCase.NicoVideoPlayer.Commands;
using Hohoema.UseCase.Playlist;
using Prism.Commands;
using Prism.Navigation;
using Prism.Unity;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity;
using Windows.System;
using Windows.UI.Xaml;

namespace Hohoema.ViewModels
{
    public class LiveCommunityInfo : Interfaces.ICommunity
    {
        public string Id { get; set; }

        public string Label { get; set; }

        public string Thumbnail { get; set; }
        public string Description { get; internal set; }
    }

    public sealed class LiveInfomationPageViewModel : HohoemaViewModelBase, Interfaces.ILiveContent, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
    {
        HohoemaPin IPinablePage.GetPin()
        {
            return new HohoemaPin()
            {
                Label = LiveProgram?.Title,
                PageType = HohoemaPageType.LiveInfomation,
                Parameter = $"id={LiveId}"
            };
        }

        IObservable<string> ITitleUpdatablePage.GetTitleObservable()
        {
            return this.ObserveProperty(x => x.LiveProgram).Select(x => x?.Title);
        }

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

        public LiveInfomationPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            AppearanceSettings appearanceSettings,
            PageManager pageManager,
            Models.NiconicoSession niconicoSession,
            NicoLiveProvider nicoLiveProvider,
            DialogService dialogService,
            ExternalAccessService externalAccessService,
            OpenLiveContentCommand openLiveContentCommand
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            _appearanceSettings = appearanceSettings;
            PageManager = pageManager;
            NiconicoSession = niconicoSession;
            NicoLiveProvider = nicoLiveProvider;
            HohoemaDialogService = dialogService;
            ExternalAccessService = externalAccessService;
            OpenLiveContentCommand = openLiveContentCommand;
            IsLoadFailed = new ReactiveProperty<bool>(false)
               .AddTo(_CompositeDisposable);
            LoadFailedMessage = new ReactiveProperty<string>()
                .AddTo(_CompositeDisposable);



            IsLiveIdAvairable = this.ObserveProperty(x => x.LiveId)
                .Select(x => x != null ? NiconicoRegex.IsLiveId(x) : false)
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);



            IsLoggedIn = NiconicoSession.ObserveProperty(x => x.IsLoggedIn)
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);

            IsPremiumAccount = NiconicoSession.ObserveProperty(x => x.IsPremiumAccount)
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);



            _IsTsPreserved = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);

            LiveTags = new ReadOnlyObservableCollection<LiveTagViewModel>(_LiveTags);

            IchibaItems = new ReadOnlyObservableCollection<IchibaItem>(_IchibaItems);

            ReccomendItems = _ReccomendItems.ToReadOnlyReactiveCollection(x =>
            {
                var liveId = "lv" + x.ProgramId;
                var liveInfoVM = new LiveInfoListItemViewModel(liveId);
                liveInfoVM.Setup(x);

                var reserve = _Reservations?.ReservedProgram.FirstOrDefault(reservation => liveId == reservation.Id);
                if (reserve != null)
                {
                    liveInfoVM.SetReservation(reserve);
                }

                return liveInfoVM;
            })
                .AddTo(_CompositeDisposable);

            IsShowOpenLiveContentButton = 
                new[]
                {
                    this.ObserveProperty(x => LiveProgram).ToUnit(),
                    NiconicoSession.ObserveProperty(x => x.IsLoggedIn).ToUnit(),
                    _IsTsPreserved.ToUnit()
                }
                .Merge()
                .Select(x =>
                {
                    if (LiveProgram == null) { return false; }

                    if (NiconicoSession.IsPremiumAccount)
                    {
                        if (LiveProgram.OnAirTime.BeginAt > DateTime.Now) { return false; }

                        return LiveProgram.Timeshift.Enabled;
                    }
                    else
                    {
                        // 一般アカウントは放送中のみ
                        if (LiveProgram.OnAirTime.BeginAt > DateTime.Now) { return false; }

                        if (_IsTsPreserved.Value) { return true; }

                        if (LiveProgram.OnAirTime.EndAt < DateTime.Now) { return false; }

                        return true;
                    }
                })
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);

            IsShowAddTimeshiftButton = 
                new []
                {
                    this.ObserveProperty(x => LiveProgram).ToUnit(),
                    NiconicoSession.ObserveProperty(x => x.IsLoggedIn).ToUnit(),
                    _IsTsPreserved.ToUnit()
                }
                .Merge()
                .Select(x =>
                {
                    if (LiveProgram == null) { return false; }
                    if (!LiveProgram.Timeshift.Enabled) { return false; }
                    if (!NiconicoSession.IsLoggedIn) { return false; }

                    if (!NiconicoSession.IsPremiumAccount)
                    {
                        // 一般アカウントは放送開始の30分前からタイムシフトの登録はできなくなる
                        if ((LiveProgram.ShowTime.BeginAt - TimeSpan.FromMinutes(30)) < DateTime.Now) { return false; }
                    }

                    // 放送前、及び放送中はタイムシフトボタンを非表示…？
                    // プレ垢なら常に表示しておいていいんじゃないの？
                    //if (LiveProgram.ShowTime.EndAt > DateTime.Now) { return false; }

                    // タイムシフト予約済み
                    if (_IsTsPreserved.Value) { return false; }

                    return true;
                })
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);

            IsShowDeleteTimeshiftButton = _IsTsPreserved;

        }


        public DialogService HohoemaDialogService { get; }
        public ExternalAccessService ExternalAccessService { get; }
        public OpenLiveContentCommand OpenLiveContentCommand { get; }




        #region Interfaces.ILiveContent

        string Interfaces.ILiveContent.ProviderId => Community.Id;
        string Interfaces.ILiveContent.ProviderName => Community.Label;
        CommunityType Interfaces.ILiveContent.ProviderType => LiveProgram.CommunityType;


        string Interfaces.INiconicoObject.Id => LiveProgram.Id;

        string Interfaces.INiconicoObject.Label => LiveProgram.Title;

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

        private NicoCasLiveProgramData _LiveProgram;
        public NicoCasLiveProgramData LiveProgram
        {
            get { return _LiveProgram; }
            private set
            {
                if (SetProperty(ref _LiveProgram, value))
                {
                    LivePageUrl = _LiveProgram != null ? NiconicoUrls.LiveWatchPageUrl + LiveId : null;
                }
            }
        }

        // 放送説明
        private Uri _HtmlDescription;
        public Uri HtmlDescription
        {
            get { return _HtmlDescription; }
            private set { SetProperty(ref _HtmlDescription, value); }
        }


        // コミュニティ放送者情報
        private LiveCommunityInfo _Community;
        public LiveCommunityInfo Community
        {
            get { return _Community; }
            private set { SetProperty(ref _Community, value); }
        }

        private DateTime? _ExpiredTime;
        public DateTime? ExpiredTime
        {
            get { return _ExpiredTime; }
            private set { SetProperty(ref _ExpiredTime, value); }
        }


        private string _LivePageUrl;
        public string LivePageUrl
        {
            get { return _LivePageUrl; }
            private set { SetProperty(ref _LivePageUrl, value); }
        }

        private DelegateCommand<object> _ScriptNotifyCommand;
        public DelegateCommand<object> ScriptNotifyCommand
        {
            get
            {
                return _ScriptNotifyCommand
                    ?? (_ScriptNotifyCommand = new DelegateCommand<object>(async (parameter) =>
                    {
                        Uri url = parameter as Uri ?? (parameter as HyperlinkItem)?.Url;
                        if (url != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"script notified: {url}");

                            if (false == PageManager.OpenPage(url))
                            {
                                await Launcher.LaunchUriAsync(url);
                            }
                        }
                    }));
            }
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
        public ReadOnlyReactiveCollection<LiveInfoListItemViewModel> ReccomendItems { get; }


        #region Commands

        private DelegateCommand _TogglePreserveTimeshift;
        public DelegateCommand TogglePreserveTimeshift
        {
            get
            {
                return _TogglePreserveTimeshift
                    ?? (_TogglePreserveTimeshift = new DelegateCommand(async () => 
                    {
                        if (!NiconicoSession.IsLoggedIn) { return; }

                        var reservations = await NiconicoSession.Context.Live.GetReservationsAsync();
                        
                        if (reservations.Any(x => LiveId.EndsWith(x)))
                        {
                            var result = await DeleteReservation(LiveId, LiveProgram.Title);
                            if (result)
                            {
                                await RefreshReservationInfo(LiveId);
                            }
                        }
                        else
                        {
                            var result = await AddReservation(LiveId, LiveProgram.Title);
                            if (result)
                            {
                                await RefreshReservationInfo(LiveId);
                            }
                        }

                    }));
            }
        }

        private async Task<bool> DeleteReservation(string liveId, string liveTitle)
        {
            if (string.IsNullOrEmpty(liveId)) { throw new ArgumentException(nameof(liveId)); }

            bool isDeleted = false;

            var token = await NiconicoSession.Context.Live.GetReservationTokenAsync();

            if (token == null) { return isDeleted; }

            if (await HohoemaDialogService.ShowMessageDialog(
                $"{liveTitle}",
                "ConfirmDeleteTimeshift".Translate()
                , "DeleteTimeshift".Translate()
                , "Cancel".Translate()
                )
                )
            {
                await NiconicoSession.Context.Live.DeleteReservationAsync(liveId, token);

                var deleteAfterReservations = await NiconicoSession.Context.Live.GetReservationsAsync();

                isDeleted = !deleteAfterReservations.Any(x => liveId.EndsWith(x));
                if (isDeleted)
                {
                    // 削除成功
                    var notificationService = (App.Current as App).Container.Resolve<Services.NotificationService>();
                    notificationService.ShowInAppNotification(new InAppNotificationPayload()
                    {
                        Content = "InAppNotification_DeletedTimeshift".Translate(),
                        IsShowDismissButton = true,
                    });
                }
                else
                {
                    // まだ存在するゾイ
                    var notificationService = App.Current.Container.Resolve<Services.NotificationService>();
                    notificationService.ShowInAppNotification(new InAppNotificationPayload()
                    {
                        Content = "InAppNotification_FailedDeleteTimeshift".Translate(),
                        IsShowDismissButton = true,
                    });

                    Debug.Fail("タイムシフト削除に失敗しました: " + liveId);
                }
            }

            return isDeleted;

        }

        private async Task<bool> AddReservation(string liveId, string liveTitle)
        {
            var result = await NiconicoSession.Context.Live.ReservationAsync(liveId);

            bool isAdded = false;
            if (result.IsCanOverwrite)
            {
                // 予約数が上限到達、他のタイムシフトを削除すれば予約可能
                // いずれかの予約を削除するよう選択してもらう
                if (await HohoemaDialogService.ShowMessageDialog(
                       "DialogContent_ConfirmTimeshiftReservationiOverwrite".Translate(result.Data.Overwrite.Title, liveTitle),
                       "DialogTitle_ConfirmTimeshiftReservationiOverwrite".Translate(),
                       "Overwrite".Translate(),
                       "Cancel".Translate()
                    ))
                {
                    result = await NiconicoSession.Context.Live.ReservationAsync(liveId, isOverwrite: true);
                }
            }

            if (result.IsOK)
            {
                // 予約できてるはず
                // LiveInfoのタイムシフト周りの情報と共に通知
                var notificationService = (App.Current as App).Container.Resolve<Services.NotificationService>();
                notificationService.ShowInAppNotification(new InAppNotificationPayload()
                {
                    Content = "InAppNotification_AddedTimeshiftWithTitle".Translate(liveTitle),
                });

                isAdded = true;
            }
            else if (result.IsCanOverwrite)
            {
                // 一つ前のダイアログで明示的的にキャンセルしてるはずなので特に通知を表示しない
            }
            else if (result.IsReservationDeuplicated)
            {
                var notificationService = (App.Current as App).Container.Resolve<Services.NotificationService>();
                notificationService.ShowInAppNotification(new InAppNotificationPayload()
                {
                    Content = "InAppNotification_ExistTimeshift".Translate(),
                });
            }
            else if (result.IsReservationExpired)
            {
                var notificationService = (App.Current as App).Container.Resolve<Services.NotificationService>();
                notificationService.ShowInAppNotification(new InAppNotificationPayload()
                {
                    Content = "InAppNotification_TimeshiftExpired".Translate(),
                });
            }

            return isAdded;
        }

        #endregion


        
        Regex GeneralUrlRegex = new Regex(@"https?:\/\/([a-zA-Z0-9.\/?=_-]*)");
        public ObservableCollection<HyperlinkItem> DescriptionHyperlinkItems { get; } = new ObservableCollection<HyperlinkItem>();


        public async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            var liveId = parameters.GetValue<string>("id");
            if (liveId != null)
            {
                await RefreshLiveInfoAsync(liveId);
            }
            else
            {

            }
        }


        async Task RefreshLiveInfoAsync(string liveId)
        {
            IsLoadFailed.Value = false;
            LoadFailedMessage.Value = string.Empty;

            IsLiveInfoLoaded.Value = false;

            if (liveId == null) { throw new Exception("Require LiveId in LiveInfomationPage navigation with (e.Parameter as string)"); }

            try
            {
                var programInfo = await NiconicoSession.Context.Nicocas.GetLiveProgramAsync(liveId);
                if (programInfo.IsOK)
                {
                    await RefreshLiveTagsAsync(programInfo.Data.Tags);

                    await RefreshHtmlDescriptionAsync(programInfo.Data.Description);

                    var communityInfo = await NiconicoSession.Context.Community.GetCommunifyInfoAsync(programInfo.Data.SocialGroupId);
                    if (communityInfo.IsStatusOK)
                    {
                        var community = communityInfo.Community;
                        Community = new LiveCommunityInfo() 
                        { 
                            Id = community.GlobalId, 
                            Label = community.Name,
                            Thumbnail = community.Thumbnail,
                            Description = community.Description
                        };
                    }
                    else
                    {
                        Community = null;
                    }

                    await RefreshReservationInfo(liveId);

                    // タイムシフト視聴開始の判定処理のため_IsTsPreservedより後にLiveInfoを代入する
                    LiveProgram = programInfo.Data;

                    LiveId = liveId;
                }
                else
                {
                    throw new Exception("Live not found. LiveId is " + LiveId);
                }
            }
            catch (Exception ex)
            {
                IsLoadFailed.Value = true;
                LoadFailedMessage.Value = ex.Message;
            }
            finally
            {
                IsLiveInfoLoaded.Value = true;
            }
        }

        private async Task RefreshHtmlDescriptionAsync(string htmlDescription)
        {
            if (htmlDescription != null)
            {
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

                HtmlDescription = await Models.Helpers.HtmlFileHelper.PartHtmlOutputToCompletlyHtml(LiveId, htmlDescription, appTheme);

                try
                {
                    var htmlDocument = new HtmlAgilityPack.HtmlDocument();
                    htmlDocument.LoadHtml(htmlDescription);
                    var root = htmlDocument.DocumentNode;
                    var anchorNodes = root.Descendants("a");

                    foreach (var anchor in anchorNodes)
                    {
                        var url = new Uri(anchor.Attributes["href"].Value);
                        string label = null;
                        var text = anchor.InnerText;
                        if (string.IsNullOrWhiteSpace(text) || text.Contains('\n') || text.Contains('\r'))
                        {
                            label = url.OriginalString;
                        }
                        else
                        {
                            label = new string(anchor.InnerText.TrimStart(' ', '\n', '\r').TakeWhile(c => c != ' ' || c != '　').ToArray());
                        }

                        DescriptionHyperlinkItems.Add(new HyperlinkItem()
                        {
                            Label = label,
                            Url = url
                        });

                        Debug.WriteLine($"{anchor.InnerText} : {anchor.Attributes["href"].Value}");
                    }

                    /*
                    var matches = GeneralUrlRegex.Matches(HtmlDescription);
                    foreach (var match in matches.Cast<Match>())
                    {
                        if (!VideoDescriptionHyperlinkItems.Any(x => x.Url.OriginalString == match.Value))
                        {
                            VideoDescriptionHyperlinkItems.Add(new HyperlinkItem()
                            {
                                Label = match.Value,
                                Url = new Uri(match.Value)
                            });

                            Debug.WriteLine($"{match.Value} : {match.Value}");
                        }
                    }
                    */

                    RaisePropertyChanged(nameof(DescriptionHyperlinkItems));

                }
                catch
                {
                    Debug.WriteLine("動画説明からリンクを抜き出す処理に失敗");
                }

            }
        }


        public ReactiveProperty<bool> IsLiveInfoLoaded { get; } = new ReactiveProperty<bool>(false);
        private async Task RefreshLiveTagsAsync(IList<Tag> liveTags)
        {
            _LiveTags.Clear();

            Func<Tag, LiveTagType, LiveTagViewModel> ConvertToLiveTagVM =
                (x, type) => new LiveTagViewModel() { Tag = x.Text, Type = type };

            var tags = new[] {
                    liveTags.Where(x => x.IsCategoryTag).Select(x => ConvertToLiveTagVM(x, LiveTagType.Category)),
                    liveTags.Where(x => x.IsLocked).Select(x => ConvertToLiveTagVM(x, LiveTagType.Locked)),
                    liveTags.Where(x => !x.IsCategoryTag && !x.IsLocked).Select(x => ConvertToLiveTagVM(x, LiveTagType.Free)),
                }
            .SelectMany(x => x ?? Enumerable.Empty<LiveTagViewModel>());

            foreach (var tag in tags)
            {
                _LiveTags.Add(tag);
            }

            RaisePropertyChanged(nameof(LiveTags));
        }

        async Task RefreshReservationInfo(string liveId)
        {
            var reseevations = await NiconicoSession.Context.Live.GetReservationsInDetailAsync();
            var thisLiveReservation = reseevations.ReservedProgram.FirstOrDefault(x => liveId.EndsWith(x.Id));
            if (thisLiveReservation != null)
            {
                var timeshiftList = await NiconicoSession.Context.Live.GetMyTimeshiftListAsync();
                ExpiredTime = (timeshiftList.Items.FirstOrDefault(x => x.Id == liveId)?.WatchTimeLimit ?? thisLiveReservation.ExpiredAt).LocalDateTime;
            }

            _IsTsPreserved.Value = thisLiveReservation != null;
        }

        AsyncLock _IchibaUpdateLock = new AsyncLock();
        public bool IsIchibaInitialized { get; private set; } = false;
        public bool IsEmptyIchibaItems { get; private set; } = true;
        public async void InitializeIchibaItems()
        {
            using (var releaser = await _IchibaUpdateLock.LockAsync())
            {
                if (LiveProgram == null) { return; }

                if (IsIchibaInitialized) { return; }

                try
                {
                    var ichibaResponse = await NiconicoSession.Context.Embed.GetIchiba(LiveProgram.Id);
                    if (ichibaResponse != null)
                    {
                        foreach (var ichibaItem in ichibaResponse.GetMainIchibaItems() ?? Enumerable.Empty<IchibaItem>())
                        {
                            _IchibaItems.Add(ichibaItem);
                        }

                        foreach (var ichibaItem in ichibaResponse.GetPickupIchibaItems() ?? Enumerable.Empty<IchibaItem>())
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

        Mntone.Nico2.Live.ReservationsInDetail.ReservationsInDetailResponse _Reservations;

        AsyncLock _LiveRecommendLock = new AsyncLock();
        private readonly AppearanceSettings _appearanceSettings;

        public bool IsLiveRecommendInitialized { get; private set; } = false;
        public bool IsEmptyLiveRecommendItems { get; private set; } = false;
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public PageManager PageManager { get; }
        public Models.NiconicoSession NiconicoSession { get; }
        public NicoLiveProvider NicoLiveProvider { get; }

        public async void InitializeLiveRecommend()
        {
            using (var releaser = await _LiveRecommendLock.LockAsync())
            {
                if (LiveProgram == null) { return; }

                if (IsLiveRecommendInitialized) { return; }

                try
                {
                    if (NiconicoSession.IsLoggedIn)
                    {
                        _Reservations = await NiconicoSession.Context.Live.GetReservationsInDetailAsync();
                    }
                }
                catch { }

                try
                {
                    LiveRecommendResponse recommendResponse = null;
                    if (LiveProgram?.SocialGroupId.StartsWith("co") ?? false)
                    {
                        recommendResponse = await NiconicoSession.Context.Live.GetCommunityRecommendAsync(LiveProgram.Id, LiveProgram.SocialGroupId);
                    }
                    else
                    {
                        recommendResponse = await NiconicoSession.Context.Live.GetOfficialOrChannelLiveRecommendAsync(LiveProgram.Id);
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

        private static DelegateCommand<LiveTagViewModel> _SearchLiveTagCommand;
        public DelegateCommand<LiveTagViewModel> SearchLiveTagCommand
        {
            get
            {
                return _SearchLiveTagCommand
                    ?? (_SearchLiveTagCommand = new DelegateCommand<LiveTagViewModel>((tagVM) => 
                    {
                        var pageManager = App.Current.Container.Resolve<PageManager>();
                        pageManager.Search(SearchTarget.Niconama, tagVM.Tag);
                    }));
            }
        }
    }
}

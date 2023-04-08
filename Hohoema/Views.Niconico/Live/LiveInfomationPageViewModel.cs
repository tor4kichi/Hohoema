using I18NPortable;
using NiconicoToolkit.Live;
using Hohoema.Models;
using Hohoema.Helpers;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Community;
using Hohoema.Models.Niconico.Live;
using Hohoema.Models.PageNavigation;
using Hohoema.Services;
using Hohoema.Services;
using Hohoema.Services.Navigations;
using CommunityToolkit.Mvvm.Input;
using Hohoema.Services.Navigations;
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
using Windows.System;
using Windows.UI.Xaml;
using NiconicoSession = Hohoema.Models.Niconico.NiconicoSession;
using Hohoema.Models.Application;
using Hohoema.ViewModels.Niconico.Live;
using Hohoema.Models.Pins;
using Hohoema.ViewModels.Niconico.Share;
using Hohoema.ViewModels.Pages.Niconico.Video;
using NiconicoToolkit.Ichiba;
using NiconicoToolkit.Live.Timeshift;
using NiconicoToolkit;
using NiconicoToolkit.Recommend;
using AngleSharp.Html.Parser;
using NiconicoToolkit.Community;

namespace Hohoema.ViewModels.Pages.Niconico.Live
{
    public class LiveCommunityInfo : ICommunity
    {
        public CommunityId CommunityId { get; set; }

        public string Name { get; set; }

        public string Thumbnail { get; set; }
        public string Description { get; internal set; }
    }

    public class LiveData : ILiveContent, ILiveContentProvider
    {
        private readonly NiconicoToolkit.Live.Cas.LiveProgramData _liveProgram;

        public LiveData(NiconicoToolkit.Live.Cas.LiveProgramData liveProgram, string providerName)
        {
            _liveProgram = liveProgram;
            ProviderName = providerName;
        }

        string ILiveContentProvider.ProviderId => _liveProgram.ProviderId;
        public string ProviderName { get; }
        ProviderType ILiveContentProvider.ProviderType => _liveProgram.ProviderType;


        LiveId ILiveContent.LiveId => _liveProgram.Id;

        string INiconicoContent.Title => _liveProgram.Title;

    }

    public sealed class LiveInfomationPageViewModel : HohoemaPageViewModelBase, IPinablePage, ITitleUpdatablePage
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
            NiconicoSession niconicoSession,
            NicoLiveProvider nicoLiveProvider,
            DialogService dialogService,
            OpenLiveContentCommand openLiveContentCommand,
            OpenLinkCommand openLinkCommand,
            CopyToClipboardCommand copyToClipboardCommand,
            CopyToClipboardWithShareTextCommand copyToClipboardWithShareTextCommand
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            _appearanceSettings = appearanceSettings;
            PageManager = pageManager;
            NiconicoSession = niconicoSession;
            NicoLiveProvider = nicoLiveProvider;
            HohoemaDialogService = dialogService;
            OpenLiveContentCommand = openLiveContentCommand;
            OpenLinkCommand = openLinkCommand;
            CopyToClipboardCommand = copyToClipboardCommand;
            CopyToClipboardWithShareTextCommand = copyToClipboardWithShareTextCommand;
            IsLoadFailed = new ReactiveProperty<bool>(false)
               .AddTo(_CompositeDisposable);
            LoadFailedMessage = new ReactiveProperty<string>()
                .AddTo(_CompositeDisposable);



            IsLiveIdAvairable = this.ObserveProperty(x => x.LiveId)
                .Select(x => x != default(LiveId))
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

            /*
            ReccomendItems = _ReccomendItems.ToReadOnlyReactiveCollection(x =>
            {
                var liveId = x.ProgramId;
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
            */

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
        public OpenLiveContentCommand OpenLiveContentCommand { get; }
        public OpenLinkCommand OpenLinkCommand { get; }
        public CopyToClipboardCommand CopyToClipboardCommand { get; }
        public CopyToClipboardWithShareTextCommand CopyToClipboardWithShareTextCommand { get; }




        #region ILiveContent

        #endregion

        public ReactiveProperty<bool> IsLoadFailed { get; }
        public ReactiveProperty<string> LoadFailedMessage { get; }

        private LiveId _LiveId;
        public LiveId LiveId
        {
            get { return _LiveId; }
            private set { SetProperty(ref _LiveId, value); }
        }

        public IReadOnlyReactiveProperty<bool> IsLiveIdAvairable { get; }

        private NiconicoToolkit.Live.Cas.LiveProgramData _LiveProgram;
        public NiconicoToolkit.Live.Cas.LiveProgramData LiveProgram
        {
            get { return _LiveProgram; }
            private set
            {
                if (SetProperty(ref _LiveProgram, value))
                {
                    LivePageUrl = _LiveProgram != null ? NiconicoUrls.MakeLiveWatchPageUrl(LiveId) : null;
                }
            }
        }

        // 放送説明
        private string _HtmlDescription;
        public string HtmlDescription
        {
            get { return _HtmlDescription; }
            private set { SetProperty(ref _HtmlDescription, value); }
        }

        private LiveData _Live;
        public LiveData Live
        {
            get { return _Live; }
            set { SetProperty(ref _Live, value); }
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
        

        private string _timeshiftStatus;
        public string TimeshiftStatus
        {
            get { return _timeshiftStatus; }
            set { SetProperty(ref _timeshiftStatus, value); }
        }


        private string _LivePageUrl;
        public string LivePageUrl
        {
            get { return _LivePageUrl; }
            private set { SetProperty(ref _LivePageUrl, value); }
        }

        private RelayCommand<object> _ScriptNotifyCommand;
        public RelayCommand<object> ScriptNotifyCommand
        {
            get
            {
                return _ScriptNotifyCommand
                    ?? (_ScriptNotifyCommand = new RelayCommand<object>(async (parameter) =>
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


        private ObservableCollection<LiveRecommendItem> _ReccomendItems = new ObservableCollection<LiveRecommendItem>();
        public ReadOnlyReactiveCollection<LiveInfoListItemViewModel> ReccomendItems { get; }


        #region Commands

        private RelayCommand _TogglePreserveTimeshift;
        public RelayCommand TogglePreserveTimeshift
        {
            get
            {
                return _TogglePreserveTimeshift
                    ?? (_TogglePreserveTimeshift = new RelayCommand(async () => 
                    {
                        if (!NiconicoSession.IsLoggedIn) { return; }

                        var reservations = await NiconicoSession.ToolkitContext.Timeshift.GetTimeshiftReservationsDetailAsync();
                        
                        if (reservations.Data.Items.Any(x => x.LiveId == LiveId))
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

            var token = await NiconicoSession.ToolkitContext.Timeshift.GetReservationTokenAsync();

            if (token == null) { return isDeleted; }

            if (await HohoemaDialogService.ShowMessageDialog(
                $"{liveTitle}",
                "ConfirmDeleteTimeshift".Translate()
                , "DeleteTimeshift".Translate()
                , "Cancel".Translate()
                )
                )
            {
                await NiconicoSession.ToolkitContext.Timeshift.DeleteTimeshiftReservationAsync(liveId, token);

                var deleteAfterReservations = await NiconicoSession.ToolkitContext.Timeshift.GetTimeshiftReservationsDetailAsync();

                isDeleted = !deleteAfterReservations.Data.Items.Any(x => liveId.EndsWith(x.LiveId));
                if (isDeleted)
                {
                    // 削除成功
                    var notificationService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<Services.NotificationService>();
                    notificationService.ShowLiteInAppNotification_Success("InAppNotification_DeletedTimeshift".Translate());
                }
                else
                {
                    // まだ存在するゾイ
                    var notificationService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<Services.NotificationService>();
                    notificationService.ShowLiteInAppNotification_Fail("InAppNotification_FailedDeleteTimeshift".Translate());

                    Debug.Fail("タイムシフト削除に失敗しました: " + liveId);
                }
            }

            return isDeleted;

        }

        private async Task<bool> AddReservation(string liveId, string liveTitle)
        {
            var result = await NiconicoSession.ToolkitContext.Timeshift.ReserveTimeshiftAsync(liveId, overwrite: false);

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
                    result = await NiconicoSession.ToolkitContext.Timeshift.ReserveTimeshiftAsync(liveId, overwrite: true);
                }
            }

            if (result.IsSuccess)
            {
                // 予約できてるはず
                // LiveInfoのタイムシフト周りの情報と共に通知
                var notificationService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<Services.NotificationService>();
                notificationService.ShowLiteInAppNotification_Success("InAppNotification_AddedTimeshiftWithTitle".Translate(liveTitle));

                isAdded = true;
            }
            else if (result.IsCanOverwrite)
            {
                // 一つ前のダイアログで明示的的にキャンセルしてるはずなので特に通知を表示しない
            }
            else if (result.IsReservationDeuplicated)
            {
                var notificationService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<Services.NotificationService>();
                notificationService.ShowLiteInAppNotification_Success("InAppNotification_ExistTimeshift".Translate());
            }
            else if (result.IsReservationExpired)
            {
                var notificationService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<Services.NotificationService>();
                notificationService.ShowLiteInAppNotification_Fail("InAppNotification_TimeshiftExpired".Translate());
            }

            return isAdded;
        }

        #endregion


        
        Regex GeneralUrlRegex = new Regex(@"https?:\/\/([a-zA-Z0-9.\/?=_-]*)");
        public ObservableCollection<HyperlinkItem> DescriptionHyperlinkItems { get; } = new ObservableCollection<HyperlinkItem>();


        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            await base.OnNavigatedToAsync(parameters);

            LiveId? maybeLiveId = null;
            if (parameters.TryGetValue("id", out string strLiveId))
            {
                maybeLiveId = strLiveId;
            }
            else if (parameters.TryGetValue("id", out uint numberLiveId))
            {
                maybeLiveId = numberLiveId;
            }
            else if (parameters.TryGetValue("id", out LiveId justLiveId))
            {
                maybeLiveId = justLiveId;
            }

            if (maybeLiveId == null)
            {
                LiveId = new LiveId();
                return;
            }

            LiveId = maybeLiveId.Value;

            await RefreshLiveInfoAsync(LiveId);
        }


        async Task RefreshLiveInfoAsync(LiveId liveId)
        {
            using var _ = await _UpdateLock.LockAsync();

            IsLoadFailed.Value = false;
            LoadFailedMessage.Value = string.Empty;

            IsLiveInfoLoaded.Value = false;

            if (liveId == default(LiveId)) { throw new Infra.HohoemaException("Require LiveId in LiveInfomationPage navigation with (e.Parameter as string)"); }

            try
            {
                var programInfo = await NiconicoSession.ToolkitContext.Live.CasApi.GetLiveProgramAsync(liveId);
                if (programInfo.IsSuccess)
                {
                    await RefreshLiveTagsAsync(programInfo.Data.Tags);

                    await RefreshHtmlDescriptionAsync(programInfo.Data.Description);

                    if (programInfo.Data.ProviderType == ProviderType.Community)
                    {
                        var communityInfo = await NiconicoSession.ToolkitContext.Community.GetCommunityInfoAsync(programInfo.Data.SocialGroupId);
                        if (communityInfo.IsOK)
                        {
                            var community = communityInfo.Community;
                            Community = new LiveCommunityInfo()
                            {
                                CommunityId = community.GlobalId,
                                Name = community.Name,
                                Thumbnail = community.ThumbnailNonSsl.OriginalString,
                                Description = community.Description
                            };
                        }
                        else
                        {
                            Community = null;
                        }
                    }

                    await RefreshReservationInfo(liveId);

                    // タイムシフト視聴開始の判定処理のため_IsTsPreservedより後にLiveInfoを代入する
                    LiveProgram = programInfo.Data;
                    Live = new LiveData(programInfo.Data, Community?.Name);
                    LiveId = liveId;
                }
                else
                {
                    throw new Infra.HohoemaException("Live not found. LiveId is " + LiveId);
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
                if (_appearanceSettings.ApplicationTheme == ElementTheme.Dark)
                {
                    appTheme = ApplicationTheme.Dark;
                }
                else if (_appearanceSettings.ApplicationTheme == ElementTheme.Light)
                {
                    appTheme = ApplicationTheme.Light;
                }
                else
                {
                    appTheme = Views.Helpers.SystemThemeHelper.GetSystemTheme();
                }

                HtmlDescription = await HtmlFileHelper.ToCompletlyHtmlAsync(htmlDescription, appTheme);

                try
                {
                    HtmlParser htmlParser = new HtmlParser();
                    using var document = await htmlParser.ParseDocumentAsync(htmlDescription);
                    var anchorNodes = document.QuerySelectorAll("a");

                    foreach (var anchor in anchorNodes)
                    {
                        var url = new Uri(anchor.Attributes["href"].Value);
                        string label = null;
                        var text = anchor.TextContent;
                        if (string.IsNullOrWhiteSpace(text) || text.Contains('\n') || text.Contains('\r'))
                        {
                            label = url.OriginalString;
                        }
                        else
                        {
                            label = new string(anchor.TextContent.TrimStart(' ', '\n', '\r').TakeWhile(c => c != ' ' || c != '　').ToArray());
                        }

                        DescriptionHyperlinkItems.Add(new HyperlinkItem()
                        {
                            Label = label,
                            Url = url
                        });

                        Debug.WriteLine($"{anchor.TextContent} : {anchor.Attributes["href"].Value}");
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

                    OnPropertyChanged(nameof(DescriptionHyperlinkItems));

                }
                catch
                {
                    Debug.WriteLine("動画説明からリンクを抜き出す処理に失敗");
                }

            }
        }


        public ReactiveProperty<bool> IsLiveInfoLoaded { get; } = new ReactiveProperty<bool>(false);
        private async Task RefreshLiveTagsAsync(IList<NiconicoToolkit.Live.Cas.Tag> liveTags)
        {
            _LiveTags.Clear();

            Func<NiconicoToolkit.Live.Cas.Tag, LiveTagType, LiveTagViewModel> ConvertToLiveTagVM =
                (x, type) => new LiveTagViewModel() { Tag = x.Text, Type = type };

            var tags = new[] {
                    liveTags.Where(x => x.Type == NiconicoToolkit.Live.Cas.TagType.Category).Select(x => ConvertToLiveTagVM(x, LiveTagType.Category)),
                    liveTags.Where(x => x.IsLocked).Select(x => ConvertToLiveTagVM(x, LiveTagType.Locked)),
                    liveTags.Where(x => x.Type == NiconicoToolkit.Live.Cas.TagType.Normal && !x.IsLocked).Select(x => ConvertToLiveTagVM(x, LiveTagType.Free)),
                }
            .SelectMany(x => x ?? Enumerable.Empty<LiveTagViewModel>());

            foreach (var tag in tags)
            {
                _LiveTags.Add(tag);
            }

            OnPropertyChanged(nameof(LiveTags));
        }

        async Task RefreshReservationInfo(LiveId liveId)
        {
            var reseevations = await NiconicoSession.ToolkitContext.Timeshift.GetTimeshiftReservationsDetailAsync();
            var thisLiveReservation = reseevations.Data.Items.FirstOrDefault(x => x.LiveId == liveId);
            if (thisLiveReservation != null)
            {
                var timeshiftList = await NiconicoSession.ToolkitContext.Timeshift.GetTimeshiftReservationsAsync();
                TimeshiftStatus = timeshiftList.Reservations.Items.FirstOrDefault(x => x.ProgramId == liveId).TimeshiftSetting.Status.ToString();
            }

            _IsTsPreserved.Value = thisLiveReservation != null;
        }

        public bool IsIchibaInitialized { get; private set; } = false;
        public bool IsEmptyIchibaItems { get; private set; } = true;
        public async void InitializeIchibaItems()
        {
            using (var releaser = await _UpdateLock.LockAsync())
            {
                if (LiveProgram == null) { return; }

                if (IsIchibaInitialized) { return; }

                try
                {
                    var ichibaResponse = await NiconicoSession.ToolkitContext.Ichiba.GetIchibaItemsAsync(LiveProgram.Id);
                    if (ichibaResponse != null)
                    {
                        foreach (var ichibaItem in ichibaResponse.MainItems ?? Enumerable.Empty<IchibaItem>())
                        {
                            _IchibaItems.Add(ichibaItem);
                        }

                        foreach (var ichibaItem in ichibaResponse.PickupItems ?? Enumerable.Empty<IchibaItem>())
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
                    OnPropertyChanged(nameof(IsEmptyIchibaItems));

                    IsIchibaInitialized = true;
                    OnPropertyChanged(nameof(IsIchibaInitialized));
                }
            }
        }

        TimeshiftReservationsDetailResponse _Reservations;

        AsyncLock _UpdateLock = new AsyncLock();
        private readonly AppearanceSettings _appearanceSettings;

        public bool IsLiveRecommendInitialized { get; private set; } = false;
        public bool IsEmptyLiveRecommendItems { get; private set; } = false;
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public PageManager PageManager { get; }
        public NiconicoSession NiconicoSession { get; }
        public NicoLiveProvider NicoLiveProvider { get; }

        public async void InitializeLiveRecommend()
        {
            using (var releaser = await _UpdateLock.LockAsync())
            {
                if (LiveProgram == null) { return; }

                if (IsLiveRecommendInitialized) { return; }

                try
                {
                    if (NiconicoSession.IsLoggedIn)
                    {
                        _Reservations = await NiconicoSession.ToolkitContext.Timeshift.GetTimeshiftReservationsDetailAsync();
                    }
                }
                catch { }

                try
                {
                    LiveRecommendResponse recommendResponse = null;
                    if (LiveProgram?.SocialGroupId.StartsWith("co") ?? false)
                    {
                        recommendResponse = await NiconicoSession.ToolkitContext.Recommend.GetLiveRecommendForUserAsync(LiveProgram.Id, LiveProgram.ProviderId);
                    }
                    else
                    {
                        recommendResponse = await NiconicoSession.ToolkitContext.Recommend.GetLiveRecommendForChannelAsync(LiveProgram.Id, LiveProgram.SocialGroupId);
                    }

                    if (recommendResponse.IsSuccess)
                    {
                        foreach (var recommendItem in recommendResponse.Data.Items)
                        {
                            _ReccomendItems.Add(recommendItem);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
                finally
                {
                    IsLiveRecommendInitialized = true;
                    OnPropertyChanged(nameof(IsLiveRecommendInitialized));

                    IsEmptyLiveRecommendItems = !_ReccomendItems.Any();
                    OnPropertyChanged(nameof(IsEmptyLiveRecommendItems));
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

        private static RelayCommand<LiveTagViewModel> _SearchLiveTagCommand;
        public RelayCommand<LiveTagViewModel> SearchLiveTagCommand
        {
            get
            {
                return _SearchLiveTagCommand
                    ?? (_SearchLiveTagCommand = new RelayCommand<LiveTagViewModel>((tagVM) => 
                    {
                        if (tagVM != null)
                        {
                            var pageManager = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<PageManager>();
                            pageManager.Search(SearchTarget.Niconama, tagVM.Tag);
                        }
                    }));
            }
        }
    }
}

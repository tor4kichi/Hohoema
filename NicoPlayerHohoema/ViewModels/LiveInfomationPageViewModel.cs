using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mntone.Nico2;
using NicoPlayerHohoema.Models;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace NicoPlayerHohoema.ViewModels
{
    public sealed class LiveInfomationPageViewModel : HohoemaViewModelBase
    {
        enum LiveStatusType
        {
            Preserved,
            Open,
            Start,
            Close,
            CloseWithArchive,
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

        public ReactiveProperty<bool> IsLoadFailed { get; }
        public ReactiveProperty<string> LoadFailedMessage { get; }


        private string _LiveId;
        public string LiveId
        {
            get { return _LiveId; }
            private set { SetProperty(ref _LiveId, value); }
        }

        public ReadOnlyReactiveProperty<bool> IsLiveIdAvairable { get; }

        string _LiveDescriptionText;
        public string LiveDescriptionText
        {
            get { return _LiveDescriptionText; }
            private set { SetProperty(ref _LiveDescriptionText, value); }
        }

        private ReactiveProperty<LiveStatusType> LiveStatus { get; }
        public ReadOnlyReactiveProperty<bool> IsLivePreserved { get; }
        public ReadOnlyReactiveProperty<bool> IsLiveOpened { get; }
        public ReadOnlyReactiveProperty<bool> IsLiveStarted { get; }
        public ReadOnlyReactiveProperty<bool> IsLiveClosed { get; }
        public ReadOnlyReactiveProperty<bool> IsLiveArchive { get; }

        private bool _IsTsAvairable;
        public bool IsTsAvairable
        {
            get { return _IsTsAvairable; }
            private set { SetProperty(ref _IsTsAvairable, value); }
        }

        private bool _IsTsPreserved;
        public bool IsTsPreserved
        {
            get { return _IsTsPreserved; }
            private set { SetProperty(ref _IsTsPreserved, value); }
        }





        NiconicoContentProvider ContentProvider;

        public LiveInfomationPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager) 
            : base(hohoemaApp, pageManager)
        {
            IsLiveIdAvairable = this.ObserveProperty(x => x.LiveId)
                .Select(x => x != null ? NiconicoRegex.IsLiveId(x) : false)
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);

            IsLoadFailed = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);
            LoadFailedMessage = new ReactiveProperty<string>()
                .AddTo(_CompositeDisposable);

            LiveStatus = new ReactiveProperty<LiveStatusType>(LiveStatusType.Close);

            IsLivePreserved = LiveStatus.Select(x => x == LiveStatusType.Preserved)
                .ToReadOnlyReactiveProperty();
            IsLiveOpened = LiveStatus.Select(x => x == LiveStatusType.Open)
                .ToReadOnlyReactiveProperty();
            IsLiveStarted = LiveStatus.Select(x => x == LiveStatusType.Start)
                .ToReadOnlyReactiveProperty();
            IsLiveClosed = LiveStatus.Select(x => x == LiveStatusType.Close)
                .ToReadOnlyReactiveProperty();
            IsLiveArchive = LiveStatus.Select(x => x == LiveStatusType.CloseWithArchive)
                .ToReadOnlyReactiveProperty();
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

                var liveInfo = liveInfoResponse.VideoInfo;

                LiveDescriptionText = liveInfo.Video?.Description ?? "???? empty description";

            }
            catch (Exception ex)
            {
                IsLoadFailed.Value = true;
                LoadFailedMessage.Value = ex.Message;
            }

            await base.NavigatedToAsync(cancelToken, e, viewModelState);
        }

    }
}

using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Player.Video;
using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Microsoft.Toolkit.Uwp.UI;
using Mntone.Nico2.Channels.Video;
using NiconicoToolkit.Video;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Player.PlayerSidePaneContent
{
    public sealed class RelatedVideosSidePaneContentViewModel : SidePaneContentViewModelBase
    {
        public RelatedVideosSidePaneContentViewModel(
           NicoVideoProvider nicoVideoProvider,
           ChannelProvider channelProvider,
           MylistRepository mylistRepository,
           HohoemaPlaylist hohoemaPlaylist,
           PageManager pageManager,
           NicoChannelCacheRepository nicoChannelCacheRepository,
           IScheduler scheduler
           )
        {
            NicoVideoProvider = nicoVideoProvider;
            ChannelProvider = channelProvider;
            _mylistRepository = mylistRepository;
            HohoemaPlaylist = hohoemaPlaylist;
            PageManager = pageManager;
            _nicoChannelCacheRepository = nicoChannelCacheRepository;
            _scheduler = scheduler;

            HasVideoDescription = _VideoViewerHelpInfo != null;

            HohoemaPlaylist.ObserveProperty(x => x.CurrentItem)
                .Subscribe(async item =>
                {
                    Clear();
                    _IsInitialized = false;

                    await Task.Delay(1000);

                    if (item != null)
                    {
                        await InitializeRelatedVideos(item);
                    }
                })
                .AddTo(_CompositeDisposable);
        }


        string CurrentVideoId;
        public List<VideoListItemControlViewModel> Videos { get; private set; }

        public VideoListItemControlViewModel CurrentVideo { get; private set; }

        VideoRelatedInfomation _VideoViewerHelpInfo;

        public NicoVideoSessionProvider Video { get; }

        public string JumpVideoId { get; set; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public ChannelProvider ChannelProvider { get; }
        public MylistProvider MylistProvider { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public PageManager PageManager { get; }
        public bool HasVideoDescription { get; private set; }
        public ObservableCollection<VideoListItemControlViewModel> OtherVideos { get; } = new ObservableCollection<VideoListItemControlViewModel>();
        public VideoListItemControlViewModel NextVideo { get; private set; }

        public VideoListItemControlViewModel JumpVideo { get; private set; }

        public ObservableCollection<MylistPlaylist> Mylists { get; } = new ObservableCollection<MylistPlaylist>();

        public Models.Helpers.AsyncLock _InitializeLock = new Models.Helpers.AsyncLock();

        private bool _IsInitialized = false;
        const double _SeriesVideosTitleSimilarityValue = 0.7;
        private readonly MylistRepository _mylistRepository;
        private readonly NicoChannelCacheRepository _nicoChannelCacheRepository;
        private readonly IScheduler _scheduler;


        public void Clear()
        {
            CurrentVideoId = null;
            CurrentVideo = null;
            NextVideo = null;
            JumpVideo = null;
            JumpVideoId = null;
            Videos?.Clear();
            OtherVideos?.Clear();
            Mylists?.Clear();
        }

        public async Task InitializeRelatedVideos(IVideoContent currentVideo)
        {
            string videoId = currentVideo.Id;

            using (var releaser = await _InitializeLock.LockAsync())
            {
                if (_IsInitialized) { return; }

                var sourceVideo = NicoVideoProvider.GetCachedVideoInfo(videoId);
                _VideoViewerHelpInfo = NicoVideoSessionProvider.GetVideoRelatedInfomationWithVideoDescription(videoId, sourceVideo.Title);

                // ニコスクリプトで指定されたジャンプ先動画
                if (JumpVideoId != null)
                {
                    var (res, jumpVideo) = await NicoVideoProvider.GetVideoInfoAsync(JumpVideoId);
                    if (jumpVideo != null)
                    {
                        JumpVideo = new VideoListItemControlViewModel(res.Video, res.Thread);
                        RaisePropertyChanged(nameof(JumpVideo));
                    }
                }

                // 再生中アイテムのタイトルと投稿者説明文に含まれる動画IDの動画タイトルを比較して
                // タイトル文字列が近似する動画をシリーズ動画として取り込む
                // 違うっぽい動画も投稿者が提示したい動画として確保
                List<NicoVideo> seriesVideos = new List<NicoVideo>();
                seriesVideos.Add(sourceVideo);
                if (_VideoViewerHelpInfo != null)
                {
                    var videoIds = _VideoViewerHelpInfo.GetVideoIds();
                    foreach (var id in videoIds)
                    {
                        var video = await NicoVideoProvider.GetCachedVideoInfoAsync(id);

                        var titleSimilarity = sourceVideo.Title.CalculateSimilarity(video.Title);
                        if (titleSimilarity > _SeriesVideosTitleSimilarityValue)
                        {
                            seriesVideos.Add(video);
                        }
                        else
                        {
                            var otherVideo = new VideoListItemControlViewModel(video);
                            OtherVideos.Add(otherVideo);
                        }
                    }
                }


                // シリーズ動画として集めたアイテムを投稿日が新しいものが最後尾になるよう並び替え
                // シリーズ動画に番兵として仕込んだ現在再生中のアイテムの位置と動画数を比べて
                // 動画数が上回っていた場合は次動画が最後尾にあると決め打ちで取得する
                var orderedSeriesVideos = seriesVideos.OrderBy(x => x.PostedAt).ToList();
                var currentVideoIndex = orderedSeriesVideos.IndexOf(sourceVideo);
                if (orderedSeriesVideos.Count - 1 > currentVideoIndex)
                {
                    var nextVideo = orderedSeriesVideos.Last();
                    if (nextVideo.RawVideoId != videoId)
                    {
                        NextVideo = new VideoListItemControlViewModel(nextVideo);

                        orderedSeriesVideos.Remove(nextVideo);

                        RaisePropertyChanged(nameof(NextVideo));
                    }
                }

                // 次動画を除いてシリーズ動画っぽいアイテムを投稿者が提示したい動画として優先表示されるようにする
                orderedSeriesVideos.Remove(sourceVideo);
                orderedSeriesVideos.Reverse();
                foreach (var video in orderedSeriesVideos)
                {
                    var videoVM = new VideoListItemControlViewModel(video);
                    OtherVideos.Insert(0, videoVM);
                }

                RaisePropertyChanged(nameof(OtherVideos));


                // チャンネル動画で次動画が見つからなかった場合は
                // チャンネル動画一覧から次動画をサジェストする
                if (sourceVideo.Owner?.UserType == OwnerType.Channel
                    && NextVideo == null
                    )
                {
                    // DBからチャンネル情報を取得
                    var db_channelInfo = _nicoChannelCacheRepository.GetFromRawId(sourceVideo.Owner.OwnerId);
                    if (db_channelInfo == null)
                    {
                        db_channelInfo = new NicoChannelInfo()
                        {
                            RawId = sourceVideo.Owner.OwnerId,
                            ThumbnailUrl = sourceVideo.Owner.IconUrl,
                            Name = sourceVideo.Owner.ScreenName
                        };
                    }

                    // チャンネル動画の一覧を取得する
                    // ページアクセスが必要なので先頭ページを取って
                    // 全体の分量を把握してから全ページ取得を行う
                    //List<ChannelVideoInfo> channelVideos = new List<ChannelVideoInfo>();
                    //var channelVideosFirstPage = await ChannelProvider.GetChannelVideo(sourceVideo.Owner.OwnerId, 0);
                    //var uncheckedCount = channelVideosFirstPage.TotalCount - channelVideosFirstPage.Videos.Count;
                    //if (channelVideosFirstPage.TotalCount != 0)
                    //{
                    //    channelVideos.AddRange(channelVideosFirstPage.Videos);

                    //    var uncheckedPageCount = (int)Math.Ceiling((double)uncheckedCount / 20); /* チャンネル動画１ページ = 20 動画 */
                    //    foreach (var page in Enumerable.Range(1, uncheckedPageCount))
                    //    {
                    //        var channelVideoInfo = await ChannelProvider.GetChannelVideo(sourceVideo.Owner.OwnerId, page);
                    //        channelVideos.AddRange(channelVideoInfo.Videos);
                    //    }

                    //    db_channelInfo.Videos = channelVideos;
                    //}

                    //_nicoChannelCacheRepository.AddOrUpdate(db_channelInfo);


                    var collectionView = new AdvancedCollectionView(db_channelInfo.Videos);
                    collectionView.SortDescriptions.Add(new SortDescription(nameof(ChannelVideoInfo.PostedAt), SortDirection.Ascending));
                    collectionView.SortDescriptions.Add(new SortDescription(nameof(ChannelVideoInfo.ItemId), SortDirection.Ascending));
                    collectionView.RefreshSorting();

                    var item = collectionView.FirstOrDefault(x => (x as ChannelVideoInfo).Title == sourceVideo.Title);
                    var pos = collectionView.IndexOf(item);
                    if (pos >= 0)
                    {
                        var nextVideo = collectionView.ElementAtOrDefault(pos + 1) as ChannelVideoInfo;
                        if (nextVideo != null)
                        {
                            var videoVM = new VideoListItemControlViewModel(nextVideo.ItemId, nextVideo.Title, nextVideo.ThumbnailUrl, nextVideo.Length, nextVideo.PostedAt);
                            if (nextVideo.IsRequirePayment)
                            {
                                videoVM.Permission = NiconicoToolkit.Video.VideoPermission.RequirePay;
                            }
                            else if (nextVideo.IsFreeForMember)
                            {
                                videoVM.Permission = NiconicoToolkit.Video.VideoPermission.FreeForChannelMember;
                            }
                            else if (nextVideo.IsMemberUnlimitedAccess)
                            {
                                videoVM.Permission = NiconicoToolkit.Video.VideoPermission.MemberUnlimitedAccess;
                            }

                            videoVM.ViewCount = nextVideo.ViewCount;
                            videoVM.CommentCount = nextVideo.CommentCount;
                            videoVM.MylistCount = nextVideo.MylistCount;

                            NextVideo = videoVM;
                            RaisePropertyChanged(nameof(NextVideo));
                        }
                    }
                }

                // マイリスト
                if (_VideoViewerHelpInfo != null)
                {
                    var relatedMylistIds = _VideoViewerHelpInfo.GetMylistIds();
                    foreach (var mylistId in relatedMylistIds)
                    {
                        var mylistDetails = await _mylistRepository.GetMylist(mylistId);
                        if (mylistDetails != null)
                        {
                            Mylists.Add(mylistDetails);
                        }
                    }

                    RaisePropertyChanged(nameof(Mylists));
                }


                Videos = new List<VideoListItemControlViewModel>();
                /*
                var items = await NicoVideoProvider.GetRelatedVideos(videoId, 0, 10);
                if (items.Video_info?.Any() ?? false)
                {
                    Videos.AddRange((IEnumerable<VideoListItemControlViewModel>)(items.Video_info?.Select((Func<Mntone.Nico2.Mylist.Video_info, VideoListItemControlViewModel>)(x =>
                    {
                        var video = NicoVideoProvider.GetCachedVideoInfo(x.Video.Id);
                        video.Title = x.Video.Title;
                        video.ThumbnailUrl = x.Video.Thumbnail_url;

                        var vm = new VideoListItemControlViewModel((NicoVideo)video);
                        return vm;
                    }))));
                }
                */

                CurrentVideo = Videos.FirstOrDefault(x => x.RawVideoId == videoId);
                RaisePropertyChanged(nameof(Videos));
                RaisePropertyChanged(nameof(CurrentVideo));

                _IsInitialized = true;
            }
        }
    }
}

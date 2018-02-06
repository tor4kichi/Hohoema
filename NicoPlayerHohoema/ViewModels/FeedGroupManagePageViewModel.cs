using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NicoPlayerHohoema.Models;
using Prism.Mvvm;
using Prism.Commands;
using NicoPlayerHohoema.Helpers;
using Reactive.Bindings;
using System.Collections.ObjectModel;
using Prism.Windows.Navigation;
using System.Reactive.Linq;
using Reactive.Bindings.Extensions;
using NicoPlayerHohoema.Views.Service;
using System.Threading;

namespace NicoPlayerHohoema.ViewModels
{
	public class FeedGroupManagePageViewModel : HohoemaViewModelBase
	{
        private Services.HohoemaDialogService _HohoemaDialogService;

		public ObservableCollection<FeedNewVideosList> FeedGroupItems { get; private set; }
		

		public FeedGroupManagePageViewModel(HohoemaApp hohoemaApp, PageManager pageManager, Services.HohoemaDialogService dialogService) 
			: base(hohoemaApp, pageManager)
		{
            _HohoemaDialogService = dialogService;

			FeedGroupItems = new ObservableCollection<FeedNewVideosList>();

			

            var feedManager = hohoemaApp.FeedManager;
            feedManager.FeedGroupAdded += FeedManager_FeedGroupAdded;
            feedManager.FeedGroupRemoved += FeedManager_FeedGroupRemoved;
        }

        private void FeedManager_FeedGroupRemoved(Database.Feed feedGroup)
        {
            var removedFeedGroup = FeedGroupItems.FirstOrDefault(x => x.Feed.Id == feedGroup.Id);
            if (removedFeedGroup != null)
            {
                FeedGroupItems.Remove(removedFeedGroup);
            }
        }

        private void FeedManager_FeedGroupAdded(Database.Feed feedGroup)
        {
            FeedGroupItems.Add(new FeedNewVideosList(feedGroup, HohoemaApp.FeedManager, HohoemaApp.Playlist));
        }

        
        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (HohoemaApp.FeedManager == null)
			{
				return;
			}

			base.OnNavigatedTo(e, viewModelState);
		}

        protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			var items = HohoemaApp.FeedManager.GetAllFeedGroup()
				.Select(x => new FeedNewVideosList(x, HohoemaApp.FeedManager, HohoemaApp.Playlist))
				.ToList();

			foreach (var feedItemListItem in items)
			{
				FeedGroupItems.Add(feedItemListItem);

                HohoemaApp.FeedManager.UpdateFeedGroup(feedItemListItem.Feed);

                feedItemListItem.UpdateFeedVideos();
            }

            RaisePropertyChanged(nameof(FeedGroupItems));

            await base.NavigatedToAsync(cancelToken, e, viewModelState);
		}

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            var items = FeedGroupItems.ToArray();
            FeedGroupItems.Clear();

            foreach (var feedGroupVM in items)
            {
                feedGroupVM.Dispose();
            }

            base.OnNavigatingFrom(e, viewModelState, suspending);
        }

        private DelegateCommand _AddVideoFeedCommand;
        public DelegateCommand AddVideoFeedCommand
        {
            get
            {
                return _AddVideoFeedCommand
                    ?? (_AddVideoFeedCommand = new DelegateCommand(async () =>
                    {
                        var selectableContents = HohoemaApp.FollowManager.GetAllFollowInfoGroups()
                            .Select(x => new Dialogs.ChoiceFromListSelectableContainer(x.FollowItemType.ToCulturelizeString(),
                                x.FollowInfoItems.Select(y => new Dialogs.SelectDialogPayload()
                                {
                                    Label = y.Name,
                                    Id = y.Id,
                                    Context = y
                                })));

                        var keywordInput = new Dialogs.TextInputSelectableContainer("キーワード入力", null);

                        var result = await _HohoemaDialogService.ShowContentSelectDialogAsync("新着対象を選択", Enumerable.Concat<Dialogs.ISelectableContainer>(new [] { keywordInput }, selectableContents));

                        if (result != null)
                        {
                            Database.Bookmark bookmark = null;
                            if (result.Context is FollowItemInfo)
                            {
                                bookmark = new Database.Bookmark()
                                {
                                    Content = (result.Context as FollowItemInfo).Id,
                                    Label = result.Label,
                                    BookmarkType = FeedManager.FollowItemTypeConvertToFeedSourceType((result.Context as FollowItemInfo).FollowItemType),
                                };

                                if (bookmark.BookmarkType == Database.BookmarkType.User)
                                {
                                    try
                                    {
                                        var userDetails = await HohoemaApp.ContentProvider.GetUserDetail(bookmark.Content);
                                        if (userDetails.IsOwnerVideoPrivate)
                                        {
                                            return;
                                        }
                                    }
                                    catch { return; }
                                }
                            }
                            else
                            {
                                bookmark = new Database.Bookmark()
                                {
                                    Content = result.Id,
                                    Label = result.Label,
                                    BookmarkType = Database.BookmarkType.SearchWithKeyword,
                                };
                            }

                            var feed = HohoemaApp.FeedManager.AddFeedGroup(result.Label, bookmark);

                            System.Diagnostics.Debug.WriteLine($"{feed.Label} に「{result.Id}」を追加");

//                            HohoemaApp.FeedManager.UpdateFeedGroup(feed);

                            await HohoemaApp.FeedManager.RefreshFeedItemsAsync(feed).ConfigureAwait(false);
                        }
                    }));
            }
        }




        private DelegateCommand<FeedNewVideosList> _OpenFeedVideoPageCommand;
		public DelegateCommand<FeedNewVideosList> OpenFeedVideoPageCommand
		{
			get
			{
				return _OpenFeedVideoPageCommand
					?? (_OpenFeedVideoPageCommand = new DelegateCommand<FeedNewVideosList>((listItem) =>
					{
                        var source = listItem.Feed.Sources.FirstOrDefault();
                        if (source == null)
                        {
                            return;
                        }

                        if (string.IsNullOrEmpty(source.Content))
                        {
                            return;
                        }

                        HohoemaPageType? pageType = null;
                        string pageParameter = null;
                        switch (source.BookmarkType)
                        {
                            case Database.BookmarkType.User:
                                pageType = HohoemaPageType.UserVideo;
                                pageParameter = source.Content;
                                break;
                            case Database.BookmarkType.Mylist:
                                pageType = HohoemaPageType.Mylist;
                                pageParameter = new MylistPagePayload(source.Content).ToParameterString();
                                break;
                            case Database.BookmarkType.SearchWithTag:
                                pageType = HohoemaPageType.SearchResultTag;
                                pageParameter = new TagSearchPagePayloadContent() { Keyword = source.Content }.ToParameterString();
                                break;
                            case Database.BookmarkType.SearchWithKeyword:
                                pageType = HohoemaPageType.SearchResultKeyword;
                                pageParameter = new KeywordSearchPagePayloadContent() { Keyword = source.Content }.ToParameterString();
                                break;
                            case Database.BookmarkType.SearchWithLive:
                                pageType = null;
                                break;
                            default:
                                break;
                        }
                        if (pageType != null && pageParameter != null)
                        {
                            PageManager.OpenPage(pageType.Value, pageParameter);
                        }
                    }));
			}
		}


		
		private DelegateCommand _CreateFeedGroupCommand;
		public DelegateCommand CreateFeedGroupCommand
		{
			get
			{
				return _CreateFeedGroupCommand
					?? (_CreateFeedGroupCommand = new DelegateCommand(async () =>
					{
						var newFeedGroupName = await _HohoemaDialogService.GetTextAsync(
							"フィードグループを作成"
							, "フィードグループ名"
							, validater: (name) =>
							{
								if (String.IsNullOrWhiteSpace(name)) { return false; }

								return true;
							});

						if (newFeedGroupName != null)
						{
							var feedGroup = HohoemaApp.FeedManager.AddFeedGroup(newFeedGroupName);

							PageManager.OpenPage(HohoemaPageType.FeedGroup, feedGroup.Id);
						}
					}));
			}
		}


		private DelegateCommand<FeedNewVideosList> _RemoveFeedGroupCommand;
		public DelegateCommand<FeedNewVideosList> RemoveFeedGroupCommand
		{
			get
			{
				return _RemoveFeedGroupCommand
					?? (_RemoveFeedGroupCommand = new DelegateCommand<FeedNewVideosList>((feedGroupVM) =>
					{
                        // TODO: 新着チェックの削除、確認ダイアログ表示


                        var result = HohoemaApp.FeedManager.RemoveFeedGroup(feedGroupVM.Feed);
                        if (result)
                        {
                            var removeItem = FeedGroupItems.FirstOrDefault(x => x.Feed.Id == feedGroupVM.Feed.Id);
                            if (removeItem != null)
                            {
                                FeedGroupItems.Remove(removeItem);
                            }
                        }
					}));
			}
		}


        
    }

    public class FeedNewVideosList : HohoemaListingPageItemBase, Interfaces.IFeedGroup, IDisposable
    {
        FeedManager FeedManager { get; }
        HohoemaPlaylist Playlist { get; }

        public Database.Feed Feed { get; }
        public Database.BookmarkType Bookmark { get; }

        public string Id => Feed.Id.ToString();
        public ReactiveProperty<bool> NowUpdate { get; private set; }


        public ObservableCollection<FeedVideoInfoControlViewModel> FeedVideos { get; }

        /// <summary>
        /// フィードの未チェック動画を全て再生（古い動画から再生）
        /// 先頭を即座に再生開始して、残りの動画は「あとで見る」プレイリストの先頭側に挿入
        /// </summary>
        public ReactiveCommand PlayAllCommand { get; }

        /// <summary>
        /// フィードの未チェック動画をすべて「あとで見る」プレイリストに追加
        /// 古い動画から先に再生されるよう追加する
        /// </summary>
        public ReactiveCommand AllAddToAfterWatchCommand { get; }


        public ReactiveCommand FeedCheckedCommand { get; }
        public ReactiveCommand UpdateCommand { get; }

        public FeedNewVideosList(Database.Feed feeds, FeedManager feedManager, HohoemaPlaylist playlist)
        {
            Feed = feeds;
            FeedManager = feedManager;
            Playlist = playlist;

            Label = feeds.Label;
            Bookmark = feeds.Sources.FirstOrDefault()?.BookmarkType ?? throw new Exception();

            FeedVideos = new ObservableCollection<FeedVideoInfoControlViewModel>();
            NowUpdate = new ReactiveProperty<bool>(false);

            PlayAllCommand = FeedVideos.CollectionChangedAsObservable()
                .Select(_ => FeedVideos.Count > 0)
                .ToReactiveCommand();

            PlayAllCommand.Subscribe(() => 
            {
                var firstItem = FeedVideos.LastOrDefault();
                if (firstItem != null)
                {
                    Playlist.PlayVideo(firstItem.RawVideoId, firstItem.Label);
                }

                foreach (var playItem in FeedVideos.Reverse().Skip(1))
                {
                    Playlist.DefaultPlaylist.AddVideo(playItem.RawVideoId, playItem.Label, ContentInsertPosition.Head);
                }

                FeedCheckedCommand.Execute();
            });

            AllAddToAfterWatchCommand = FeedVideos.CollectionChangedAsObservable()
                .Select(_ => FeedVideos.Count > 0)
                .ToReactiveCommand();

            AllAddToAfterWatchCommand.Subscribe(() => 
            {
                foreach (var playItem in FeedVideos.Reverse())
                {
                    Playlist.DefaultPlaylist.AddVideo(playItem.RawVideoId, playItem.Label, ContentInsertPosition.Tail);
                }

                FeedCheckedCommand.Execute();
            });

            PlayAllCommand.Subscribe(() =>
            {
                IEnumerable<FeedVideoInfoControlViewModel> playItems = FeedVideos.AsEnumerable();

                var firstItem = FeedVideos.FirstOrDefault();
                if (firstItem != null)
                {
                    Playlist.PlayVideo(firstItem.RawVideoId, firstItem.Label);
                }

                playItems = FeedVideos.Skip(1);


                foreach (var playItem in playItems)
                {
                    Playlist.DefaultPlaylist.AddVideo(playItem.RawVideoId, playItem.Label);
                }

                FeedCheckedCommand.Execute();
            });


            FeedCheckedCommand = FeedVideos.CollectionChangedAsObservable()
                .Select(_ => FeedVideos.Count > 0)
                .ToReactiveCommand();

            FeedCheckedCommand.Subscribe(() => 
            {
                Feed.CheckedAt = DateTime.Now;
                FeedManager.UpdateFeedGroup(Feed);

                UpdateFeedVideos();
            });
            //            UpdateFeedVideos();

            UpdateCommand = NowUpdate.Select(x => !x).ToReactiveCommand();
            UpdateCommand.Subscribe(() => 
            {
                UpdateFeedVideos();
            });

            feedManager.FeedUpdated += FeedManager_FeedUpdated;
        }

        protected override void OnDispose()
        {
            FeedManager.FeedUpdated -= FeedManager_FeedUpdated;

            base.OnDispose();
        }

        private void FeedManager_FeedUpdated(object sender, FeedUpdateEventArgs e)
        {
            try
            {
                NowUpdate.Value = true;

                if (e.Feed.Id != Feed.Id) { return; }

                foreach (var feed in e.Items)
                {
                    var video = feed.Item1;

                    // 視聴済みの動画は表示しない
                    var playedHistory = Models.Db.VideoPlayHistoryDb.Get(video.VideoId);
                    if (playedHistory?.PlayCount != 0)
                    {
                        continue;
                    }

                    // 前回チェックした日時よりも古い動画は表示しない
                    if (video.PostedAt < Feed.CheckedAt)
                    {
                        continue;
                    }

                    FeedVideos.Add(new FeedVideoInfoControlViewModel(video, feed.Item2));
                }

                RaisePropertyChanged(nameof(FeedVideos));
            }
            finally
            {
                NowUpdate.Value = false;
            }
        }

        internal async void UpdateFeedVideos()
        {
            FeedVideos.Clear();

            await FeedManager.RefreshFeedItemsAsync(Feed);
        }

    }



    public class FeedGroupListItem : HohoemaListingPageItemBase, Interfaces.IFeedGroup
    {
        PageManager _PageManager;
        public Database.Feed FeedGroup { get; private set; }

        public List<Database.Bookmark> SourceItems { get; private set; }

        public ReactiveProperty<bool> NowUpdate { get; private set; }

        public FeedGroupListItem(Database.Feed feedGroup, PageManager pageManager)
        {
            FeedGroup = feedGroup;
            _PageManager = pageManager;

            Label = feedGroup.Label;
            SourceItems = FeedGroup.Sources
                .ToList();
            OptionText = FeedGroup.UpdateAt.ToString();
            NowUpdate = new ReactiveProperty<bool>(false);
        }

        private DelegateCommand _SelectedCommand;
        public ICommand PrimaryCommand
        {
            get
            {
                return _SelectedCommand
                    ?? (_SelectedCommand = new DelegateCommand(() =>
                    {
                        _PageManager.OpenPage(HohoemaPageType.FeedVideoList, FeedGroup.Id);
                    }));
            }
        }


        private DelegateCommand _SecondaryActionCommand;
        public DelegateCommand SecondaryActionCommand
        {
            get
            {
                return _SecondaryActionCommand
                    ?? (_SecondaryActionCommand = new DelegateCommand(() =>
                    {
                        _PageManager.OpenPage(HohoemaPageType.FeedGroup, FeedGroup.Id);
                    }));
            }
        }

        private DelegateCommand _OpenEditCommand;
        public DelegateCommand OpenEditCommand
        {
            get
            {
                return _OpenEditCommand
                    ?? (_OpenEditCommand = new DelegateCommand(() =>
                    {
                        _PageManager.OpenPage(HohoemaPageType.FeedGroup, FeedGroup.Id);
                    }));
            }
        }

        public string Id => FeedGroup.Id.ToString();

        public void UpdateStarted()
        {
            NowUpdate.Value = true;
        }

        public void UpdateCompleted()
        {
            NowUpdate.Value = false;
            OptionText = FeedGroup.UpdateAt.ToString();
        }
    }
}

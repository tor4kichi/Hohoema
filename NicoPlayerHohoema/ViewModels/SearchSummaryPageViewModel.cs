using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NicoPlayerHohoema.Helpers;
using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace NicoPlayerHohoema.ViewModels
{
    public class SearchSummaryPageViewModel : HohoemaViewModelBase
    {
        private string _Keyword;
        public string Keyword
        {
            get { return _Keyword; }
            set { SetProperty(ref _Keyword, value); }
        }

        private bool _NowUpdating;
        public bool NowUpdating
        {
            get { return _NowUpdating; }
            set { SetProperty(ref _NowUpdating, value); }
        }


        public ReadOnlyReactiveProperty<bool> HasKeywordSearchResultItems { get; }
        public ReadOnlyReactiveCollection<VideoInfoControlViewModel> KeywordSearchResultItems { get; }
        public ReadOnlyReactiveProperty<bool> HasLiveSearchResultItems { get; }
        public ReadOnlyReactiveCollection<LiveInfoViewModel> LiveSearchResultItems { get; }

        private int _KeywordSearchItemsTotalCount;
        public int KeywordSearchItemsTotalCount
        {
            get { return _KeywordSearchItemsTotalCount; }
            set { SetProperty(ref _KeywordSearchItemsTotalCount, value); }
        }

        private int _LiveSearchItemsTotalCount;
        public int LiveSearchItemsTotalCount
        {
            get { return _LiveSearchItemsTotalCount; }
            set { SetProperty(ref _LiveSearchItemsTotalCount, value); }
        }

        public ObservableCollection<string> RelatedVideoTags { get; }
        public ObservableCollection<string> RelatedLiveTags { get; }


        public SearchSummaryPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager) 
            : base(hohoemaApp, pageManager, useDefaultPageTitle:false)
        {
            RelatedVideoTags = new ObservableCollection<string>();

            KeywordSearchResultItems = this.ObserveProperty(x => x.Keyword)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .SelectMany(async (x, i, cancelToken) =>
                {
                    RelatedVideoTags.Clear();
                    var res = await HohoemaApp.ContentProvider.GetKeywordSearch(x, 0, 10);
                    
                    if (res.IsOK)
                    {
                        KeywordSearchItemsTotalCount = (int)res.GetTotalCount();

                        if (res.Tags != null)
                        {
                            foreach (var tag in res.Tags.TagItems)
                            {
                                RelatedVideoTags.Add(tag.Name);
                            }
                        }

                        return res.VideoInfoItems?.AsEnumerable() ?? Enumerable.Empty<Mntone.Nico2.Searches.Video.VideoInfo>();
                    }
                    else
                    {
                        return Enumerable.Empty<Mntone.Nico2.Searches.Video.VideoInfo>();
                    }
                })
                .SelectMany(x => x)
                .Select(x =>
                {
                    var vm = new VideoInfoControlViewModel(x.Video.Id);
                    vm.SetTitle(x.Video.Title);
                    vm.SetThumbnailImage(x.Video.ThumbnailUrl.OriginalString);
                    return vm;
                })
                .ToReadOnlyReactiveCollection(onReset: this.ObserveProperty(x => x.Keyword).ToUnit())
                .AddTo(_CompositeDisposable);
            HasKeywordSearchResultItems = KeywordSearchResultItems
                .ObserveProperty(x => x.Count)
                .Select(x => x > 0)
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);

            RelatedLiveTags = new ObservableCollection<string>();
            LiveSearchResultItems = this.ObserveProperty(x => x.Keyword)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .SelectMany(async (x, i, cancelToken) =>
                {
                    RelatedLiveTags.Clear();
                    var res = await HohoemaApp.ContentProvider.LiveSearchAsync(x, false, length: 10);
                    if (res.IsStatusOK)
                    {
                        if (res.Tags != null)
                        {
                            foreach (var tag in res.Tags.Tag)
                            {
                                RelatedLiveTags.Add(tag.Name);
                            }
                        }

                        LiveSearchItemsTotalCount = res.TotalCount.FilteredCount;
                        return res.VideoInfo?.AsEnumerable() ?? Enumerable.Empty<Mntone.Nico2.Searches.Live.VideoInfo>();
                    }
                    else
                    {
                        return Enumerable.Empty<Mntone.Nico2.Searches.Live.VideoInfo>();
                    }
                })
                .SelectMany(x => x)
                .Select(x =>
                {
                    var liveVM = new LiveInfoViewModel(x, HohoemaApp.Playlist, PageManager);
                    return liveVM;
                })
                .ToReadOnlyReactiveCollection(onReset: this.ObserveProperty(x => x.Keyword).ToUnit());
            HasLiveSearchResultItems = LiveSearchResultItems
                .ObserveProperty(x => x.Count)
                .Select(x => x > 0)
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);
        }

        
        protected override Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            if (e.Parameter is string)
            {
                Keyword = e.Parameter as string;
                UpdateTitle($"\"{Keyword}\"を検索");

                SearchWithTargetCommand.RaiseCanExecuteChanged();

                Models.Db.SearchHistoryDb.Searched(Keyword, SearchTarget.Keyword);
            }

            return base.NavigatedToAsync(cancelToken, e, viewModelState);
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            Keyword = null;

            base.OnNavigatingFrom(e, viewModelState, suspending);
        }


        private DelegateCommand<SearchTarget?> _SearchWithTargetCommand;
        public DelegateCommand<SearchTarget?> SearchWithTargetCommand
        {
            get
            {
                return _SearchWithTargetCommand
                    ?? (_SearchWithTargetCommand = new DelegateCommand<SearchTarget?>((target) =>
                    {
                        if (target.HasValue)
                        {
                            ISearchPagePayloadContent searchContent =
                                SearchPagePayloadContentHelper.CreateDefault(target.Value, Keyword);
                            PageManager.Search(searchContent);
                        }
                    }
                    ));
            }
        }


        private DelegateCommand<string> _SearchVideoTagCommand;
        public DelegateCommand<string> SearchVideoTagCommand
        {
            get
            {
                return _SearchVideoTagCommand
                    ?? (_SearchVideoTagCommand = new DelegateCommand<string>((tag) =>
                    {
                        ISearchPagePayloadContent searchContent =
                            SearchPagePayloadContentHelper.CreateDefault(SearchTarget.Tag, tag);
                        PageManager.Search(searchContent);
                    },
                    (tag) => !string.IsNullOrWhiteSpace(tag)
                    ));
            }
        }
        private DelegateCommand<string> _SearchLiveTagCommand;
        public DelegateCommand<string> SearchLiveTagCommand
        {
            get
            {
                return _SearchLiveTagCommand
                    ?? (_SearchLiveTagCommand = new DelegateCommand<string>((tag) =>
                    {
                        ISearchPagePayloadContent searchContent =
                            SearchPagePayloadContentHelper.CreateDefault(SearchTarget.Niconama, tag);
                        (searchContent as LiveSearchPagePayloadContent).IsTagSearch = true;
                        PageManager.Search(searchContent);
                    },
                    (tag) => !string.IsNullOrWhiteSpace(tag)
                    ));
            }
        }
    }
}

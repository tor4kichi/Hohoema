﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hohoema.Models.Helpers;
using Hohoema.Models;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Hohoema.ViewModels.Pages;
using Hohoema.Models.Provider;
using Unity;
using Hohoema.UseCase;

namespace Hohoema.ViewModels
{
    public class SearchSummaryPageViewModel : HohoemaViewModelBase, INavigatedAwareAsync
    {

        public SearchSummaryPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            SearchProvider searchProvider,
            PageManager pageManager
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            SearchProvider = searchProvider;
            PageManager = pageManager;

            RelatedVideoTags = new ObservableCollection<string>();

            KeywordSearchResultItems = this.ObserveProperty(x => x.Keyword)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .SelectMany(async (x, i, cancelToken) =>
                {
                    RelatedVideoTags.Clear();
                    var res = await SearchProvider.GetKeywordSearch(x, 0, 10);

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
                    var res = await SearchProvider.LiveSearchAsync(x, 0, 10);
                    if (res.IsOK)
                    {
                        LiveSearchItemsTotalCount = res.Meta.TotalCount ?? 0;
                        return res.Data?.AsEnumerable() ?? Enumerable.Empty<Mntone.Nico2.Searches.Live.LiveSearchResultItem>();
                    }
                    else
                    {
                        return Enumerable.Empty<Mntone.Nico2.Searches.Live.LiveSearchResultItem>();
                    }
                })
                .SelectMany(x => x)
                .Select(x =>
                {
                    var liveInfoVM = new LiveInfoListItemViewModel(x.ContentId);
                    liveInfoVM.Setup(x);
                    return liveInfoVM;
                })
                .ToReadOnlyReactiveCollection(onReset: this.ObserveProperty(x => x.Keyword).ToUnit());
            HasLiveSearchResultItems = LiveSearchResultItems
                .ObserveProperty(x => x.Count)
                .Select(x => x > 0)
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);
        }


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
        public ReadOnlyReactiveCollection<LiveInfoListItemViewModel> LiveSearchResultItems { get; }

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



        public Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            if (parameters.TryGetValue("keyword", out string keyword))
            {
                Keyword = keyword;
            }

            SearchWithTargetCommand.RaiseCanExecuteChanged();

            return Task.CompletedTask;
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
                            PageManager.Search(target.Value, Keyword);
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
                        PageManager.Search(SearchTarget.Tag, tag);
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
                        PageManager.Search(SearchTarget.Niconama, tag);
                    },
                    (tag) => !string.IsNullOrWhiteSpace(tag)
                    ));
            }
        }

        public AppearanceSettings AppearanceSettings { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public SearchProvider SearchProvider { get; }
        public PageManager PageManager { get; }
    }
}

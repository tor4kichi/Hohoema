﻿using System.Collections.Async;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Mntone.Nico2.Videos.Recommend;
using NicoPlayerHohoema.Models.Helpers;
using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Windows.Navigation;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.Models.Provider;
using Microsoft.Practices.Unity;
using Reactive.Bindings.Extensions;
using Reactive.Bindings;

namespace NicoPlayerHohoema.ViewModels
{
    public class RecommendPageViewModel : HohoemaListingPageViewModelBase<RecommendVideoListItem>
    {
        public RecommendPageViewModel(
            NGSettings ngSettings,
            LoginUserRecommendProvider loginUserRecommendProvider,
            Services.HohoemaPlaylist hohoemaPlaylist,
            Services.PageManager pageManager
            )
            : base(pageManager)
        {
            NgSettings = ngSettings;
            LoginUserRecommendProvider = loginUserRecommendProvider;
            HohoemaPlaylist = hohoemaPlaylist;
        }

        public NGSettings NgSettings { get; }
        public LoginUserRecommendProvider LoginUserRecommendProvider { get; }
        public Services.HohoemaPlaylist HohoemaPlaylist { get; }
        public ReadOnlyObservableCollection<TagViewModel> RecommendSourceTags { get; private set; }

        protected override void PostResetList()
        {
            var source = this.IncrementalLoadingItems.Source as RecommendVideoIncrementalLoadingSource;
            RecommendSourceTags = source.RecommendSourceTags
                .ToReadOnlyReactiveCollection(x => new TagViewModel(x));
            RaisePropertyChanged(nameof(RecommendSourceTags));

            base.PostResetList();
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            RecommendSourceTags = null;
            RaisePropertyChanged(nameof(RecommendSourceTags));

            base.OnNavigatingFrom(e, viewModelState, suspending);
        }
        protected override IIncrementalSource<RecommendVideoListItem> GenerateIncrementalSource()
        {
            return new RecommendVideoIncrementalLoadingSource(LoginUserRecommendProvider, NgSettings);
        }



        private DelegateCommand<string> _OpenTagCommand;
        public DelegateCommand<string> OpenTagCommand
        {
            get
            {
                return _OpenTagCommand
                    ?? (_OpenTagCommand = new DelegateCommand<string>((tag) =>
                    {
                        PageManager.SearchTag(tag, Mntone.Nico2.Order.Descending, Mntone.Nico2.Sort.FirstRetrieve);
                    }));
            }
        }
    }


    public sealed class RecommendVideoListItem : VideoInfoControlViewModel
    {
        Mntone.Nico2.Videos.Recommend.Item _Item;

        public string RecommendSourceTag { get; private set; }


        public RecommendVideoListItem(
            Mntone.Nico2.Videos.Recommend.Item item
            )
            : base(item.Id, null)
        {
            _Item = item;
            RecommendSourceTag = _Item.AdditionalInfo?.Sherlock.Tag;
        }

    }

    public sealed class RecommendVideoIncrementalLoadingSource : HohoemaIncrementalSourceBase<RecommendVideoListItem>
    {
        public RecommendVideoIncrementalLoadingSource(
            LoginUserRecommendProvider loginUserRecommendProvider,
            NGSettings ngSettings
            )
        {
            LoginUserRecommendProvider = loginUserRecommendProvider;
            NgSettings = ngSettings;
        }

        public LoginUserRecommendProvider LoginUserRecommendProvider { get; }
        public NGSettings NgSettings { get; }

        private RecommendResponse _RecommendResponse;
        private RecommendContent _PrevRecommendContent;

        private HashSet<string> _RecommendSourceTagsHashSet = new HashSet<string>();
        public ObservableCollection<string> RecommendSourceTags { get; } = new ObservableCollection<string>();

        private bool _EndOfRecommend = false;

        

        public override uint OneTimeLoadCount => 18;


        protected override async Task<IAsyncEnumerable<RecommendVideoListItem>> GetPagedItemsImpl(int head, int count)
        {
            if (_EndOfRecommend)
            {
                return AsyncEnumerable.Empty<RecommendVideoListItem>();
            }

            // 初回はページアクセスで得られるデータを使う
            if (_PrevRecommendContent == null)
            {
                _PrevRecommendContent = _RecommendResponse?.FirstData;
            }
            else
            {
                _PrevRecommendContent = await LoginUserRecommendProvider.GetRecommendAsync(_RecommendResponse, _PrevRecommendContent);
            }


            if (_PrevRecommendContent != null && _PrevRecommendContent.Status == "ok")
            {
                _EndOfRecommend = _PrevRecommendContent?.RecommendInfo.EndOfRecommend ?? true;

                AddRecommendTags(
                    _PrevRecommendContent.Items.Select(x => x.AdditionalInfo?.Sherlock.Tag)
                    .Where(x => x != null)
                    );


                return _PrevRecommendContent.Items.Select(x =>
                {
                    var video = Database.NicoVideoDb.Get(x.Id);
                    video.ThumbnailUrl = x.ThumbnailUrl;
                    video.Title = x.ParseTitle();
                    video.Length = x.ParseLengthToTimeSpan();
                    video.ViewCount = x.ViewCounter;
                    video.CommentCount = x.NumRes;
                    video.MylistCount = x.MylistCounter;
                    video.PostedAt = x.ParseForstRetroeveToDateTimeOffset().DateTime;
                    Database.NicoVideoDb.AddOrUpdate(video);

                    var vm = new RecommendVideoListItem(x);
                    vm.SetupFromThumbnail(video);
                    return vm;
                })
                .ToAsyncEnumerable()
                ;
            }
            else
            {
                return AsyncEnumerable.Empty<RecommendVideoListItem>();
            }
            
        }

        protected override async Task<int> ResetSourceImpl()
        {
            _RecommendResponse = await LoginUserRecommendProvider.GetRecommendFirstAsync();
            if (_RecommendResponse.FirstData.Status == "ok")
            {
                _EndOfRecommend = _RecommendResponse.FirstData.RecommendInfo.EndOfRecommend;

                AddRecommendTags(
                    _RecommendResponse.FirstData.Items.Select(x => x.AdditionalInfo?.Sherlock.Tag)
                    .Where(x => x != null)
                    );

            }
            else
            {
                _EndOfRecommend = true;
            }
            return 1000;
        }


        private void AddRecommendTags(IEnumerable<string> tags)
        {
            foreach (var tag in tags)
            {
                if (!_RecommendSourceTagsHashSet.Contains(tag))
                {
                    _RecommendSourceTagsHashSet.Add(tag);
                    RecommendSourceTags.Add(tag);
                }
            }
        }
    }
}

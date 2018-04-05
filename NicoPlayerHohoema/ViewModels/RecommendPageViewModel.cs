using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mntone.Nico2.Videos.Recommend;
using NicoPlayerHohoema.Database;
using NicoPlayerHohoema.Helpers;
using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Windows.Navigation;

namespace NicoPlayerHohoema.ViewModels
{
    public class RecommendPageViewModel : HohoemaVideoListingPageViewModelBase<RecommendVideoListItem>
    {
        NiconicoContentProvider _NiconicoContentProvider;

        public ReadOnlyObservableCollection<string> RecommendSourceTags { get; private set; }

        public RecommendPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager, NiconicoContentProvider contentProvider)
            : base(hohoemaApp, pageManager)
        {
            _NiconicoContentProvider = contentProvider;
        }

        protected override void PostResetList()
        {
            var source = this.IncrementalLoadingItems.Source as RecommendVideoIncrementalLoadingSource;
            RecommendSourceTags = new  ReadOnlyObservableCollection<string>(source.RecommendSourceTags);
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
            return new RecommendVideoIncrementalLoadingSource(_NiconicoContentProvider);
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

        public string RecommendSourceTag { get; }


        public RecommendVideoListItem(Database.NicoVideo nicoVideo, Mntone.Nico2.Videos.Recommend.Item recommendItem) 
            : base(nicoVideo, isNgEnabled:true, playlistItem:null, requireLatest:true)
        {
            _Item = recommendItem;
            RecommendSourceTag = _Item.AdditionalInfo?.Sherlock.Tag;
        }
    }

    public sealed class RecommendVideoIncrementalLoadingSource : HohoemaIncrementalSourceBase<RecommendVideoListItem>
    {
        private RecommendResponse _RecommendResponse;
        private RecommendContent _PrevRecommendContent;

        private HashSet<string> _RecommendSourceTagsHashSet = new HashSet<string>();
        public ObservableCollection<string> RecommendSourceTags { get; } = new ObservableCollection<string>();

        private bool _EndOfRecommend = false;
        NiconicoContentProvider NiconicoContentProvider;
        public RecommendVideoIncrementalLoadingSource(NiconicoContentProvider contentProvider)
        {
            NiconicoContentProvider = contentProvider;
        }

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
                _PrevRecommendContent = await NiconicoContentProvider.GetRecommendAsync(_RecommendResponse, _PrevRecommendContent);
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

                    return new RecommendVideoListItem(video, x);
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
            _RecommendResponse = await NiconicoContentProvider.GetRecommendFirstAsync();
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

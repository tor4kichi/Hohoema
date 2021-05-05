using Hohoema.Models.Domain.Niconico.Recommend.LoginUser;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Mntone.Nico2.Videos.Recommend;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.LoginUser
{
    public class RecommendPageViewModel : HohoemaListingPageViewModelBase<RecommendVideoListItem>, INavigatedAwareAsync
    {
        public RecommendPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            LoginUserRecommendProvider loginUserRecommendProvider,
            HohoemaPlaylist hohoemaPlaylist,
            PageManager pageManager,
            NicoVideoCacheRepository nicoVideoRepository
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            LoginUserRecommendProvider = loginUserRecommendProvider;
            HohoemaPlaylist = hohoemaPlaylist;
            PageManager = pageManager;
            _nicoVideoRepository = nicoVideoRepository;
        }

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public LoginUserRecommendProvider LoginUserRecommendProvider { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public PageManager PageManager { get; }
        public ReadOnlyObservableCollection<NicoVideoTag> RecommendSourceTags { get; private set; }
        
        protected override IIncrementalSource<RecommendVideoListItem> GenerateIncrementalSource()
        {
            var source = new RecommendVideoIncrementalLoadingSource(LoginUserRecommendProvider, _nicoVideoRepository);
            RecommendSourceTags = source.RecommendSourceTags
               .ToReadOnlyReactiveCollection(x => new NicoVideoTag(x));
            RaisePropertyChanged(nameof(RecommendSourceTags));
            return source;
        }

        private DelegateCommand<string> _OpenTagCommand;
        private readonly NicoVideoCacheRepository _nicoVideoRepository;

        public DelegateCommand<string> OpenTagCommand
        {
            get
            {
                return _OpenTagCommand
                    ?? (_OpenTagCommand = new DelegateCommand<string>((tag) =>
                    {
                        PageManager.Search(SearchTarget.Tag, tag);
                    }));
            }
        }
    }


    public sealed class RecommendVideoListItem : VideoListItemControlViewModel
    {
        Mntone.Nico2.Videos.Recommend.Item _Item;

        public string RecommendSourceTag { get; private set; }


        public RecommendVideoListItem(
            Mntone.Nico2.Videos.Recommend.Item item
            )
            : base(item.Id, item.ParseTitle(), item.ThumbnailUrl, item.ParseLengthToTimeSpan())
        {
            _Item = item;
            RecommendSourceTag = _Item.AdditionalInfo?.Sherlock.Tag;
        }
    }

    public sealed class RecommendVideoIncrementalLoadingSource : HohoemaIncrementalSourceBase<RecommendVideoListItem>
    {
        public RecommendVideoIncrementalLoadingSource(
            LoginUserRecommendProvider loginUserRecommendProvider,
            NicoVideoCacheRepository nicoVideoRepository
            )
        {
            LoginUserRecommendProvider = loginUserRecommendProvider;
            _nicoVideoRepository = nicoVideoRepository;
        }

        public LoginUserRecommendProvider LoginUserRecommendProvider { get; }
        
        private RecommendResponse _RecommendResponse;
        private RecommendContent _PrevRecommendContent;

        private HashSet<string> _RecommendSourceTagsHashSet = new HashSet<string>();
        public ObservableCollection<string> RecommendSourceTags { get; } = new ObservableCollection<string>();

        private bool _EndOfRecommend = false;
        private readonly NicoVideoCacheRepository _nicoVideoRepository;

        public override uint OneTimeLoadCount => 18;


        protected override async IAsyncEnumerable<RecommendVideoListItem> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation] CancellationToken ct = default)
        {
            if (_EndOfRecommend)
            {
                yield break;
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

            ct.ThrowIfCancellationRequested();

            if (_PrevRecommendContent != null && _PrevRecommendContent.Status == "ok")
            {
                _EndOfRecommend = _PrevRecommendContent?.RecommendInfo.EndOfRecommend ?? true;

                AddRecommendTags(
                    _PrevRecommendContent.Items.Select(x => x.AdditionalInfo?.Sherlock.Tag)
                    .Where(x => x != null)
                    );


                foreach (var item in _PrevRecommendContent.Items)
                {
                    var video = _nicoVideoRepository.Get(item.Id);
                    video.ThumbnailUrl = item.ThumbnailUrl;
                    video.Title = item.ParseTitle();
                    video.Length = item.ParseLengthToTimeSpan();
                    video.ViewCount = item.ViewCounter;
                    video.CommentCount = item.NumRes;
                    video.MylistCount = item.MylistCounter;
                    video.PostedAt = item.ParseForstRetroeveToDateTimeOffset().DateTime;
                    _nicoVideoRepository.AddOrUpdate(video);

                    var vm = new RecommendVideoListItem(item);
                    vm.Setup(video);
                    yield return vm;

                    ct.ThrowIfCancellationRequested();
                }
            }
        }

        protected override async ValueTask<int> ResetSourceImpl()
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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Hohoema.Models.Helpers;
using Hohoema.Models;
using Prism.Commands;
using Hohoema.ViewModels.Pages;
using Reactive.Bindings;
using Prism.Navigation;
using Hohoema.Models.Niconico.Video;
using Hohoema.UseCase.Playlist;
using Hohoema.UseCase;
using System.Runtime.CompilerServices;
using System.Threading;
using Hohoema.Models.Repository.Niconico;
using Hohoema.Models.Pages;
using Hohoema.Models.Repository.Niconico.NicoVideo.Recommend;
using Hohoema.ViewModels.Player.Commands;
using Hohoema.ViewModels.Pages.Commands;

namespace Hohoema.ViewModels
{
    public class RecommendPageViewModel : HohoemaListingPageViewModelBase<RecommendVideoListItem>, INavigatedAwareAsync
    {
        public RecommendPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            LoginUserRecommendProvider loginUserRecommendProvider,
            HohoemaPlaylist hohoemaPlaylist,
            PageManager pageManager,
            PlayVideoCommand playVideoCommand,
            OpenPageCommand openPageCommand
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            LoginUserRecommendProvider = loginUserRecommendProvider;
            HohoemaPlaylist = hohoemaPlaylist;
            PageManager = pageManager;
            PlayVideoCommand = playVideoCommand;
            OpenPageCommand = openPageCommand;
        }

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public LoginUserRecommendProvider LoginUserRecommendProvider { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public PageManager PageManager { get; }
        public PlayVideoCommand PlayVideoCommand { get; }
        public OpenPageCommand OpenPageCommand { get; }
        public ReadOnlyObservableCollection<NicoVideoTag> RecommendSourceTags { get; private set; }
        
        protected override IIncrementalSource<RecommendVideoListItem> GenerateIncrementalSource()
        {
            var source = new RecommendVideoIncrementalLoadingSource(LoginUserRecommendProvider);
            RecommendSourceTags = source.RecommendSourceTags
               .ToReadOnlyReactiveCollection(x => new NicoVideoTag(x));
            RaisePropertyChanged(nameof(RecommendSourceTags));
            return source;
        }

        private DelegateCommand<string> _OpenTagCommand;
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


    public sealed class RecommendVideoListItem : VideoInfoControlViewModel
    {
        RecommendItem _Item;

        public string RecommendSourceTag { get; private set; }


        public RecommendVideoListItem(
            RecommendItem item
            )
            : base(item.Id)
        {
            _Item = item;
            RecommendSourceTag = _Item.AdditionalInfo?.Sherlock.Tag;
        }

    }

    public sealed class RecommendVideoIncrementalLoadingSource : HohoemaIncrementalSourceBase<RecommendVideoListItem>
    {
        public RecommendVideoIncrementalLoadingSource(
            LoginUserRecommendProvider loginUserRecommendProvider
            )
        {
            LoginUserRecommendProvider = loginUserRecommendProvider;
        }

        public LoginUserRecommendProvider LoginUserRecommendProvider { get; }

        private RecommendResponse _RecommendResponse;
        private RecommendContent _PrevRecommendContent;

        private HashSet<string> _RecommendSourceTagsHashSet = new HashSet<string>();
        public ObservableCollection<string> RecommendSourceTags { get; } = new ObservableCollection<string>();

        private bool _EndOfRecommend = false;

        

        public override uint OneTimeLoadCount => 18;


        protected override async IAsyncEnumerable<RecommendVideoListItem> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation]CancellationToken cancellationToken)
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


            if (_PrevRecommendContent != null && _PrevRecommendContent.IsOK)
            {
                _EndOfRecommend = _PrevRecommendContent?.RecommendInfo.EndOfRecommend ?? true;

                AddRecommendTags(
                    _PrevRecommendContent.Items.Select(x => x.AdditionalInfo?.Sherlock.Tag)
                    .Where(x => x != null)
                    );


                foreach (var item in _PrevRecommendContent.Items)
                {
                    var video = Database.NicoVideoDb.Get(item.Id);
                    video.ThumbnailUrl = item.ThumbnailUrl;
                    video.Title = item.Title;
                    video.Length = item.Length;
                    video.ViewCount = item.ViewCounter;
                    video.CommentCount = item.NumRes;
                    video.MylistCount = item.MylistCounter;
                    video.PostedAt = item.FirstRetrieve;
                    Database.NicoVideoDb.AddOrUpdate(video);

                    var vm = new RecommendVideoListItem(item);
                    await vm.InitializeAsync(cancellationToken);
                    yield return vm;
                }
            }
            else
            {
                yield break;
            }
        }

        protected override async Task<int> ResetSourceImpl()
        {
            _RecommendResponse = await LoginUserRecommendProvider.GetRecommendFirstAsync();
            if (_RecommendResponse.FirstData.IsOK)
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

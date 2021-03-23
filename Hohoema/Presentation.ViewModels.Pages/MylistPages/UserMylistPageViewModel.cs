using Hohoema.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Reactive.Bindings;
using System.Threading;
using System.Reactive.Linq;
using Windows.UI.Popups;
using Hohoema.Dialogs;
using Hohoema.Models.Domain.Helpers;
using Hohoema.Presentation.Services;
using Hohoema.Presentation.Services.Page;
using Prism.Navigation;
using Hohoema.Models.UseCase.NicoVideos;
using Reactive.Bindings.Extensions;
using Hohoema.Models.UseCase;
using I18NPortable;
using System.Runtime.CompilerServices;
using Hohoema.Models.Domain.Niconico.UserFeature.Mylist;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Niconico.User;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Presentation.ViewModels.NicoVideos.Commands;

namespace Hohoema.Presentation.ViewModels.Pages.MylistPages
{
    public class UserMylistPageViewModel : HohoemaListingPageViewModelBase<MylistPlaylist>, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
	{
        HohoemaPin IPinablePage.GetPin()
        {
            return new HohoemaPin()
            {
                Label = UserName,
                PageType = HohoemaPageType.UserMylist,
                Parameter = $"id={UserId}"
            };
        }

        IObservable<string> ITitleUpdatablePage.GetTitleObservable()
        {
            return this.ObserveProperty(x => x.UserName);
        }

        public UserMylistPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            PageManager pageManager,
            Services.DialogService dialogService,
            NiconicoSession niconicoSession,
            UserProvider userProvider,
            MylistRepository mylistRepository,
            LocalMylistManager localMylistManager,
            HohoemaPlaylist hohoemaPlaylist
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            PageManager = pageManager;
            DialogService = dialogService;
            NiconicoSession = niconicoSession;
            UserProvider = userProvider;
            _mylistRepository = mylistRepository;
            _localMylistManager = localMylistManager;
            
            HohoemaPlaylist = hohoemaPlaylist;
        }

        public HohoemaPlaylist HohoemaPlaylist { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public PageManager PageManager { get; }
        public Services.DialogService DialogService { get; }
        public NiconicoSession NiconicoSession { get; }
        public UserProvider UserProvider { get; }
        private readonly MylistRepository _mylistRepository;
        private readonly LocalMylistManager _localMylistManager;

        public string UserId { get; private set; }

        private string _UserName;
        public string UserName
        {
            get { return _UserName; }
            set { SetProperty(ref _UserName, value); }
        }

        public ReactiveCommand<IPlaylist> OpenMylistCommand { get; private set; }

        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            if (parameters.TryGetValue<string>("id", out string userId))
            {
                UserId = userId;
            }

            if ((UserId == null && NiconicoSession.IsLoggedIn) || NiconicoSession.IsLoginUserId(UserId))
            {
                // ログインユーザー用のマイリスト一覧ページにリダイレクト
                PageManager.ForgetLastPage();
                PageManager.OpenPage(HohoemaPageType.OwnerMylist);

                return;
            }
            else if (UserId != null)
            {
                try
                {
                    var userInfo = await UserProvider.GetUser(UserId);
                    UserName = userInfo.ScreenName;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
            else
            {
                throw new Exception("UserMylistPage が不明なパラメータと共に開かれました : " + parameters.ToString());
            }


            await base.OnNavigatedToAsync(parameters);
        }


        protected override IIncrementalSource<MylistPlaylist> GenerateIncrementalSource()
        {
            if (UserId == null)
            {
                UserId = NiconicoSession.UserIdString;
            }

            return new OtherUserMylistIncrementalLoadingSource(UserId, _mylistRepository);
        }
    }

    public sealed class OtherUserMylistIncrementalLoadingSource : HohoemaIncrementalSourceBase<MylistPlaylist>
    {
        List<MylistPlaylist> _userMylists { get; set; }

        public string UserId { get; }
        public OtherOwneredMylistManager OtherOwneredMylistManager;
        private readonly MylistRepository _mylistRepository;

        public OtherUserMylistIncrementalLoadingSource(string userId, MylistRepository mylistRepository)
        {
            UserId = userId;
            _mylistRepository = mylistRepository;
        }

        protected override async IAsyncEnumerable<MylistPlaylist> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation] CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            foreach (var item in _userMylists.Skip(head).Take(count))
            {
                yield return item;

                ct.ThrowIfCancellationRequested();
            }
        }

        protected override async Task<int> ResetSourceImpl()
        {
            try
            {
                _userMylists = await _mylistRepository.GetUserMylistsAsync(UserId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return _userMylists?.Count ?? 0;
        }
    }
}

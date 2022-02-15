using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Models.Domain.Niconico.User;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Pins;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Models.UseCase.PageNavigation;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.User;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Hohoema.Models.UseCase.Hohoema.LocalMylist;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Mylist
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
            ILoggerFactory loggerFactory,
            ApplicationLayoutManager applicationLayoutManager,
            PageManager pageManager,
            Services.DialogService dialogService,
            NiconicoSession niconicoSession,
            UserProvider userProvider,
            MylistResolver mylistRepository,
            LocalMylistManager localMylistManager
            )
            : base(loggerFactory.CreateLogger<UserMylistPageViewModel>())
        {
            ApplicationLayoutManager = applicationLayoutManager;
            PageManager = pageManager;
            DialogService = dialogService;
            NiconicoSession = niconicoSession;
            UserProvider = userProvider;
            _mylistRepository = mylistRepository;
            _localMylistManager = localMylistManager;
        }

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public PageManager PageManager { get; }
        public Services.DialogService DialogService { get; }
        public NiconicoSession NiconicoSession { get; }
        public UserProvider UserProvider { get; }
        private readonly MylistResolver _mylistRepository;
        private readonly LocalMylistManager _localMylistManager;

        public UserId? UserId { get; private set; }

        private string _UserName;
        public string UserName
        {
            get { return _UserName; }
            set { SetProperty(ref _UserName, value); }
        }

        public ReactiveCommand<IPlaylist> OpenMylistCommand { get; private set; }

        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            if (parameters.TryGetValue("id", out string userId))
            {
                UserId = userId;
            }
            else if (parameters.TryGetValue("id", out UserId justUserId))
            {
                UserId = justUserId;
            }


            if ((!UserId.HasValue && NiconicoSession.IsLoggedIn) || UserId.HasValue && (NiconicoSession.IsLoginUserId(UserId.Value)))
            {
                // ログインユーザー用のマイリスト一覧ページにリダイレクト
                PageManager.ForgetLastPage();
                PageManager.OpenPage(HohoemaPageType.OwnerMylistManage);

                return;
            }
            else if (UserId != null)
            {
                try
                {
                    var userInfo = await UserProvider.GetUserInfoAsync(UserId.Value);
                    UserName = userInfo.ScreenName;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
            else
            {
                throw new Models.Infrastructure.HohoemaExpception("UserMylistPage が不明なパラメータと共に開かれました : " + parameters.ToString());
            }


            await base.OnNavigatedToAsync(parameters);
        }


        protected override (int, IIncrementalSource<MylistPlaylist>) GenerateIncrementalSource()
        {
            return (25 /* 全件取得するため指定不要 */, new OtherUserMylistIncrementalLoadingSource(UserId, _mylistRepository, _logger));
        }
    }

    public sealed class OtherUserMylistIncrementalLoadingSource : IIncrementalSource<MylistPlaylist>
    {
        List<MylistPlaylist> _userMylists { get; set; }

        public string UserId { get; }

        private readonly MylistResolver _mylistRepository;
        private readonly ILogger _logger;

        public OtherUserMylistIncrementalLoadingSource(
            string userId, 
            MylistResolver mylistRepository,
            ILogger logger
            )
        {
            UserId = userId;
            _mylistRepository = mylistRepository;
            _logger = logger;
        }

        async Task<IEnumerable<MylistPlaylist>> IIncrementalSource<MylistPlaylist>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken)
        {
            try
            {
                _userMylists ??= await _mylistRepository.GetUserMylistsAsync(UserId);

                var head = pageIndex * pageSize;
                return _userMylists.Skip(head).Take(pageSize);
            }
            catch (Exception ex)
            {
                _logger.ZLogErrorWithPayload(exception: ex, UserId, "UserMylists loading failed");
                return Enumerable.Empty<MylistPlaylist>();
            }

        }
    }
}

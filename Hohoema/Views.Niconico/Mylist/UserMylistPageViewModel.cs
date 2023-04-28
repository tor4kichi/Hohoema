#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.User;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Pins;
using Hohoema.Models.Playlist;
using Hohoema.Services;
using Hohoema.Services.LocalMylist;
using Hohoema.Services.Playlist;
using Hohoema.ViewModels.Navigation.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.User;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZLogger;

namespace Hohoema.ViewModels.Pages.Niconico.Mylist;

public class UserMylistPageViewModel : HohoemaListingPageViewModelBase<MylistPlaylist>, IPinablePage, ITitleUpdatablePage
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
        IMessenger messenger,
        ILoggerFactory loggerFactory,
        ApplicationLayoutManager applicationLayoutManager,
        OpenPageCommand openPageCommand,
        Services.DialogService dialogService,
        NiconicoSession niconicoSession,
        UserProvider userProvider,
        MylistResolver mylistRepository,
        LocalMylistManager localMylistManager
        )
        : base(loggerFactory.CreateLogger<UserMylistPageViewModel>())
    {
        _messenger = messenger;
        ApplicationLayoutManager = applicationLayoutManager;
        OpenPageCommand = openPageCommand;
        DialogService = dialogService;
        NiconicoSession = niconicoSession;
        UserProvider = userProvider;
        _mylistRepository = mylistRepository;
        _localMylistManager = localMylistManager;
    }

    public ApplicationLayoutManager ApplicationLayoutManager { get; }
    public OpenPageCommand OpenPageCommand { get; }    
    public Services.DialogService DialogService { get; }
    public NiconicoSession NiconicoSession { get; }
    public UserProvider UserProvider { get; }

    private readonly IMessenger _messenger;
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
            // TODO: ForgetLastの実装
            _ =  _messenger.OpenPageAsync(HohoemaPageType.OwnerMylistManage);
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
            throw new Infra.HohoemaException("UserMylistPage が不明なパラメータと共に開かれました : " + parameters.ToString());
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
            return _userMylists.Skip(head).Take(pageSize)
                .ToArray()// Note: IncrementalLoadingSourceが複数回呼び出すためFreezeしたい
                ;
        }
        catch (Exception ex)
        {
            _logger.ZLogErrorWithPayload(exception: ex, UserId, "UserMylists loading failed");
            return Enumerable.Empty<MylistPlaylist>();
        }

    }
}

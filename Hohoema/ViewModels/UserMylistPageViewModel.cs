using Hohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Reactive.Bindings;
using System.Threading;
using System.Reactive.Linq;
using Windows.UI.Popups;
using Hohoema.Models.Helpers;
using Hohoema.ViewModels.Pages;
using Prism.Navigation;
using Hohoema.UseCase.Playlist;
using Hohoema.Interfaces;
using Reactive.Bindings.Extensions;
using Hohoema.UseCase;
using I18NPortable;
using Hohoema.Commands.Mylist;
using Hohoema.Models.Repository.Niconico.Mylist;
using Hohoema.Models.Pages;
using Hohoema.Models.Niconico;
using Hohoema.Models.Repository.Niconico;
using Hohoema.UseCase.Services;
using Hohoema.Models.Repository;

namespace Hohoema.ViewModels
{
    public class UserMylistPageViewModel : HohoemaListingPageViewModelBase<MylistPlaylist>, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
	{
        Models.Pages.HohoemaPin IPinablePage.GetPin()
        {
            return new Models.Pages.HohoemaPin()
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
            ITextInputDialogService textInputDialogService,
            IEditMylistGroupDialogService editMylistGroupDialogService,
            NiconicoSession niconicoSession,
            UserProvider userProvider,
            MylistRepository mylistRepository,
            UserMylistManager userMylistManager,
            LocalMylistManager localMylistManager,
            HohoemaPlaylist hohoemaPlaylist,
            CreateLocalMylistCommand createLocalMylistCommand
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            PageManager = pageManager;
            _textInputDialogService = textInputDialogService;
            _editMylistGroupDialogService = editMylistGroupDialogService;
            NiconicoSession = niconicoSession;
            UserProvider = userProvider;
            _mylistRepository = mylistRepository;
            _userMylistManager = userMylistManager;
            _localMylistManager = localMylistManager;
            CreateLocalMylistCommand = createLocalMylistCommand;
            HohoemaPlaylist = hohoemaPlaylist;
            IsLoginUserMylist = new ReactiveProperty<bool>(false);

            OpenMylistCommand = new ReactiveCommand<IPlaylist>();

            OpenMylistCommand.Subscribe(listItem =>
            {
                PageManager.OpenPage(HohoemaPageType.Mylist, $"id={listItem.Id}");
            });

            AddMylistGroupCommand = new DelegateCommand(async () =>
            {
                MylistGroupEditData data = new MylistGroupEditData()
                {
                    Name = "",
                    Description = "",
                    IsPublic = false,
                    MylistDefaultSort = MylistGroupDefaultSort.Latest,
                    IconType = MylistGroupIconType.Default,
                };

                // 成功するかキャンセルが押されるまで繰り返す
                while (true)
                {
                    if (true == await _editMylistGroupDialogService.ShowCreateMylistGroupDialogAsync(data))
                    {
                        var result = await _userMylistManager.AddMylist(
                            data.Name,
                            data.Description,
                            data.IsPublic,
                            data.MylistDefaultSort,
                            data.IconType
                        );

                        if (result == ContentManageResult.Success)
                        {
                            await ResetList();
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }


            }
            , () => _userMylistManager.Mylists.Count < _userMylistManager.MaxMylistGroupCountCurrentUser
            );

            RemoveMylistGroupCommand = new DelegateCommand<IPlaylist>(async (item) =>
            {
                {
                    if (item is LocalPlaylist localPlaylist)
                    {
                        if (localPlaylist.IsWatchAfterPlaylist()) { return; }
                    }
                    else if (item is LoginUserMylistPlaylist loginUserMylist)
                    {
                        if (loginUserMylist.IsDefaultMylist()) { return; }
                    }
                }

                // 確認ダイアログ
                var contentMessage = "ConfirmDeleteX_ImpossibleReDo".Translate(item.Label);

                var dialog = new MessageDialog(contentMessage, "ConfirmDeleteX".Translate(item.GetOrigin().Translate()));
                dialog.Commands.Add(new UICommand("Delete".Translate(), async (i) =>
                {
                    if (item is LocalPlaylist localPlaylist)
                    {
                        _localMylistManager.RemovePlaylist(localPlaylist);
                    }
                    else if (item is LoginUserMylistPlaylist loginUserMylist)
                    {
                        await _userMylistManager.RemoveMylist(item.Id);
                    }
                }));

                dialog.Commands.Add(new UICommand("Cancel".Translate()));
                dialog.CancelCommandIndex = 1;
                dialog.DefaultCommandIndex = 1;

                await dialog.ShowAsync();
            });


            EditMylistGroupCommand = new DelegateCommand<IPlaylist>(async item =>
            {
                if (item is LocalPlaylist localPlaylist)
                {
                    if (item.Id == HohoemaPlaylist.WatchAfterPlaylistId)
                    {
                        return;
                    }

                    var resultText = await _textInputDialogService.GetTextAsync("RenameLocalPlaylist".Translate(),
                        localPlaylist.Label,
                        localPlaylist.Label,
                        (tempName) => !string.IsNullOrWhiteSpace(tempName)
                        );

                    if (!string.IsNullOrWhiteSpace(resultText))
                    {
                        localPlaylist.Label = resultText;
                    }
                }
                else if (item is LoginUserMylistPlaylist loginUserMylist)
                {
                    if (loginUserMylist.IsDefaultMylist())
                    {
                        return;
                    }

                    MylistGroupEditData data = new MylistGroupEditData()
                    {
                        Name = loginUserMylist.Label,
                        Description = loginUserMylist.Description,
                        IsPublic = loginUserMylist.IsPublic,
                        MylistDefaultSort = loginUserMylist.DefaultSort,
                        IconType = loginUserMylist.IconType,
                    };

                    // 成功するかキャンセルが押されるまで繰り返す
                    while (true)
                    {
                        if (true == await _editMylistGroupDialogService.ShowCreateMylistGroupDialogAsync(data))
                        {
                            var result = await loginUserMylist.UpdateMylist(data);

                            if (result == ContentManageResult.Success)
                            {
                                await _navigationService.RefreshAsync();
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            });
        }
        private INavigationService _navigationService;

        public UserMylistManager _userMylistManager { get; private set; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public PageManager PageManager { get; }
        public NiconicoSession NiconicoSession { get; }
        public UserProvider UserProvider { get; }

        private readonly ITextInputDialogService _textInputDialogService;
        private readonly IEditMylistGroupDialogService _editMylistGroupDialogService;
        private readonly MylistRepository _mylistRepository;
        private readonly LocalMylistManager _localMylistManager;

        public string UserId { get; private set; }

        private string _UserName;

        public string UserName
        {
            get { return _UserName; }
            set { SetProperty(ref _UserName, value); }
        }


        public ReactiveProperty<bool> IsLoginUserMylist { get; private set; }


        public ReactiveCommand<IPlaylist> OpenMylistCommand { get; private set; }
        public DelegateCommand AddMylistGroupCommand { get; private set; }
        public DelegateCommand<IPlaylist> RemoveMylistGroupCommand { get; private set; }
        public DelegateCommand<IPlaylist> EditMylistGroupCommand { get; private set; }
        
        public CreateLocalMylistCommand CreateLocalMylistCommand { get; private set; }


        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            _navigationService = parameters.GetNavigationService();

            if (parameters.TryGetValue<string>("id", out string userId))
            {
                UserId = userId;
            }

            if ((UserId == null && NiconicoSession.IsLoggedIn) || NiconicoSession.IsLoginUserId(UserId))
            {
                IsLoginUserMylist.Value = true;

                // ログインユーザーのマイリスト一覧を表示
                UserName = NiconicoSession.UserName;
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

            AddMylistGroupCommand.RaiseCanExecuteChanged();

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

        protected override IAsyncEnumerable<MylistPlaylist> GetPagedItemsImpl(int head, int count, CancellationToken cancellationToken)
        {
            return _userMylists.Skip(head).Take(count).ToAsyncEnumerable();
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

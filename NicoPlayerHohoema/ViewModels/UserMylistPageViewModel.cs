using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Mntone.Nico2.Mylist.MylistGroup;
using Reactive.Bindings;
using Mntone.Nico2.Mylist;
using System.Threading;
using System.Reactive.Linq;
using Windows.UI.Popups;
using NicoPlayerHohoema.Dialogs;
using Mntone.Nico2.Searches.Mylist;
using NicoPlayerHohoema.Models.Helpers;
using NicoPlayerHohoema.Models.Provider;
using NicoPlayerHohoema.Models.LocalMylist;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Services.Page;
using Prism.Navigation;
using NicoPlayerHohoema.Repository.Playlist;
using NicoPlayerHohoema.UseCase.Playlist;
using NicoPlayerHohoema.Interfaces;
using Reactive.Bindings.Extensions;
using NicoPlayerHohoema.UseCase;
using I18NPortable;
using NicoPlayerHohoema.Commands.Mylist;
using System.Runtime.CompilerServices;

namespace NicoPlayerHohoema.ViewModels
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
            Services.PageManager pageManager,
            Services.DialogService dialogService,
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
            DialogService = dialogService;
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
                    DefaultSortKey = Mntone.Nico2.Users.Mylist.MylistSortKey.AddedAt,
                    DefaultSortOrder = Mntone.Nico2.Users.Mylist.MylistSortOrder.Desc
                };

                // 成功するかキャンセルが押されるまで繰り返す
                while (true)
                {
                    if (true == await DialogService.ShowCreateMylistGroupDialogAsync(data))
                    {
                        var result = await _userMylistManager.AddMylist(
                            data.Name,
                            data.Description,
                            data.IsPublic,
                            data.DefaultSortKey,
                            data.DefaultSortOrder
                        );

                        if (result != null)
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

            RemoveMylistGroupCommand = new DelegateCommand<Interfaces.IPlaylist>(async (item) =>
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


            EditMylistGroupCommand = new DelegateCommand<Interfaces.IPlaylist>(async item =>
            {
                throw new NotImplementedException();
                /*
                if (item is LocalPlaylist localPlaylist)
                {
                    if (item.Id == HohoemaPlaylist.WatchAfterPlaylistId)
                    {
                        return;
                    }

                    var resultText = await DialogService.GetTextAsync("RenameLocalPlaylist".Translate(),
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
                        DefaultSortKey = loginUserMylist.DefaultSort,
                        IconType = loginUserMylist.IconType,
                    };

                    // 成功するかキャンセルが押されるまで繰り返す
                    while (true)
                    {
                        if (true == await DialogService.ShowCreateMylistGroupDialogAsync(data))
                        {
                            var result = await loginUserMylist.UpdateMylist(data);

                            if (result == Mntone.Nico2.ContentManageResult.Success)
                            {
                                loginUserMylist.Label = data.Name;
                                loginUserMylist.Description = data.Description;
                                loginUserMylist.IsPublic = data.IsPublic;
                                loginUserMylist.DefaultSort = data.DefaultSortKey;
                                loginUserMylist.IconType = data.IconType;

                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                */
            });
        }

        public UserMylistManager _userMylistManager { get; private set; }
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


        public ReactiveProperty<bool> IsLoginUserMylist { get; private set; }


        public ReactiveCommand<Interfaces.IPlaylist> OpenMylistCommand { get; private set; }
        public DelegateCommand AddMylistGroupCommand { get; private set; }
        public DelegateCommand<Interfaces.IPlaylist> RemoveMylistGroupCommand { get; private set; }
        public DelegateCommand<Interfaces.IPlaylist> EditMylistGroupCommand { get; private set; }
        
        public CreateLocalMylistCommand CreateLocalMylistCommand { get; private set; }


        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
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

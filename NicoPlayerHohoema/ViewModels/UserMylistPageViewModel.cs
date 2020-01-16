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
using System.Collections.Async;
using NicoPlayerHohoema.Models.Provider;
using NicoPlayerHohoema.Models.LocalMylist;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Services.Page;
using Prism.Navigation;
using NicoPlayerHohoema.Repository.Playlist;
using NicoPlayerHohoema.UseCase.Playlist;
using NicoPlayerHohoema.Interfaces;

namespace NicoPlayerHohoema.ViewModels
{
    public class UserMylistPageViewModel : HohoemaListingPageViewModelBase<MylistPlaylist>, INavigatedAwareAsync, IPinablePage
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

        public UserMylistPageViewModel(
            Services.PageManager pageManager,
            Services.DialogService dialogService,
            NiconicoSession niconicoSession,
            UserProvider userProvider,
            MylistRepository mylistRepository,
            UserMylistManager userMylistManager,
            LocalMylistManager localMylistManager,
            HohoemaPlaylist hohoemaPlaylist
            )
        {
            PageManager = pageManager;
            DialogService = dialogService;
            NiconicoSession = niconicoSession;
            UserProvider = userProvider;
            _mylistRepository = mylistRepository;
            _userMylistManager = userMylistManager;
            _localMylistManager = localMylistManager;
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
                    Name = "新しいマイリスト",
                    Description = "",
                    IsPublic = false,
                    MylistDefaultSort = MylistDefaultSort.Latest,
                    IconType = IconType.Default,
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
                            data.MylistDefaultSort,
                            data.IconType
                        );

                        if (result == Mntone.Nico2.ContentManageResult.Success)
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
                var originText = item.GetOrigin() == PlaylistOrigin.Local ? "ローカルマイリスト" : "マイリスト";
                var contentMessage = $"{item.Label} を削除してもよろしいですか？（変更は元に戻せません）";

                var dialog = new MessageDialog(contentMessage, $"{originText}削除の確認");
                dialog.Commands.Add(new UICommand("削除", async (i) =>
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

                dialog.Commands.Add(new UICommand("キャンセル"));
                dialog.CancelCommandIndex = 1;
                dialog.DefaultCommandIndex = 1;

                await dialog.ShowAsync();
            });


            EditMylistGroupCommand = new DelegateCommand<Interfaces.IPlaylist>(async item =>
            {
                if (item is LocalPlaylist localPlaylist)
                {
                    if (item.Id == HohoemaPlaylist.WatchAfterPlaylistId)
                    {
                        return;
                    }

                    var resultText = await DialogService.GetTextAsync("プレイリスト名を変更",
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
                        if (true == await DialogService.ShowCreateMylistGroupDialogAsync(data))
                        {
                            var result = await _userMylistManager.UpdateMylist(loginUserMylist.Id, data);

                            if (result == Mntone.Nico2.ContentManageResult.Success)
                            {
                                loginUserMylist.Label = data.Name;
                                loginUserMylist.Description = data.Description;
                                loginUserMylist.IsPublic = data.IsPublic;
                                loginUserMylist.DefaultSort = data.MylistDefaultSort;
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
            });



            AddLocalMylistCommand = new DelegateCommand(async () =>
            {
                var name = await DialogService.GetTextAsync("新しいローカルマイリスト名を入力", "ローカルマイリスト名", "",
                    (s) =>
                    {
                        if (string.IsNullOrWhiteSpace(s)) { return false; }

                        if (_localMylistManager.LocalPlaylists.Any(x => x.Label == s))
                        {
                            return false;
                        }

                        return true;
                    });

                if (name != null)
                {
                    _localMylistManager.CreatePlaylist(name);
                }
            });
            
        }

        public UserMylistManager _userMylistManager { get; private set; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
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
        
        public DelegateCommand AddLocalMylistCommand { get; private set; }


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

        protected override Task<IAsyncEnumerable<MylistPlaylist>> GetPagedItemsImpl(int head, int count)
        {
            return Task.FromResult(_userMylists.Skip(head).Take(count).ToAsyncEnumerable());
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

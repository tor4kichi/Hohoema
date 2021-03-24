using Hohoema.Dialogs;
using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Niconico.UserFeature.Mylist;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Presentation.Services;
using Hohoema.Presentation.Services.Page;
using Hohoema.Presentation.ViewModels.NicoVideos.Commands;
using I18NPortable;
using Microsoft.Toolkit.Uwp.UI;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Windows.UI.Popups;

namespace Hohoema.Presentation.ViewModels.Pages.MylistPages
{
    public class OwnerMylistPageViewModel : HohoemaViewModelBase
    {
        ObservableCollection<IPlaylist> _sourcePlaylistItems = new ObservableCollection<IPlaylist>();
        public AdvancedCollectionView ItemsView { get; }
        public ReactivePropertySlim<bool> NowLoading { get; } = new ReactivePropertySlim<bool>();

        private readonly NiconicoSession _niconicoSession;
        private readonly PageManager _pageManager;
        private readonly DialogService _dialogService;
        private readonly UserMylistManager _userMylistManager;
        private readonly LocalMylistManager _localMylistManager;

        public ApplicationLayoutManager ApplicationLayoutManager { get; }

        public ReactiveCommand<IPlaylist> OpenMylistCommand { get; private set; }
        public DelegateCommand AddMylistGroupCommand { get; private set; }
        public DelegateCommand<IPlaylist> RemoveMylistGroupCommand { get; private set; }
        public DelegateCommand<IPlaylist> EditMylistGroupCommand { get; private set; }
        public LocalPlaylistCreateCommand CreateLocalMylistCommand { get; private set; }




        public OwnerMylistPageViewModel(
            NiconicoSession niconicoSession,
            PageManager pageManager,
            Services.DialogService dialogService,
            ApplicationLayoutManager applicationLayoutManager,
            UserMylistManager userMylistManager,
            LocalMylistManager localMylistManager,
            LocalPlaylistCreateCommand createLocalMylistCommand
            )
        {
            _niconicoSession = niconicoSession;
            _pageManager = pageManager;
            _dialogService = dialogService;
            ApplicationLayoutManager = applicationLayoutManager;
            _userMylistManager = userMylistManager;
            _localMylistManager = localMylistManager;
            CreateLocalMylistCommand = createLocalMylistCommand;

            ItemsView = new AdvancedCollectionView(_sourcePlaylistItems);

            OpenMylistCommand = new ReactiveCommand<IPlaylist>();

            OpenMylistCommand.Subscribe(listItem =>
            {
                _pageManager.OpenPageWithId(HohoemaPageType.Mylist, listItem.Id);
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
                    if (true == await _dialogService.ShowCreateMylistGroupDialogAsync(data))
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
                            await RefreshPlaylistItems();
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

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            new[]
            {
                _niconicoSession.ObserveProperty(x => x.IsLoggedIn),
            }
                .Merge()
                .Subscribe(async _ =>
                {
                    await RefreshPlaylistItems();
                })
                .AddTo(_NavigatingCompositeDisposable);
        }

        private async Task RefreshPlaylistItems()
        {
            NowLoading.Value = true;
            try
            {
                _sourcePlaylistItems.Clear();

                // TODO: タイムアウト処理を追加する
                using var _ = await _niconicoSession.SigninLock.LockAsync();
                await _userMylistManager.WaitUpdate();

                if (_niconicoSession.IsLoggedIn)
                {
                    _sourcePlaylistItems.AddRange(_userMylistManager.Mylists);
                }

                _sourcePlaylistItems.AddRange(_localMylistManager.LocalPlaylists);
            }
            finally
            {
                NowLoading.Value = false;
            }

            AddMylistGroupCommand.RaiseCanExecuteChanged();
        }





    }
}

using Hohoema.Dialogs;
using Hohoema.Models;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.Mylist.LoginUser;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Playlist;
using Hohoema.Services;
using Hohoema.Services.Playlist;
using Hohoema.Services;
using Hohoema.Services.PageNavigation;
using Hohoema.ViewModels.Niconico.Video.Commands;
using I18NPortable;
using Microsoft.Toolkit.Uwp.UI;
using CommunityToolkit.Mvvm.Input;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.System;
using Microsoft.Toolkit.Uwp;
using NiconicoToolkit.Mylist;
using Hohoema.Navigations;

namespace Hohoema.ViewModels.Pages.Niconico.Mylist
{

    public class OwnerMylistManagePageViewModel : HohoemaPageViewModelBase
    {
        ObservableCollection<IPlaylist> _sourcePlaylistItems = new ObservableCollection<IPlaylist>();
        public AdvancedCollectionView ItemsView { get; }
        public ReactivePropertySlim<bool> NowLoading { get; } = new ReactivePropertySlim<bool>();

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly NiconicoSession _niconicoSession;
        private readonly PageManager _pageManager;
        private readonly DialogService _dialogService;
        private readonly LoginUserOwnedMylistManager _userMylistManager;

        public ApplicationLayoutManager ApplicationLayoutManager { get; }

        public PlaylistPlayAllCommand PlaylistPlayAllCommand { get; }
        public ReactiveCommand<LoginUserMylistPlaylist> OpenMylistCommand { get;  }
        public RelayCommand AddMylistGroupCommand { get; }
        public RelayCommand<LoginUserMylistPlaylist> RemoveMylistGroupCommand { get; }
        public RelayCommand<LoginUserMylistPlaylist> EditMylistGroupCommand { get; }

        public OwnerMylistManagePageViewModel(
            NiconicoSession niconicoSession,
            PageManager pageManager,
            Services.DialogService dialogService,
            ApplicationLayoutManager applicationLayoutManager,
            LoginUserOwnedMylistManager userMylistManager,
            PlaylistPlayAllCommand playlistPlayAllCommand
            )
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _niconicoSession = niconicoSession;
            _pageManager = pageManager;
            _dialogService = dialogService;
            ApplicationLayoutManager = applicationLayoutManager;
            _userMylistManager = userMylistManager;
            PlaylistPlayAllCommand = playlistPlayAllCommand;

            ItemsView = new AdvancedCollectionView(_sourcePlaylistItems);

            OpenMylistCommand = new ReactiveCommand<LoginUserMylistPlaylist>()
                .AddTo(_CompositeDisposable);

            OpenMylistCommand.Subscribe(listItem =>
            {
                _pageManager.OpenPageWithId(HohoemaPageType.Mylist, listItem.MylistId);
            });

            AddMylistGroupCommand = new RelayCommand(async () =>
            {
                MylistGroupEditData data = new MylistGroupEditData()
                {
                    Name = "",
                    Description = "",
                    IsPublic = false,
                    DefaultSortKey = MylistSortKey.AddedAt,
                    DefaultSortOrder = MylistSortOrder.Desc
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

            RemoveMylistGroupCommand = new RelayCommand<LoginUserMylistPlaylist>(async (mylist) =>
            {
                if (mylist.MylistId.IsWatchAfterMylist) { return; }

                // 確認ダイアログ
                var contentMessage = "ConfirmDeleteX_ImpossibleReDo".Translate(mylist.Name);

                var dialog = new MessageDialog(contentMessage, "ConfirmDeleteX".Translate("Mylist".Translate()));
                dialog.Commands.Add(new UICommand("Delete".Translate(), async (i) =>
                {
                    if (await _userMylistManager.RemoveMylist(mylist.MylistId))
                    {
                        await RefreshPlaylistItems();
                    }
                }));

                dialog.Commands.Add(new UICommand("Cancel".Translate()));
                dialog.CancelCommandIndex = 1;
                dialog.DefaultCommandIndex = 1;

                await dialog.ShowAsync();
            });


            EditMylistGroupCommand = new RelayCommand<LoginUserMylistPlaylist>(async mylist =>
            {
                if (mylist.MylistId.IsWatchAfterMylist)
                {
                    return;
                }

                MylistGroupEditData data = new MylistGroupEditData()
                {
                    Name = mylist.Name,
                    Description = mylist.Description,
                    IsPublic = mylist.IsPublic,
                    DefaultSortKey = mylist.DefaultSortKey,
                    DefaultSortOrder = mylist.DefaultSortOrder,
                };

                // 成功するかキャンセルが押されるまで繰り返す
                while (true)
                {
                    if (true == await _dialogService.ShowCreateMylistGroupDialogAsync(data))
                    {
                        if (await mylist.UpdateMylistInfo(data.Name, data.Description, data.IsPublic, data.DefaultSortKey, data.DefaultSortOrder))
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            });
        }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            new[]
            {
                _niconicoSession.ObserveProperty(x => x.IsLoggedIn).ToUnit(),
                _userMylistManager.Mylists.CollectionChangedAsObservable().ToUnit(),
            }
            .Merge()
            .Subscribe(async _ =>
            {
                await RefreshPlaylistItems();
            })
            .AddTo(_navigationDisposables);

            /*
            new[]
            {
                SelectedDisplayMylistKind.ToUnit()
            }
            .Merge()
            .Subscribe(_ => ItemsView.RefreshFilter())
            .AddTo(_NavigatingCompositeDisposable);
            */
        }

        private async Task RefreshPlaylistItems()
        {
            await _dispatcherQueue.EnqueueAsync(async () =>
            {
                NowLoading.Value = true;
                try
                {
                    using (ItemsView.DeferRefresh())
                    {
                        _sourcePlaylistItems.Clear();

                        // TODO: タイムアウト処理を追加する
                        using var _ = await _niconicoSession.SigninLock.LockAsync();
                        await _userMylistManager.WaitUpdate();
                        
                        foreach (var item in _userMylistManager.Mylists.Where(x => x.MylistId.IsWatchAfterMylist is false))
                        {
                            _sourcePlaylistItems.Add(item);
                        }                        
                    }
                }
                finally
                {
                    NowLoading.Value = false;
                }

                AddMylistGroupCommand.NotifyCanExecuteChanged();
            });
        }
    }
}

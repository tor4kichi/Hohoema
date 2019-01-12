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

namespace NicoPlayerHohoema.ViewModels
{
    public class UserMylistPageViewModel : HohoemaListingPageViewModelBase<Interfaces.IMylist>
	{
        public UserMylistPageViewModel(
            Services.PageManager pageManager,
            Services.DialogService dialogService,
            NiconicoSession niconicoSession,
            UserProvider userProvider,
            LoginUserMylistProvider loginUserMylistProvider,
            OtherOwneredMylistManager otherOwneredMylistManager,
            UserMylistManager userMylistManager,
            LocalMylistManager localMylistManager,
            HohoemaPlaylist hohoemaPlaylist
            )
        {
            PageManager = pageManager;
            DialogService = dialogService;
            NiconicoSession = niconicoSession;
            UserProvider = userProvider;
            LoginUserMylistProvider = loginUserMylistProvider;
            OtherOwneredMylistManager = otherOwneredMylistManager;
            UserMylistManager = userMylistManager;
            LocalMylistManager = localMylistManager;
            HohoemaPlaylist = hohoemaPlaylist;
            IsLoginUserMylist = new ReactiveProperty<bool>(false);

            OpenMylistCommand = new ReactiveCommand<Interfaces.IMylist>();

            OpenMylistCommand.Subscribe(listItem =>
            {
                PageManager.OpenPage(HohoemaPageType.Mylist, $"id={listItem.Id}&origin={listItem.ToMylistOrigin().ToString()}");
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
                        var result = await UserMylistManager.AddMylist(
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
            , () => UserMylistManager.Mylists.Count < UserMylistManager.MaxMylistGroupCountCurrentUser
            );

            RemoveMylistGroupCommand = new DelegateCommand<Interfaces.IMylist>(async (item) =>
            {
                var mylistOrigin = item.ToMylistOrigin();
                if (mylistOrigin == PlaylistOrigin.Local)
                {
                    if (item.Id == HohoemaPlaylist.WatchAfterPlaylistId) { return; }
                }
                else if (mylistOrigin == PlaylistOrigin.LoginUser)
                {
                    if (item.Id == "0") { return; }
                }

                // 確認ダイアログ
                var originText = mylistOrigin == PlaylistOrigin.Local ? "ローカルマイリスト" : "マイリスト";
                var contentMessage = $"{item.Label} を削除してもよろしいですか？（変更は元に戻せません）";

                var dialog = new MessageDialog(contentMessage, $"{originText}削除の確認");
                dialog.Commands.Add(new UICommand("削除", async (i) =>
                {
                    if (mylistOrigin == PlaylistOrigin.Local)
                    {
                        LocalMylistManager.RemoveCommand.Execute(item as LocalMylistGroup);
                    }
                    else if (mylistOrigin == PlaylistOrigin.LoginUser)
                    {
                        await UserMylistManager.RemoveMylist(item.Id);
                        //                        await UpdateUserMylist();
                    }
                }));

                dialog.Commands.Add(new UICommand("キャンセル"));
                dialog.CancelCommandIndex = 1;
                dialog.DefaultCommandIndex = 1;

                await dialog.ShowAsync();
            });


            EditMylistGroupCommand = new DelegateCommand<Interfaces.IMylist>(async item =>
            {
                var mylistOrigin = item.ToMylistOrigin();
                if (mylistOrigin == PlaylistOrigin.Local)
                {
                    if (item.Id == HohoemaPlaylist.WatchAfterPlaylistId) { return; }
                }
                else if (mylistOrigin == PlaylistOrigin.LoginUser)
                {
                    if (item.Id == "0") { return; }
                }

                if (mylistOrigin == PlaylistOrigin.Local)
                {
                    var localMylist = item as LocalMylistGroup;
                    var resultText = await DialogService.GetTextAsync("プレイリスト名を変更",
                        localMylist.Label,
                        localMylist.Label,
                        (tempName) => !string.IsNullOrWhiteSpace(tempName)
                        );

                    if (!string.IsNullOrWhiteSpace(resultText))
                    {
                        localMylist.Label = resultText;
                    }
                }


                if (mylistOrigin == PlaylistOrigin.LoginUser)
                {
                    var mylistGroupListItem = item as UserOwnedMylist;
                    var selectedMylistGroupId = mylistGroupListItem.Id;

                    if (selectedMylistGroupId == null) { return; }

                    var mylistGroup = UserMylistManager.GetMylistGroup(selectedMylistGroupId);
                    MylistGroupEditData data = new MylistGroupEditData()
                    {
                        Name = mylistGroup.Label,
                        Description = mylistGroup.Description,
                        IsPublic = mylistGroup.IsPublic,
                        MylistDefaultSort = mylistGroup.Sort,
                        IconType = mylistGroup.IconType,
                    };

                    // 成功するかキャンセルが押されるまで繰り返す
                    while (true)
                    {
                        if (true == await DialogService.ShowCreateMylistGroupDialogAsync(data))
                        {
                            mylistGroup.Label = data.Name;
                            mylistGroup.Description = data.Description;
                            mylistGroup.IsPublic = data.IsPublic;
                            mylistGroup.Sort = data.MylistDefaultSort;
                            mylistGroup.IconType = data.IconType;
                            var result = await LoginUserMylistProvider.UpdateMylist(mylistGroup);

                            if (result == Mntone.Nico2.ContentManageResult.Success)
                            {
                                // TODO: UI上のマイリスト表示を更新する
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

            PlayAllCommand = new DelegateCommand<Interfaces.IMylist>((mylist) =>
            {
                if (mylist.ItemCount == 0) { return; }

                HohoemaPlaylist.Play(mylist);
            });



            AddLocalMylistCommand = new DelegateCommand(async () =>
            {
                var name = await DialogService.GetTextAsync("新しいローカルマイリスト名を入力", "ローカルマイリスト名", "",
                    (s) =>
                    {
                        if (string.IsNullOrWhiteSpace(s)) { return false; }

                        if (LocalMylistManager.Mylists.Any(x => x.Label == s))
                        {
                            return false;
                        }

                        return true;
                    });

                if (name != null)
                {
                    LocalMylistManager.Mylists.Add(new LocalMylistGroup(Guid.NewGuid().ToString(), name));
                }
            });
            
        }

        public UserMylistManager UserMylistManager { get; private set; }
        public LocalMylistManager LocalMylistManager { get; }
        public Services.HohoemaPlaylist HohoemaPlaylist { get; }
        public OtherOwneredMylistManager OtherOwneredMylistManager { get; }
        public PageManager PageManager { get; }
        public Services.DialogService DialogService { get; }
        public NiconicoSession NiconicoSession { get; }
        public UserProvider UserProvider { get; }
        public LoginUserMylistProvider LoginUserMylistProvider { get; }


        public string UserId { get; private set; }

        private string _UserName;
        public string UserName
        {
            get { return _UserName; }
            set { SetProperty(ref _UserName, value); }
        }


        public ReactiveProperty<bool> IsLoginUserMylist { get; private set; }


        public ReactiveCommand<Interfaces.IMylist> OpenMylistCommand { get; private set; }
        public DelegateCommand AddMylistGroupCommand { get; private set; }
        public DelegateCommand<Interfaces.IMylist> RemoveMylistGroupCommand { get; private set; }
        public DelegateCommand<Interfaces.IMylist> EditMylistGroupCommand { get; private set; }
        public DelegateCommand<Interfaces.IMylist> PlayAllCommand { get; private set; }

        public DelegateCommand AddLocalMylistCommand { get; private set; }


        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            var userId = parameters.GetValue<string>("id");

            UserId = userId;

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


        protected override IIncrementalSource<Interfaces.IMylist> GenerateIncrementalSource()
        {
            if (!IsLoginUserMylist.Value && UserId != null)
            {
                return new OtherUserMylistIncrementalLoadingSource(UserId, OtherOwneredMylistManager);
            }
            else
            {
                if (NiconicoSession.IsLoggedIn)
                {
                    var items =
                        Enumerable.Concat(
                            LocalMylistManager.Mylists.Cast<Interfaces.IMylist>(),
                            UserMylistManager.Mylists ?? Enumerable.Empty<Interfaces.IMylist>()
                        )
                        .ToList();
                    return new ImmidiateIncrementalLoadingCollectionSource<Interfaces.IMylist>(items);
                }
                else
                {
                    return new ImmidiateIncrementalLoadingCollectionSource<Interfaces.IMylist>(LocalMylistManager.Mylists);

                }
            }
        }

        protected override bool TryGetHohoemaPin(out HohoemaPin pin)
        {
            pin = new HohoemaPin()
            {
                Label = UserName,
                PageType = HohoemaPageType.UserMylist,
                Parameter = $"id={UserId}"
            };

            return true;
        }
    }

    public sealed class OtherUserMylistIncrementalLoadingSource : HohoemaIncrementalSourceBase<Interfaces.IMylist>
    {
        List<OtherOwneredMylist> OtherUserMylists { get; set; }

        public string UserId { get; }
        public OtherOwneredMylistManager OtherOwneredMylistManager;
        public OtherUserMylistIncrementalLoadingSource(string userId, OtherOwneredMylistManager otherOwneredMylistManager)
        {
            UserId = userId;
            OtherOwneredMylistManager = otherOwneredMylistManager;
        }

        protected override Task<IAsyncEnumerable<Interfaces.IMylist>> GetPagedItemsImpl(int head, int count)
        {
            return Task.FromResult(OtherUserMylists.Skip(head).Take(count).Cast<Interfaces.IMylist>().ToAsyncEnumerable());
        }

        protected override async Task<int> ResetSourceImpl()
        {
            try
            {
                OtherUserMylists = await OtherOwneredMylistManager.GetByUserId(UserId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return OtherUserMylists?.Count ?? 0;
        }
    }



    public class MylistGroupListItem : HohoemaListingPageItemBase, Interfaces.IMylistItem
	{
        public MylistGroupListItem(Interfaces.IMylist list)
        {
            GroupId = list.Id;

            Label = list.Label;
        }


        public MylistGroupListItem(UserOwnedMylist info)
		{
			GroupId = info.GroupId;

			Update(info);
        }

		public MylistGroupListItem(MylistGroupData mylistGroup)
		{
			Label = mylistGroup.Name;
			Description = mylistGroup.Description;
			GroupId = mylistGroup.Id;
            OptionText = (mylistGroup.GetIsPublic() ? "公開" : "非公開") + $" - {mylistGroup.Count}件";

            ThemeColor = mylistGroup.GetIconType().ToColor();
            ItemCount = (uint)mylistGroup.Count;

            if (mylistGroup.ThumbnailUrls != null)
            {
                foreach (var thumbnailUri in mylistGroup.ThumbnailUrls)
                {
                    AddImageUrl(thumbnailUri.OriginalString);
                }
            }
        }

        public MylistGroupListItem(MylistGroup mylistGroup)
        {
            Label = mylistGroup.Name;
            Description = mylistGroup.Description;
            GroupId = mylistGroup.Id;
            OptionText = ("公開") + $" - {mylistGroup.ItemCount}件";
            ItemCount = mylistGroup.ItemCount;

            foreach (var thumbnailUri in mylistGroup.VideoInfoItems.Take(3).Select(x => x.Video.ThumbnailUrl))
            {
                AddImageUrl(thumbnailUri.OriginalString);
            }
        }

        public MylistGroupListItem(Mylistgroup mylistGroup)
        {
            Label = mylistGroup.Name;
            Description = mylistGroup.Description;
            GroupId = mylistGroup.Id;
            OptionText = ("公開") + $" - {mylistGroup.Count}件";
            ItemCount = mylistGroup.Count;

            foreach (var thumbnailUri in mylistGroup.SampleVideoInfoItems.Select(x => x.Video.ThumbnailUrl))
            {
                AddImageUrl(thumbnailUri.OriginalString);
            }
        }

        public void Update(UserOwnedMylist info)
		{
			Label = info.Label;
			Description = info.Description;
			OptionText = (info.IsPublic ? "公開" : "非公開") + $" - {info.ItemCount}件";

            ThemeColor = info.IconType.ToColor();

            // ユーザーマイリストの情報はそのままではサムネが取れない
            // マイリスト内の動画からサムネを取得する？
        }

		public string GroupId { get; private set; }

        public string Id => GroupId;

        public uint ItemCount { get; private set; }
        public DateTime UpdateTime { get; private set; }
        public List<Mntone.Nico2.Searches.Video.Video> SampleVideos { get; private set; }
	}

}

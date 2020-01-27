using NicoPlayerHohoema.Dialogs;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Repository.Playlist;
using NicoPlayerHohoema.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase.Playlist
{
    public sealed class PlaylistSelectDialogService
    {
        private readonly DialogService _dialogService;
        private readonly UserMylistManager _userMylistManager;
        private readonly LocalMylistManager _localMylistManager;

        public PlaylistSelectDialogService(
            DialogService dialogService,
            UserMylistManager userMylistManager,
            LocalMylistManager localMylistManager
            )
        {
            _dialogService = dialogService;
            _userMylistManager = userMylistManager;
            _localMylistManager = localMylistManager;
        }

        public async Task<Interfaces.IPlaylist> ChoiceMylist(
            params string[] ignoreMylistId
            )
        {
            const string CreateNewContextLabel = @"@create_new";
            var mylists = _userMylistManager.Mylists;
            var localMylists = _localMylistManager.LocalPlaylists;

            List<ISelectableContainer> selectDialogContent;
            if (false)
            {
                selectDialogContent = new List<ISelectableContainer>()
                {
                    new ChoiceFromListSelectableContainer("マイリスト",
                        mylists.Where(x => ignoreMylistId.All(y => x.Id != y))
                            .Select(x => new SelectDialogPayload() { Label = x.Label, Id = x.Id, Context = x })
                    ),
                    new ChoiceFromListSelectableContainer("ローカルマイリスト",
                        localMylists.Where(x => ignoreMylistId.All(y => x.Id != y))
                            .Select(x => new SelectDialogPayload() { Label = x.Label, Id = x.Id, Context = x })
                    ),
                    new ChoiceFromListSelectableContainer("新規作成",
                        new [] {
                            new SelectDialogPayload() { Label = "マイリストを作成", Id = "mylist", Context = CreateNewContextLabel},
                            new SelectDialogPayload() { Label = "ローカルマイリストを作成", Id = "local", Context = CreateNewContextLabel},
                        }
                    )
                };
            }
            else
            {
                selectDialogContent = new List<ISelectableContainer>()
                {
                    new ChoiceFromListSelectableContainer("ローカルマイリスト",
                        localMylists.Where(x => ignoreMylistId.All(y => x.Id != y))
                            .Select(x => new SelectDialogPayload() { Label = x.Label, Id = x.Id, Context = x })
                    ),
                    new ChoiceFromListSelectableContainer("新規作成",
                        new [] {
                            new SelectDialogPayload() { Label = "ローカルマイリストを作成", Id = "local", Context = CreateNewContextLabel},
                        }
                    )
                };

            }

            Interfaces.IPlaylist resultList = null;
            while (resultList == null)
            {
                var result = await _dialogService.ShowContentSelectDialogAsync(
                    "追加先マイリストを選択",
                    selectDialogContent
                    );

                if (result == null) { break; }

                if (result?.Context as string == CreateNewContextLabel)
                {
                    var mylistTypeLabel = result.Id == "mylist" ? "マイリスト" : "ローカルマイリスト";
                    var title = await _dialogService.GetTextAsync(
                        $"{mylistTypeLabel}を作成",
                        $"{mylistTypeLabel}名",
                        validater: (str) => !string.IsNullOrWhiteSpace(str)
                        );
                    if (title == null)
                    {
                        continue;
                    }

                    if (result.Id == "mylist")
                    {
                        await _userMylistManager.AddMylist(title, "", false, Mntone.Nico2.Mylist.MylistDefaultSort.FirstRetrieve_Descending, Mntone.Nico2.Mylist.IconType.Default);
                        resultList = _userMylistManager.Mylists.LastOrDefault(x => x.Label == title);
                    }
                    else //if (result.Id == "local")
                    {
                        resultList = _localMylistManager.CreatePlaylist(title); ;
                    }
                }
                else
                {
                    resultList = result?.Context as Interfaces.IPlaylist;
                }
            }

            return resultList;
        }

    }
}

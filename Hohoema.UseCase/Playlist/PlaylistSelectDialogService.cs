using Hohoema.Models.Repository.Niconico.Mylist;
using Hohoema.UseCase.Services;
using I18NPortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.UseCase.Playlist
{

    // TODO: マイリスト選択用ダイアログを専用で作成する

    public sealed class PlaylistSelectDialogService
    {
//        private readonly dialog _dialogService;
        private readonly UserMylistManager _userMylistManager;
        private readonly LocalMylistManager _localMylistManager;

        public PlaylistSelectDialogService(
            IMultiSelectionDialogService dialogService,
            UserMylistManager userMylistManager,
            LocalMylistManager localMylistManager
            )
        {
//            _dialogService = dialogService;
            _userMylistManager = userMylistManager;
            _localMylistManager = localMylistManager;
        }


        public async Task<IPlaylist> ChoiceMylist(
            params string[] ignoreMylistId
            )
        {
            throw new NotImplementedException();
            /*
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
                    new ChoiceFromListSelectableContainer("LocalPlaylist".Translate(),
                        localMylists.Where(x => ignoreMylistId.All(y => x.Id != y))
                            .Select(x => new SelectDialogPayload() { Label = x.Label, Id = x.Id, Context = x })
                    ),
                    new ChoiceFromListSelectableContainer("CreateNew".Translate(),
                        new [] {
                            new SelectDialogPayload() { Label = "LocalPlaylistCreate".Translate(), Id = "local", Context = CreateNewContextLabel},
                        }
                    )
                };

            }

            Interfaces.IPlaylist resultList = null;
            while (resultList == null)
            {
                var result = await _dialogService.ShowContentSelectDialogAsync(
                    "SelectMylist".Translate(),
                    selectDialogContent
                    );

                if (result == null) { break; }

                if (result?.Context as string == CreateNewContextLabel)
                {
                    if (result.Id == "mylist")
                    {
                        var title = await _dialogService.GetTextAsync(
                        $"MylistCreate".Translate(),
                        $"MylistNameTextBoxPlacefolder".Translate(),
                        validater: (str) => !string.IsNullOrWhiteSpace(str)
                        );
                        await _userMylistManager.AddMylist(title, "", false, Mntone.Nico2.Mylist.MylistDefaultSort.FirstRetrieve_Descending, Mntone.Nico2.Mylist.IconType.Default);
                        resultList = _userMylistManager.Mylists.LastOrDefault(x => x.Label == title);
                    }
                    else //if (result.Id == "local")
                    {
                        var title = await _dialogService.GetTextAsync(
                        $"LocalPlaylistCreate".Translate(),
                        $"LocalPlaylistNameTextBoxPlacefolder".Translate(),
                        validater: (str) => !string.IsNullOrWhiteSpace(str)
                        );
                        resultList = _localMylistManager.CreatePlaylist(title); ;
                    }
                }
                else
                {
                    resultList = result?.Context as IPlaylist;
                }
            }

            return resultList;
            */
        }

    }
}

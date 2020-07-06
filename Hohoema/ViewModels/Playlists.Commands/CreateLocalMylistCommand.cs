﻿using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using System.Diagnostics;
using Hohoema.Models.LocalMylist;
using Hohoema.Services;
using Hohoema.UseCase.Playlist;
using I18NPortable;
using Hohoema.UseCase.Services;
using Hohoema.Models.Repository;

namespace Hohoema.Commands.Mylist
{
    public sealed class CreateLocalMylistCommand : DelegateCommandBase
    {
        public CreateLocalMylistCommand(
            LocalMylistManager localMylistManager,
            ITextInputDialogService dialogService
            )
        {
            LocalMylistManager = localMylistManager;
            DialogService = dialogService;
        }

        public LocalMylistManager LocalMylistManager { get; }
        public ITextInputDialogService DialogService { get; }

        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override async void Execute(object parameter)
        {
            var result = await DialogService.GetTextAsync(
                "LocalPlaylistCreate".Translate(),
                "LocalPlaylistNameTextBoxPlacefolder".Translate(), 
                "", 
                (s) => !string.IsNullOrWhiteSpace(s)
                );
            if (result != null)
            {
                var localMylist = new Models.LocalMylist.LocalMylistGroup(Guid.NewGuid().ToString(), result);
                var localPlaylist = LocalMylistManager.CreatePlaylist(result);

                Debug.WriteLine("ローカルマイリスト作成：" + result);

                if (parameter is IVideoContent content)
                {
                    localPlaylist.AddPlaylistItem(content);
                }
                else if (parameter is string itemId)
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}

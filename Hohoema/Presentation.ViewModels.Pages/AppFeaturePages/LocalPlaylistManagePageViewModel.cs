using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Presentation.Services.Page;
using Hohoema.Presentation.ViewModels.NicoVideos.Commands;
using Microsoft.Toolkit.Uwp.UI;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Pages.AppFeaturePages
{
    public sealed class LocalPlaylistManagePageViewModel : HohoemaViewModelBase
    {
        private readonly PageManager _pageManager;
        private readonly LocalMylistManager _localMylistManager;

        public AdvancedCollectionView ItemsView { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public LocalPlaylistCreateCommand CreateLocalMylistCommand { get; }
        public LocalPlaylistDeleteCommand DeleteLocalPlaylistCommand { get; }
        public ReactiveCommand<IPlaylist> OpenMylistCommand { get; }


        public LocalPlaylistManagePageViewModel(
            PageManager pageManager,
            Services.DialogService dialogService,
            ApplicationLayoutManager applicationLayoutManager,
            LocalMylistManager localMylistManager,
            LocalPlaylistCreateCommand localPlaylistCreateCommand,
            LocalPlaylistDeleteCommand localPlaylistDeleteCommand
            )
        {
            _pageManager = pageManager;
            ApplicationLayoutManager = applicationLayoutManager;
            _localMylistManager = localMylistManager;
            CreateLocalMylistCommand = localPlaylistCreateCommand;
            DeleteLocalPlaylistCommand = localPlaylistDeleteCommand;
            ItemsView = new AdvancedCollectionView(_localMylistManager.LocalPlaylists);

            OpenMylistCommand = new ReactiveCommand<IPlaylist>()
                .AddTo(_CompositeDisposable);

            OpenMylistCommand.Subscribe(listItem =>
            {
                _pageManager.OpenPageWithId(HohoemaPageType.LocalPlaylist, listItem.Id);
            });

        }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);
        }
    }
}

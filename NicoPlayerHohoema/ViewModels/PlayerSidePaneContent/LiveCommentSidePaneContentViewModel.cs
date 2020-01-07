using Mntone.Nico2.Videos.Comment;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Views;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.PlayerSidePaneContent
{
	public class LiveCommentSidePaneContentViewModel : SidePaneContentViewModelBase
	{
		public LiveCommentSidePaneContentViewModel(
            NGSettings settings, 
            Microsoft.Toolkit.Uwp.UI.AdvancedCollectionView comments,
            Services.ExternalAccessService externalAccessService,
            Commands.NicoLiveUserIdAddToNGCommand nicoLiveUserIdAddToNGCommand,
            Commands.NicoLiveUserIdRemoveFromNGCommand nicoLiveUserIdRemoveFromNGCommand,
            IScheduler scheduler
            )
		{
            NGSettings = settings;
			Comments = comments;
            ExternalAccessService = externalAccessService;
            NicoLiveUserIdAddToNGCommand = nicoLiveUserIdAddToNGCommand;
            NicoLiveUserIdRemoveFromNGCommand = nicoLiveUserIdRemoveFromNGCommand;
            _scheduler = scheduler;
            IsCommentListScrollWithVideo = new ReactiveProperty<bool>(_scheduler, false)
				.AddTo(_CompositeDisposable);

            NGUsers = new ReadOnlyObservableCollection<NGUserIdInfo>(NGSettings.NGLiveCommentUserIds);
            IsNGCommentUserIdEnabled = NGSettings.ToReactivePropertyAsSynchronized(x => x.IsNGLiveCommentUserEnable, _scheduler)
                .AddTo(_CompositeDisposable);
        }


		public void UpdatePlayPosition(uint videoPosition)
		{
			if (IsCommentListScrollWithVideo.Value)
			{
				// 表示位置の更新
			}
		}

		public ReactiveProperty<bool> IsCommentListScrollWithVideo { get; private set; }

		public NGSettings NGSettings { get; private set; }
        public ReactiveProperty<bool> IsNGCommentUserIdEnabled { get; private set; }


        public ReadOnlyObservableCollection<Models.NGUserIdInfo> NGUsers { get; }

        /*
        ReadOnlyObservableCollection<Comment> _Comments;

        public ReadOnlyObservableCollection<Comment> Comments
        {
            get { return _Comments; }
            set { SetProperty(ref _Comments, value); }
        }
        */


        Microsoft.Toolkit.Uwp.UI.AdvancedCollectionView _Comments;
        private readonly IScheduler _scheduler;

        public Microsoft.Toolkit.Uwp.UI.AdvancedCollectionView Comments
        {
            get { return _Comments; }
            set { SetProperty(ref _Comments, value); }
        }

        public Services.ExternalAccessService ExternalAccessService { get; }
        public Commands.NicoLiveUserIdAddToNGCommand NicoLiveUserIdAddToNGCommand { get; }
        public Commands.NicoLiveUserIdRemoveFromNGCommand NicoLiveUserIdRemoveFromNGCommand { get; }
    }
}

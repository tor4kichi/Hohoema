#nullable enable
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Player.Comment;
using Hohoema.Services.Player.Videos;
using Hohoema.ViewModels.Niconico.Live;
using Hohoema.ViewModels.Niconico.Share;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;

namespace Hohoema.ViewModels.Player.PlayerSidePaneContent;

public sealed class LiveCommentsSidePaneContentViewModel : SidePaneContentViewModelBase
	{
		public LiveCommentsSidePaneContentViewModel(
        CommentFilteringFacade commentFiltering, 
        IScheduler scheduler,
        NicoVideoOwnerCacheRepository nicoVideoOwnerRepository,
        OpenLinkCommand openLinkCommand,
        CopyToClipboardCommand copyToClipboardCommand
        )
		{
        _playerSettings = commentFiltering;
        _scheduler = scheduler;
        _nicoVideoOwnerRepository = nicoVideoOwnerRepository;
        OpenLinkCommand = openLinkCommand;
        CopyToClipboardCommand = copyToClipboardCommand;
        NicoLiveUserIdAddToNGCommand = new NicoLiveUserIdAddToNGCommand(_playerSettings, _nicoVideoOwnerRepository);
        NicoLiveUserIdRemoveFromNGCommand = new NicoLiveUserIdRemoveFromNGCommand(_playerSettings);
        IsCommentListScrollWithVideo = new ReactiveProperty<bool>(_scheduler, false)
				.AddTo(_CompositeDisposable);

        NGUsers = new ObservableCollection<CommentFliteringRepository.FilteringCommentOwnerId>(_playerSettings.GetFilteringCommentOwnerIdList());
        IsNGCommentUserIdEnabled = _playerSettings.ToReactivePropertyAsSynchronized(x => x.IsEnableFilteringCommentOwnerId, _scheduler)
            .AddTo(_CompositeDisposable);
    }

    public override void Dispose()
    {
        IsCommentListScrollWithVideo?.Dispose();
        IsNGCommentUserIdEnabled?.Dispose();
        base.Dispose();
    }

    public void UpdatePlayPosition(uint videoPosition)
		{
			if (IsCommentListScrollWithVideo.Value)
			{
				// 表示位置の更新
			}
		}

		public ReactiveProperty<bool> IsCommentListScrollWithVideo { get; private set; }

    public ReactiveProperty<bool> IsNGCommentUserIdEnabled { get; private set; }


    public ObservableCollection<CommentFliteringRepository.FilteringCommentOwnerId> NGUsers { get; }

    /*
    ReadOnlyObservableCollection<Comment> _Comments;

    public ReadOnlyObservableCollection<Comment> Comments
    {
        get { return _Comments; }
        set { SetProperty(ref _Comments, value); }
    }
    */


    Microsoft.Toolkit.Uwp.UI.AdvancedCollectionView _Comments;
    private readonly CommentFilteringFacade _playerSettings;
    private readonly IScheduler _scheduler;
    private readonly NicoVideoOwnerCacheRepository _nicoVideoOwnerRepository;
    
    public Microsoft.Toolkit.Uwp.UI.AdvancedCollectionView Comments
    {
        get { return _Comments; }
        set { SetProperty(ref _Comments, value); }
    }

    public OpenLinkCommand OpenLinkCommand { get; }
    public CopyToClipboardCommand CopyToClipboardCommand { get; }
    public NicoLiveUserIdAddToNGCommand NicoLiveUserIdAddToNGCommand { get; }
    public NicoLiveUserIdRemoveFromNGCommand NicoLiveUserIdRemoveFromNGCommand { get; }
}

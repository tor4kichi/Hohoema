#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Player;
using Hohoema.Models.Player.Comment;
using Hohoema.Services.Player.Videos;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Hohoema.ViewModels.Player.PlayerSidePaneContent;

public class SettingsSidePaneContentViewModel : SidePaneContentViewModelBase
{
    public SettingsSidePaneContentViewModel(
        VideoFilteringSettings videoFilteringRepository, 
        PlayerSettings playerSettings,
        CommentFilteringFacade commentFiltering,
        MediaPlayerSoundVolumeManager soundVolumeManager,
        IScheduler scheduler
        )
    {
        _videoFilteringRepository = videoFilteringRepository;
        PlayerSettings = playerSettings;
        CommentFiltering = commentFiltering;
        SoundVolumeManager = soundVolumeManager;
        _scheduler = scheduler;


        FilteringKeywords = new ObservableCollection<CommentFliteringRepository.FilteringCommentTextKeyword>(CommentFiltering.GetAllFilteringCommentTextCondition());
        Observable.FromEventPattern<CommentFilteringFacade.FilteringCommentTextKeywordEventArgs>(
            h => CommentFiltering.FilterKeywordAdded += h,
            h => CommentFiltering.FilterKeywordAdded -= h
            )
            .Subscribe(args => 
            {
                FilteringKeywords.Add(args.EventArgs.FilterKeyword);
            })
            .AddTo(_CompositeDisposable);

        Observable.FromEventPattern<CommentFilteringFacade.FilteringCommentTextKeywordEventArgs>(
            h => CommentFiltering.FilterKeywordRemoved += h,
            h => CommentFiltering.FilterKeywordRemoved -= h
            )
            .Subscribe(args =>
            {
                FilteringKeywords.Remove(args.EventArgs.FilterKeyword);
            })
            .AddTo(_CompositeDisposable);

        // 
        VideoCommentTransformConditions = new ObservableCollection<CommentFliteringRepository.CommentTextTransformCondition>(CommentFiltering.GetTextTranformConditions());
        Observable.FromEventPattern<CommentFilteringFacade.CommentTextTranformConditionChangedArgs>(
            h => CommentFiltering.TransformConditionAdded += h,
            h => CommentFiltering.TransformConditionAdded -= h
            )
            .Subscribe(args =>
            {
                VideoCommentTransformConditions.Add(args.EventArgs.TransformCondition);
            })
            .AddTo(_CompositeDisposable);

        Observable.FromEventPattern<CommentFilteringFacade.CommentTextTranformConditionChangedArgs>(
            h => CommentFiltering.TransformConditionRemoved += h,
            h => CommentFiltering.TransformConditionRemoved -= h
            )
            .Subscribe(args =>
            {
                VideoCommentTransformConditions.Remove(args.EventArgs.TransformCondition);
            })
            .AddTo(_CompositeDisposable);
    }

    private void CommentFiltering_FilterKeywordAdded(object sender, CommentFilteringFacade.FilteringCommentTextKeywordEventArgs e)
    {
        throw new NotImplementedException();
    }

    public ObservableCollection<CommentFliteringRepository.FilteringCommentTextKeyword> FilteringKeywords { get; }
    public ObservableCollection<CommentFliteringRepository.CommentTextTransformCondition> VideoCommentTransformConditions { get; }


    public PlayerSettings PlayerSettings { get; }

    public CommentFilteringFacade CommentFiltering { get; }
    public MediaPlayerSoundVolumeManager SoundVolumeManager { get; }

    private readonly VideoFilteringSettings _videoFilteringRepository;
    private readonly IScheduler _scheduler;
    
    private void OnRemoveNGCommentUserIdFromList(string userId)
    {
        CommentFiltering.RemoveFilteringCommentOwnerId(userId);
    }

}


public class ValueWithAvairability<T> : ObservableObject
{
    public ValueWithAvairability(T value, bool isAvairable = true)
    {
        Value = value;
        IsAvairable = isAvairable;
    }
    public T Value { get; set; }

    private bool _IsAvairable;
    public bool IsAvairable
    {
        get { return _IsAvairable; }
        set { SetProperty(ref _IsAvairable, value); }
    }
}

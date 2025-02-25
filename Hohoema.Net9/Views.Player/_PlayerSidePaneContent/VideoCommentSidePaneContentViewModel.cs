#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hohoema.Models.Player.Video.Comment;
using Hohoema.Services;
using Hohoema.Services.Player.Videos;
using Microsoft.Toolkit.Uwp.UI;
using Reactive.Bindings.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Hohoema.ViewModels.Player.PlayerSidePaneContent;

public sealed class VideoCommentDefaultOrderComparer : IComparer, IComparer<IVideoComment>
{
    public int Compare(IVideoComment x, IVideoComment y)
    {
        return TimeSpan.Compare(x.VideoPosition, y.VideoPosition);
    }

    public int Compare(object x, object y)
    {
        return Compare(x as IVideoComment, y as IVideoComment);
    }
}

public sealed class VideoCommentSidePaneContentViewModel : ObservableObject, IDisposable
{
    public CommentFilteringFacade CommentFiltering { get; }

    public VideoCommentSidePaneContentViewModel(
        VideoCommentPlayer commentPlayer,
        CommentFilteringFacade  commentFiltering,
        Services.DialogService dialogService
        )
    {
        CommentPlayer = commentPlayer;
        CommentFiltering = commentFiltering;
        _dialogService = dialogService;
        Comments = new AdvancedCollectionView(CommentPlayer.DisplayingComments, false);

        HandleCommentFilterConditionChanged();

        void HandleCommentFilterConditionChanged()
        {
#pragma warning disable IDISP004 // Don't ignore created IDisposable.
            new[]
            {
            Observable.FromEventPattern<CommentFilteringFacade.CommentOwnerIdFilteredEventArgs>(
                h => CommentFiltering.FilteringCommentOwnerIdAdded += h,
                h => CommentFiltering.FilteringCommentOwnerIdAdded -= h
                ).ToUnit(),
            Observable.FromEventPattern<CommentFilteringFacade.CommentOwnerIdFilteredEventArgs>(
                h => CommentFiltering.FilteringCommentOwnerIdRemoved += h,
                h => CommentFiltering.FilteringCommentOwnerIdRemoved -= h
                ).ToUnit(),

            Observable.FromEventPattern<CommentFilteringFacade.FilteringCommentTextKeywordEventArgs>(
                h => CommentFiltering.FilterKeywordAdded += h,
                h => CommentFiltering.FilterKeywordAdded -= h
                ).ToUnit(),
            Observable.FromEventPattern<CommentFilteringFacade.FilteringCommentTextKeywordEventArgs>(
                h => CommentFiltering.FilterKeywordUpdated += h,
                h => CommentFiltering.FilterKeywordUpdated -= h
                ).ToUnit(),
            Observable.FromEventPattern<CommentFilteringFacade.FilteringCommentTextKeywordEventArgs>(
                h => CommentFiltering.FilterKeywordRemoved += h,
                h => CommentFiltering.FilterKeywordRemoved -= h
                ).ToUnit(),
            }   
            .Merge()
            .Subscribe(_ => Comments.RefreshFilter())
#pragma warning restore IDISP004 // Don't ignore created IDisposable.
            .AddTo(_disposables);

            using (Comments.DeferRefresh())
            {
                Comments.SortDescriptions.Add(new SortDescription(SortDirection.Ascending, new VideoCommentDefaultOrderComparer()));
                Comments.Filter = (c) => !isCommentFiltered(c as IVideoComment);
            }
        }
    }


    public VideoCommentPlayer CommentPlayer { get; }
    public AdvancedCollectionView Comments { get; }
    private CompositeDisposable _disposables = new CompositeDisposable();
    private readonly DialogService _dialogService;


    private RelayCommand _ClearFilteringCommentUserIdOnCurrentVideoCommand;
    public RelayCommand ClearFilteringCommentUserIdOnCurrentVideoCommand => _ClearFilteringCommentUserIdOnCurrentVideoCommand
        ?? (_ClearFilteringCommentUserIdOnCurrentVideoCommand = new RelayCommand(() => 
        {
            var currentVideoComments = CommentPlayer.Comments.ToArray().Select(x => x.UserId).Distinct();

            foreach (var commentUserId in currentVideoComments)
            {
                CommentFiltering.RemoveFilteringCommentOwnerId(commentUserId);
            }
        }));

    

    bool isCommentFiltered(IVideoComment comment)
    {
        if (CommentFiltering.IsHiddenCommentOwnerUserId(comment.UserId)) { return true; }
        if (CommentFiltering.GetAllFilteringCommentTextCondition().IsMatchAny(comment.CommentText)) { return true; }
        if (CommentFiltering.IsHiddenShareNGScore(comment.NGScore)) { return true; }

        return false;
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}

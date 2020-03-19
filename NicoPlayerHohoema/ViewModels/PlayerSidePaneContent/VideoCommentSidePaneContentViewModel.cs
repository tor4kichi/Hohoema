using Microsoft.Toolkit.Uwp.UI;
using NicoPlayerHohoema.Models.Niconico;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.UseCase;
using NicoPlayerHohoema.UseCase.NicoVideoPlayer;
using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.PlayerSidePaneContent
{
    public sealed class VideoCommentSizePaneContentViewModel : BindableBase, IDisposable
    {
        public CommentFiltering CommentFiltering { get; }

        public VideoCommentSizePaneContentViewModel(
            UseCase.CommentPlayer commentPlayer,
            CommentFiltering  commentFiltering,
            Services.DialogService dialogService
            )
        {
            CommentPlayer = commentPlayer;
            CommentFiltering = commentFiltering;
            _dialogService = dialogService;
            Comments = new AdvancedCollectionView(CommentPlayer.Comments, true);

            HandleCommentFilterConditionChanged();

            void HandleCommentFilterConditionChanged()
            {
                new[]
                {
                Observable.FromEventPattern<CommentFiltering.CommentOwnerIdFilteredEventArgs>(
                    h => CommentFiltering.FilteringCommentOwnerIdAdded += h,
                    h => CommentFiltering.FilteringCommentOwnerIdAdded -= h
                    ).ToUnit(),
                Observable.FromEventPattern<CommentFiltering.CommentOwnerIdFilteredEventArgs>(
                    h => CommentFiltering.FilteringCommentOwnerIdRemoved += h,
                    h => CommentFiltering.FilteringCommentOwnerIdRemoved -= h
                    ).ToUnit(),

                Observable.FromEventPattern<CommentFiltering.FilteringCommentTextKeywordEventArgs>(
                    h => CommentFiltering.FilterKeywordAdded += h,
                    h => CommentFiltering.FilterKeywordAdded -= h
                    ).ToUnit(),
                Observable.FromEventPattern<CommentFiltering.FilteringCommentTextKeywordEventArgs>(
                    h => CommentFiltering.FilterKeywordUpdated += h,
                    h => CommentFiltering.FilterKeywordUpdated -= h
                    ).ToUnit(),
                Observable.FromEventPattern<CommentFiltering.FilteringCommentTextKeywordEventArgs>(
                    h => CommentFiltering.FilterKeywordRemoved += h,
                    h => CommentFiltering.FilterKeywordRemoved -= h
                    ).ToUnit(),
                }   
                .Merge()
                .Subscribe(_ => Comments.RefreshFilter())
                .AddTo(_disposables);

                using (Comments.DeferRefresh())
                {
                    Comments.SortDescriptions.Add(new SortDescription("VideoPosition", SortDirection.Ascending));
                    Comments.Filter = (c) => !isCommentFiltered(c as Comment);
                }
            }
        }


        public CommentPlayer CommentPlayer { get; }
        public AdvancedCollectionView Comments { get; }
        private CompositeDisposable _disposables = new CompositeDisposable();
        private readonly DialogService _dialogService;


        private DelegateCommand _ClearFilteringCommentUserIdOnCurrentVideoCommand;
        public DelegateCommand ClearFilteringCommentUserIdOnCurrentVideoCommand => _ClearFilteringCommentUserIdOnCurrentVideoCommand
            ?? (_ClearFilteringCommentUserIdOnCurrentVideoCommand = new DelegateCommand(() => 
            {
                var currentVideoComments = CommentPlayer.Comments.ToArray().Select(x => x.UserId).Distinct();

                foreach (var commentUserId in currentVideoComments)
                {
                    CommentFiltering.RemoveFilteringCommentOwnerId(commentUserId);
                }
            }));

        

        bool isCommentFiltered(Comment comment)
        {
            if (CommentFiltering.IsCommentOwnerUserIdFiltered(comment.UserId)) { return true; }
            if (CommentFiltering.GetAllFilteringCommentTextCondition().IsMatchAny(comment.CommentText)) { return true; }
            if (CommentFiltering.IsShareNGScoreFilterd(comment.NGScore)) { return true; }

            return false;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}

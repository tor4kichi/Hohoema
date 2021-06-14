using Microsoft.Toolkit.Uwp.UI;
using Hohoema.Models.Domain.Niconico;
using Hohoema.Presentation.Services;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.NicoVideos.Player;
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
using Hohoema.Models.Domain.Player.Video.Comment;

namespace Hohoema.Presentation.ViewModels.Player.PlayerSidePaneContent
{
    public sealed class VideoCommentSidePaneContentViewModel : BindableBase, IDisposable
    {
        public CommentFilteringFacade CommentFiltering { get; }

        public VideoCommentSidePaneContentViewModel(
            CommentPlayer commentPlayer,
            CommentFilteringFacade  commentFiltering,
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
                    Comments.SortDescriptions.Add(new SortDescription("VideoPosition", SortDirection.Ascending));
                    Comments.Filter = (c) => !isCommentFiltered(c as VideoComment);
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

        

        bool isCommentFiltered(VideoComment comment)
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
}

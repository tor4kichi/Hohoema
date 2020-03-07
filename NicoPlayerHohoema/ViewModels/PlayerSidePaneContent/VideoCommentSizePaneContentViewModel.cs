using Microsoft.Toolkit.Uwp.UI;
using NicoPlayerHohoema.Models.Niconico;
using NicoPlayerHohoema.UseCase;
using NicoPlayerHohoema.UseCase.NicoVideoPlayer;
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
        private readonly CommentFiltering _commentFiltering;

        public VideoCommentSizePaneContentViewModel(
            UseCase.CommentPlayer commentPlayer,
            CommentFiltering  commentFiltering
            )
        {
            CommentPlayer = commentPlayer;
            _commentFiltering = commentFiltering;

            Comments = new AdvancedCollectionView(CommentPlayer.Comments);

            HandleCommentFilterConditionChanged();

            void HandleCommentFilterConditionChanged()
            {
                new[]
                {
                Observable.FromEventPattern<CommentFiltering.CommentOwnerIdFilteredEventArgs>(
                    h => _commentFiltering.FilteringCommentOwnerIdAdded += h,
                    h => _commentFiltering.FilteringCommentOwnerIdAdded -= h
                    ).ToUnit(),
                Observable.FromEventPattern<CommentFiltering.CommentOwnerIdFilteredEventArgs>(
                    h => _commentFiltering.FilteringCommentOwnerIdRemoved += h,
                    h => _commentFiltering.FilteringCommentOwnerIdRemoved -= h
                    ).ToUnit(),

                Observable.FromEventPattern<CommentFiltering.FilteringCommentTextKeywordEventArgs>(
                    h => _commentFiltering.FilterKeywordAdded += h,
                    h => _commentFiltering.FilterKeywordAdded -= h
                    ).ToUnit(),
                Observable.FromEventPattern<CommentFiltering.FilteringCommentTextKeywordEventArgs>(
                    h => _commentFiltering.FilterKeywordUpdated += h,
                    h => _commentFiltering.FilterKeywordUpdated -= h
                    ).ToUnit(),
                Observable.FromEventPattern<CommentFiltering.FilteringCommentTextKeywordEventArgs>(
                    h => _commentFiltering.FilterKeywordRemoved += h,
                    h => _commentFiltering.FilterKeywordRemoved -= h
                    ).ToUnit(),
                }   
                .Merge()
                .Subscribe(_ => Comments.RefreshFilter())
                .AddTo(_disposables);

                Comments.Filter = (c) => isCommentFiltered(c as Comment);
            }
        }


        public CommentPlayer CommentPlayer { get; }
        public AdvancedCollectionView Comments { get; }
        private CompositeDisposable _disposables = new CompositeDisposable();

        

        bool isCommentFiltered(Comment comment)
        {
            if (_commentFiltering.IsCommentOwnerUserIdFiltered(comment.UserId)) { return true; }
            if (_commentFiltering.GetAllFilteringCommentTextCondition().IsMatchAny(comment.CommentText)) { return true; }
            if (_commentFiltering.IsShareNGScoreFilterd(comment.NGScore)) { return true; }

            return false;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}

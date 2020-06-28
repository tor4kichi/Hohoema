using Hohoema.Models;
using Hohoema.UseCase.NicoVideoPlayer;
using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI;

namespace Hohoema.ViewModels.PlayerSidePaneContent
{
    public class SettingsSidePaneContentViewModel : SidePaneContentViewModelBase
    {
        public SettingsSidePaneContentViewModel(
            NGSettings ngSettings, 
            PlayerSettings playerSettings,
            CommentFiltering commentFiltering,
            IScheduler scheduler
            )
        {
            PlayerSettings = playerSettings;
            CommentFiltering = commentFiltering;
            _scheduler = scheduler;


            FilteringKeywords = new ObservableCollection<Repository.CommentFliteringRepository.FilteringCommentTextKeyword>(CommentFiltering.GetAllFilteringCommentTextCondition());
            Observable.FromEventPattern<CommentFiltering.FilteringCommentTextKeywordEventArgs>(
                h => CommentFiltering.FilterKeywordAdded += h,
                h => CommentFiltering.FilterKeywordAdded -= h
                )
                .Subscribe(args => 
                {
                    FilteringKeywords.Add(args.EventArgs.FilterKeyword);
                })
                .AddTo(_CompositeDisposable);

            Observable.FromEventPattern<CommentFiltering.FilteringCommentTextKeywordEventArgs>(
                h => CommentFiltering.FilterKeywordRemoved += h,
                h => CommentFiltering.FilterKeywordRemoved -= h
                )
                .Subscribe(args =>
                {
                    FilteringKeywords.Remove(args.EventArgs.FilterKeyword);
                })
                .AddTo(_CompositeDisposable);

            // 
            VideoCommentTransformConditions = new ObservableCollection<Repository.CommentFliteringRepository.CommentTextTransformCondition>(CommentFiltering.GetTextTranformConditions());
            Observable.FromEventPattern<CommentFiltering.CommentTextTranformConditionChangedArgs>(
                h => CommentFiltering.TransformConditionAdded += h,
                h => CommentFiltering.TransformConditionAdded -= h
                )
                .Subscribe(args =>
                {
                    VideoCommentTransformConditions.Add(args.EventArgs.TransformCondition);
                })
                .AddTo(_CompositeDisposable);

            Observable.FromEventPattern<CommentFiltering.CommentTextTranformConditionChangedArgs>(
                h => CommentFiltering.TransformConditionRemoved += h,
                h => CommentFiltering.TransformConditionRemoved -= h
                )
                .Subscribe(args =>
                {
                    VideoCommentTransformConditions.Remove(args.EventArgs.TransformCondition);
                })
                .AddTo(_CompositeDisposable);
        }

        private void CommentFiltering_FilterKeywordAdded(object sender, CommentFiltering.FilteringCommentTextKeywordEventArgs e)
        {
            throw new NotImplementedException();
        }

        public ObservableCollection<Repository.CommentFliteringRepository.FilteringCommentTextKeyword> FilteringKeywords { get; }
        public ObservableCollection<Repository.CommentFliteringRepository.CommentTextTransformCondition> VideoCommentTransformConditions { get; }


        public PlayerSettings PlayerSettings { get; }

        public CommentFiltering CommentFiltering { get; }
        private readonly IScheduler _scheduler;
        
        protected override void OnDispose()
        {
            base.OnDispose();
        }

        private void OnRemoveNGCommentUserIdFromList(string userId)
        {
            var removeTarget = PlayerSettings.NGCommentUserIds.First(x => x.UserId == userId);
            PlayerSettings.NGCommentUserIds.Remove(removeTarget);
        }

    }


    public class ValueWithAvairability<T> : BindableBase
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

}

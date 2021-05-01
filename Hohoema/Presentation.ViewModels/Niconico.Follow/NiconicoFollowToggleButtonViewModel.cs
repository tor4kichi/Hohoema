using Hohoema.Models.Domain.Niconico.Follow;
using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Hohoema.Presentation.Services;
using I18NPortable;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public sealed class NiconicoFollowToggleButtonViewModel : Prism.Mvvm.BindableBase, IDisposable
    {
        public NiconicoFollowToggleButtonViewModel(
            IScheduler scheduler,
            FollowManager followManager,
            DialogService dialogService
            )
        {
            FollowManager = followManager;
            DialogService = dialogService;

            _followTarget = new ReactiveProperty<IFollowable>(scheduler, null).AddTo(disposables);
            _nowProcessFollow = new ReactiveProperty<bool>(scheduler, false).AddTo(disposables);
            _isFollowTarget = new ReactiveProperty<bool>(scheduler, false).AddTo(disposables);

            CanToggleFollow = 
                new []
                {
                    _followTarget.ToUnit(),
                    _isFollowTarget.ToUnit()
                }
                .CombineLatest()
                .Select(_ => 
                {
                    var isFollow = _isFollowTarget.Value;
                    if (_followTarget.Value == null) { return false; }

                    return isFollow 
                    ? true
                    : FollowManager.CanMoreAddFollow(_followTarget.Value)
                    ;
                })
                .ToReadOnlyReactiveProperty(eventScheduler: scheduler)
                .AddTo(disposables);

            ToggleFollowCommand = 
                new[] 
                {
                    _nowProcessFollow.Select(x => !x),
                    CanToggleFollow
                }
                .CombineLatestValuesAreAllTrue()
                .ToAsyncReactiveCommand()
                .AddTo(disposables);

            ToggleFollowCommand.Subscribe(async () => 
            {
                _nowProcessFollow.Value = true;

                try
                {
                    var followTarget = _followTarget.Value;
                    if (FollowManager.IsFollowItem(followTarget))
                    {
                        if (await ConfirmRemovingFollow())
                        {
                            var result = await FollowManager.RemoveFollow(followTarget);
                        }
                    }
                    else if (FollowManager.CanMoreAddFollow(followTarget))
                    {
                        await FollowManager.AddFollow(followTarget);
                    }

                    _isFollowTarget.Value = FollowManager.IsFollowItem(FollowTarget.Value);
                }
                finally
                {
                    _nowProcessFollow.Value = false;
                }

                // トグルボタンの押したらとりあえず切り替わる仕様に対応するためのコード
                // 現在のフォロー状態に応じたトグル状態を確実化する
                await Task.Delay(500);

                RaisePropertyChanged(nameof(IsFollowTarget));

            })
            .AddTo(disposables);
        }

        private CompositeDisposable disposables = new CompositeDisposable();

        public FollowManager FollowManager { get; }
        public DialogService DialogService { get; }
        private ReactiveProperty<IFollowable> _followTarget { get; }
        public ReactiveProperty<IFollowable> FollowTarget => _followTarget;

        private ReactiveProperty<bool> _nowProcessFollow { get; }
        public IReadOnlyReactiveProperty<bool> NowProcessFollow => _nowProcessFollow;

        private ReactiveProperty<bool> _isFollowTarget { get; }
        public IReadOnlyReactiveProperty<bool> IsFollowTarget => _isFollowTarget;

        public IReadOnlyReactiveProperty<bool> CanToggleFollow { get; }

        public AsyncReactiveCommand ToggleFollowCommand { get; }

        internal void SetFollowTarget(IFollowable followTarget)
        {
            _nowProcessFollow.Value = true;

            try
            {
                _followTarget.Value = followTarget;
                _isFollowTarget.Value = FollowManager.IsFollowItem(followTarget);
            }
            finally
            {
                _nowProcessFollow.Value = false;
                     
            }
        }


        private async Task<bool> ConfirmRemovingFollow()
        {
            var followTarget = FollowTarget.Value;

            // Note: IsFollowTargetを使うと意図した動作にならない
            // これはToggleButtonにTwoWayバインディングを適用してないとチェック状態をVM側から書き戻せない動作に対するものだが
            // その影響でここに来る段階で IsFollowTarget.Value が false になってしまう
            // この問題を回避するため、FollowManagerによるフォロー済みチェックを行う形を取っている
            // if (!IsFollowTarget.Value) { return false; }

            if (!FollowManager.IsFollowItem(followTarget)) { return false; }

            return await DialogService.ShowMessageDialog(
                "",
                "ConfirmRemoveFollow_DialogTitle".Translate(),
                "RemoveFollow".Translate(),
                "Cancel".Translate()
                );
        }

        public void Dispose()
        {
            disposables.Dispose();
        }
    }
}

using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Player.Video.Cache;
using Hohoema.Presentation.Services;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase
{
    public sealed class VideoCacheResumingObserver : IDisposable
    {
        CompositeDisposable _disposables = new CompositeDisposable();
        private readonly IScheduler _scheduler;
        private readonly NiconicoSession _niconicoSession;
        private readonly ScondaryViewPlayerManager _playerViewManager;
        private readonly VideoCacheManager _videoCacheManager;

        public VideoCacheResumingObserver(
            IScheduler scheduler,
            NiconicoSession niconicoSession,
            ScondaryViewPlayerManager playerViewManager,
            VideoCacheManager videoCacheManager
            )
        {
            _scheduler = scheduler;
            _niconicoSession = niconicoSession;
            _playerViewManager = playerViewManager;
            _videoCacheManager = videoCacheManager;

            // 一般会員は再生とキャッシュDLを１ラインしか許容していないため
            // 再生終了時にキャッシュダウンロードの再開を行う必要がある
            // PlayerViewManager.NowPlaying はSecondaryViewでの再生時にFalseを示してしまうため
            // IsPlayerShowWithSecondaryViewを使ってセカンダリビューでの再生中を検出している
            //new[]
            //{
            //    // PlayerViewManager.ObserveProperty(x => x.NowPlaying).Select(x => !x),
            //    _playerViewManager.ObserveProperty(x => x.IsPlayerShowWithPrimaryView).Select(x => !x),
            //    _playerViewManager.ObserveProperty(x => x.IsPlayerShowWithSecondaryView).Select(x => !x),
            //    _niconicoSession.ObserveProperty(x => x.IsPremiumAccount).Select(x => !x)
            //}
            //.CombineLatestValuesAreAllTrue()
            //.Throttle(TimeSpan.FromSeconds(1))
            //.Subscribe(nowResumingCacheDL =>
            //{
            //    _scheduler.Schedule(() =>
            //    {
            //        if (nowResumingCacheDL)
            //        {
            //            _ = _videoCacheManager.ResumeCacheDownload();

            //            // TODO: キャッシュDL再開した場合の通知
            //        }
            //    });
            //})
            //.AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}

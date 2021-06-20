using Hohoema.Models.Helpers;
using Hohoema.Models.Domain.Player.Video;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Hohoema.Models.Domain.VideoCache
{
    public enum VideoCacheDownloadOperationCompleteState
    {
        Completed,
        DownloadPaused,
        ReturnDownloadSessionOwnership,
        DownloadCanceledWithUser,
    }


    public class VideoCacheDownloadOperation : IDisposable, IVideoCacheDownloadOperation
    {
        public string VideoId => VideoCacheItem.VideoId;

        public VideoCacheItem VideoCacheItem { get; }

        private readonly VideoCacheManager _videoCacheManager;
        private readonly DmcVideoStreamingSession _dmcVideoStreamingSession;
        private IVideoCacheDownloadOperationOutput _videoCacheDownloadOperationOutput;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationTokenSource _pauseCancellationTokenSource;
        private CancellationTokenSource _onwerShipReturnedCancellationTokenSource;
        private CancellationTokenSource _linkedCancellationTokenSource;

        TaskCompletionSource<bool> _cancelAwaitTcs;

        public event EventHandler Started;
        public event EventHandler<VideoCacheDownloadOperationProgress> Progress;
        public event EventHandler Completed;

        internal VideoCacheDownloadOperation(VideoCacheManager videoCacheManager, VideoCacheItem videoCacheItem, DmcVideoStreamingSession dmcVideoStreamingSession, IVideoCacheDownloadOperationOutput videoCacheDownloadOperationOutput)
        {
            _videoCacheManager = videoCacheManager;
            VideoCacheItem = videoCacheItem;
            _dmcVideoStreamingSession = dmcVideoStreamingSession;
            _videoCacheDownloadOperationOutput = videoCacheDownloadOperationOutput;
        }

        private void _dmcVideoStreamingSession_StopStreamingFromOwnerShipReturned(object sender, EventArgs e)
        {
            _onwerShipReturnedCancellationTokenSource.Cancel();
        }

        public async Task<VideoCacheDownloadOperationCompleteState> DownloadAsync()
        {
            IRandomAccessStream downloadStream = null;
            try
            {
                var uri = await _dmcVideoStreamingSession.GetDownloadUrlAndSetupDownloadSession();
                downloadStream = await HttpRandomAccessStream.CreateAsync(_dmcVideoStreamingSession.NiconicoSession.ToolkitContext.HttpClient, uri);
            }
            catch
            {
                downloadStream?.Dispose();
                throw;
            }

            Started?.Invoke(this, EventArgs.Empty);

            _cancelAwaitTcs = new TaskCompletionSource<bool>();
            _cancellationTokenSource = new CancellationTokenSource();
            _onwerShipReturnedCancellationTokenSource = new CancellationTokenSource();
            _pauseCancellationTokenSource = new CancellationTokenSource();
            _linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, _onwerShipReturnedCancellationTokenSource.Token, _pauseCancellationTokenSource.Token);

            _dmcVideoStreamingSession.StopStreamingFromOwnerShipReturned += _dmcVideoStreamingSession_StopStreamingFromOwnerShipReturned;

            try
            {
                var ct = _linkedCancellationTokenSource.Token;
                await Task.Run(async () => await _videoCacheDownloadOperationOutput.CopyStreamAsync(downloadStream.AsStreamForRead(), new _Progress(x => Progress?.Invoke(this, x)), ct), ct);
            }
            catch (OperationCanceledException)
            {
                // 削除操作、または視聴権を喪失した場合
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    return VideoCacheDownloadOperationCompleteState.DownloadCanceledWithUser;
                }
                else if (_pauseCancellationTokenSource.IsCancellationRequested)
                {
                    return VideoCacheDownloadOperationCompleteState.DownloadPaused;
                }
                else if (_onwerShipReturnedCancellationTokenSource.IsCancellationRequested)
                {
                    return VideoCacheDownloadOperationCompleteState.ReturnDownloadSessionOwnership;
                }
                else
                {
                    throw;
                }
            }
            catch (FileLoadException)
            {
                throw;
            }
            catch (Exception)
            {
                // ニコ動サーバー側からタイムアウトで切られた場合は一時停止扱い
                return VideoCacheDownloadOperationCompleteState.DownloadPaused;
            }
            finally
            {
                _cancelAwaitTcs.TrySetResult(true);
                _dmcVideoStreamingSession.StopStreamingFromOwnerShipReturned -= _dmcVideoStreamingSession_StopStreamingFromOwnerShipReturned;
                downloadStream.Dispose();
                _onwerShipReturnedCancellationTokenSource.Dispose();
                _pauseCancellationTokenSource.Dispose();
                _cancellationTokenSource.Dispose();
                _linkedCancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
                Completed?.Invoke(this, EventArgs.Empty);
            }

            return VideoCacheDownloadOperationCompleteState.Completed;
        }


        struct _Progress : IProgress<VideoCacheDownloadOperationProgress>
        {
            private readonly Action<VideoCacheDownloadOperationProgress> _act;

            public _Progress(Action<VideoCacheDownloadOperationProgress> act)
            {
                _act = act;
            }

            public void Report(VideoCacheDownloadOperationProgress value)
            {
                _act(value);
            }
        }

        public async Task CancelAsync()
        {
            if (_cancellationTokenSource is not null)
            {
                _cancellationTokenSource.Cancel();
            }

            await _videoCacheDownloadOperationOutput.DeleteAsync();
        }

        void IDisposable.Dispose()
        {
            _dmcVideoStreamingSession.Dispose();
        }

        public Task PauseAsync()
        {
            if (_pauseCancellationTokenSource is not null)
            {
                _pauseCancellationTokenSource.Cancel();
            }

            return _cancelAwaitTcs?.Task ?? Task.CompletedTask;
        }
    }
}

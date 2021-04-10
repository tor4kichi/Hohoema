using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Hohoema.Models.Domain.VideoCache
{
    internal class VideoCacheMigretedFileEncryptOperation : IVideoCacheDownloadOperation, IDisposable
    {
        private CancellationTokenSource _cancellationTokenSource;

        private readonly StorageFile _inputFile;
        
        public VideoCacheItem VideoCacheItem { get; }

        public string VideoId => VideoCacheItem.VideoId;

        public event EventHandler Completed;
        public event EventHandler Paused;
        public event EventHandler<VideoCacheDownloadOperationProgress> Progress;
        public event EventHandler Started;

        private IVideoCacheDownloadOperationOutput _videoCacheDownloadOperationOutput;

        internal VideoCacheMigretedFileEncryptOperation(VideoCacheItem item, StorageFile inputFile, IVideoCacheDownloadOperationOutput videoCacheDownloadOperationOutput)
        {
            VideoCacheItem = item;
            _inputFile = inputFile;
            _videoCacheDownloadOperationOutput = videoCacheDownloadOperationOutput;
        }

        public async Task DownloadAsync()
        {
            Started?.Invoke(this, EventArgs.Empty);

            _cancellationTokenSource = new CancellationTokenSource();
            try
            {
                using (var inputStream = await _inputFile.OpenReadAsync())
                {
                    await _videoCacheDownloadOperationOutput.CopyStreamAsync(inputStream.AsStreamForRead(), new Progress<VideoCacheDownloadOperationProgress>(x => Progress?.Invoke(this, x)), _cancellationTokenSource.Token);
                    Completed?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (OperationCanceledException)
            {
                
            }
            catch (Exception e)
            {
                // ファイルが開けなかった？など
                Paused?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public async Task StopAndDeleteDownloadedAsync()
        {
            if (_cancellationTokenSource is not null)
            {
                _cancellationTokenSource.Cancel();
            }

            await _videoCacheDownloadOperationOutput.DeleteAsync();
        }

        public void Dispose()
        {
        }

        public Task PauseAsync()
        {
            if (_cancellationTokenSource is not null)
            {
                _cancellationTokenSource.Cancel();

                Paused?.Invoke(this, EventArgs.Empty);
            }

            return Task.CompletedTask;
        }
    }
}

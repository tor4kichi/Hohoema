using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using XTSSharp;

namespace Hohoema.Models.Domain.VideoCache
{
    public class VideoCacheDownloadOperationOutputWithEncryption : IVideoCacheDownloadOperationOutput
    {
        private readonly StorageFile _destinationFile;
        private readonly Xts _xts;

        public VideoCacheDownloadOperationOutputWithEncryption(StorageFile destinationFile, XTSSharp.Xts xts)
        {
            _destinationFile = destinationFile;
            _xts = xts;
        }

        public async Task CopyStreamAsync(Stream sourceStream, IProgress<VideoCacheDownloadOperationProgress> progress, CancellationToken cancellationToken)
        {
            using (var outputFileStream = await _destinationFile.OpenStreamForReadAsync())
            {
                // 途中までDLしていた場合はそこから再開
                if (outputFileStream.Length != 0)
                {
                    outputFileStream.Seek(0, SeekOrigin.End);
                    sourceStream.Seek(outputFileStream.Length, SeekOrigin.Begin);
                }

                byte[] inputBuffer = new byte[XtsSectorStream.DEFAULT_SECTOR_SIZE];
                byte[] outputBuffer = new byte[XtsSectorStream.DEFAULT_SECTOR_SIZE];
                using (var encryptor = _xts.CreateEncryptor())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ulong currentSector = (ulong)(outputFileStream.Length / XtsSectorStream.DEFAULT_SECTOR_SIZE);
                    int readLength = -1;
                    while (readLength != 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        readLength = await sourceStream.ReadAsync(inputBuffer, 0, inputBuffer.Length);
                        encryptor.TransformBlock(inputBuffer, 0, inputBuffer.Length, outputBuffer, 0, currentSector);
                        currentSector++;
                        await outputFileStream.WriteAsync(outputBuffer, 0, outputBuffer.Length);
                        await outputFileStream.FlushAsync();

                        progress?.Report(new VideoCacheDownloadOperationProgress() { ProgressBytes = sourceStream.Position, TotalBytes = sourceStream.Length });
                    }
                }
            }
        }

        public Task DeleteAsync()
        {
            return _destinationFile.DeleteAsync().AsTask();
        }
    }
}

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
            using (var outputFileStream = await _destinationFile.OpenStreamForWriteAsync())
            {
                // 途中までDLしていた場合はそこから再開
                if (outputFileStream.Length != 0)
                {
                    var remainder = outputFileStream.Length % XtsSectorStream.DEFAULT_SECTOR_SIZE;
                    if (remainder != 0)
                    {
                        outputFileStream.Seek(remainder, SeekOrigin.End);
                        sourceStream.Seek(outputFileStream.Length - remainder, SeekOrigin.Begin);
                    }
                    else
                    {
                        if (outputFileStream.Length >= XtsSectorStream.DEFAULT_SECTOR_SIZE)
                        {
                            outputFileStream.Seek(-XtsSectorStream.DEFAULT_SECTOR_SIZE, SeekOrigin.End);
                            sourceStream.Seek(outputFileStream.Position, SeekOrigin.Begin);
                        }
                    }
                }

                byte[] inputBuffer = new byte[XtsSectorStream.DEFAULT_SECTOR_SIZE];
                byte[] outputBuffer = new byte[XtsSectorStream.DEFAULT_SECTOR_SIZE];
                using (var encryptor = _xts.CreateEncryptor())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ulong currentSector = (ulong)(outputFileStream.Position / XtsSectorStream.DEFAULT_SECTOR_SIZE);
                    int readLength = -1;
                    try
                    {
                        while (readLength != 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            // Note: 対称暗号のためセクター単位で書き込まないといけない
                            // ReadAsyncで読み取った長さを無視してセクター全体が書き込まれるようにする
                            readLength = await sourceStream.ReadAsync(inputBuffer, 0, inputBuffer.Length);
                            encryptor.TransformBlock(inputBuffer, 0, inputBuffer.Length, outputBuffer, 0, currentSector);
                            currentSector++;
                            await outputFileStream.WriteAsync(outputBuffer, 0, outputBuffer.Length);

                            progress?.Report(new VideoCacheDownloadOperationProgress() { ProgressBytes = sourceStream.Position, TotalBytes = sourceStream.Length });
                        }
                    }
                    finally
                    {
                        await outputFileStream.FlushAsync();
                    }

                    // XTSによる暗号化によってセクター単位でサイズが決まるため
                    // DLしたサイズではなく書き込まれたファイルのサイズを最終的なサイズとして扱う
                    progress?.Report(new VideoCacheDownloadOperationProgress() { ProgressBytes = outputFileStream.Position, TotalBytes = outputFileStream.Length });
                }
            }
        }

        public Task DeleteAsync()
        {
            return _destinationFile.DeleteAsync().AsTask();
        }
    }
}

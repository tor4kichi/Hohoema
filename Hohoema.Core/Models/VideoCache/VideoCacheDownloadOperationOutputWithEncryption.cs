using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using XTSSharp;

namespace Hohoema.Models.VideoCache;

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
        using Stream outputFileStream = await _destinationFile.OpenStreamForWriteAsync();
        using XtsSectorStream xtsSectorStream = new(outputFileStream, _xts);
        // 途中までDLしていた場合はそこから再開
        _ = sourceStream.Seek(xtsSectorStream.Length, SeekOrigin.Begin);

        byte[] inputBuffer = new byte[XtsSectorStream.DEFAULT_SECTOR_SIZE];
        cancellationToken.ThrowIfCancellationRequested();

        int readLength = -1;
        while (readLength != 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Note: 対称暗号のためセクター単位で書き込まないといけない
            // ReadAsyncで読み取った長さを無視してセクター全体が書き込まれるようにする

            // sourceStream側の都合上countに指定したbyte数(inputBuffer.Length)通りに読み込まれないケースがある
            // inputBuffer.Lengthに満たない場合は不足分を追加で読み込んで1セクター分を埋める
            readLength = sourceStream.Read(inputBuffer, 0, inputBuffer.Length);
            if (readLength != 0 && readLength != inputBuffer.Length)
            {
                readLength += sourceStream.Read(inputBuffer, readLength, inputBuffer.Length - readLength);
            }

            // 書き込みしてない後部をゼロフィルした上で書き込み
            Array.Clear(inputBuffer, readLength, inputBuffer.Length - readLength);
            xtsSectorStream.Write(inputBuffer, 0, inputBuffer.Length);

            progress?.Report(new VideoCacheDownloadOperationProgress() { ProgressBytes = sourceStream.Position, TotalBytes = sourceStream.Length });
        }

        // XTSによる暗号化によってセクター単位でサイズが決まるため
        // DLしたサイズではなく書き込まれたファイルのサイズを最終的なサイズとして扱う
        progress?.Report(new VideoCacheDownloadOperationProgress() { ProgressBytes = xtsSectorStream.Position, TotalBytes = xtsSectorStream.Length });
    }

    public Task DeleteAsync()
    {
        return _destinationFile.DeleteAsync().AsTask();
    }
}

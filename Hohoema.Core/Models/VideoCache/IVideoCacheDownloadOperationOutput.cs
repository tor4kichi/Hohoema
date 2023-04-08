using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.VideoCache;

public interface IVideoCacheDownloadOperationOutput
{
    Task CopyStreamAsync(Stream inputStream, IProgress<VideoCacheDownloadOperationProgress> progress, CancellationToken cancellationToken);
    Task DeleteAsync();
}

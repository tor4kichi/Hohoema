using LiteDB;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Web.Http;

namespace Hohoema.Models.Application
{
    public class UrlToCachedImageConverter : IValueConverter
    {
        private readonly ThumbnailCacheManager _thumbnailCacheManager;

        public UrlToCachedImageConverter()
        {
            _thumbnailCacheManager =  Ioc.Default.GetService<ThumbnailCacheManager>();
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string url)
            {
                var bitmap = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                _thumbnailCacheManager.ResolveImage(bitmap, url);
                return bitmap;
            }
            else if (value is Uri uri)
            {
                var bitmap = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                _thumbnailCacheManager.ResolveImage(bitmap, uri.OriginalString);
                return bitmap;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class ThumbnailCacheManager
    {
        private readonly ILiteStorage<string> _fileStorage;
        private readonly HttpClient _httpClient;

        public ThumbnailCacheManager(
            LiteDatabase liteDatabase
            )
        {
            _fileStorage = liteDatabase.FileStorage;
#if DEBUG && true
            foreach (var file in _fileStorage.FindAll().ToArray())
            {
                _fileStorage.Delete(file.Id);
            }
#endif
            _httpClient = new HttpClient();
        }

        public void Maitenance(TimeSpan expiredTime, int maxCount)
        {
            DateTime expiredDateTime = DateTime.Now - expiredTime;
            foreach (var fileInfo in _fileStorage.FindAll().Where(x => x.Metadata.TryGetValue("updateAt", out var val) && (DateTime)val < expiredDateTime).ToArray())
            {
                _fileStorage.Delete(fileInfo.Id);
            }

            foreach (var fileInfo in _fileStorage.FindAll().OrderByDescending(x => x.UploadDate).Skip(maxCount).ToArray())
            {
                _fileStorage.Delete(fileInfo.Id);
            }
        }

        public async void ResolveImage(BitmapImage image, string imageUrl)
        {
            var id = imageUrl.Replace("https://nicovideo.cdn.nimg.jp/thumbnails/", "");
            if (TryGetCacheImageStream(id, imageUrl, out var stream))
            {
                using var _ = stream;
                image.SetSource(stream.AsRandomAccessStream());
            }
            else
            {
                using var res = await _httpClient.GetAsync(new Uri(imageUrl));
                if (!res.Content.TryComputeLength(out var length)) { throw new InvalidOperationException(); }
                using var memoryStream = new MemoryStream((int)length);
                await res.Content.WriteToStreamAsync(memoryStream.AsOutputStream());
                memoryStream.Seek(0, SeekOrigin.Begin);
                image.SetSource(memoryStream.AsRandomAccessStream());
                memoryStream.Seek(0, SeekOrigin.Begin);
                SetCacheImage(id, imageUrl, memoryStream.AsInputStream());
            }
        }

        private bool TryGetCacheImageStream(string id, string imageUrl, out Stream outImageStream)
        {
            id = $"$/{id}";
            if (_fileStorage.Exists(id))
            {
                var file = _fileStorage.FindById(id);
                var stream = new MemoryStream((int)file.Length);
                try
                {
                    file.CopyTo(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    _fileStorage.SetMetadata(id, new BsonDocument(new Dictionary<string, BsonValue>() { { "updateAt", DateTime.Now } }));
                    outImageStream = stream;
                    return true;
                }
                catch
                {
                    stream.Dispose();
                    throw;
                }
            }
            else
            {
                outImageStream = null;
                return false;
            }

        }


        private bool SetCacheImage(string id, string imageUrl, IInputStream stream)
        {
            try
            {
                id = $"$/{id}";
                var file = _fileStorage.Upload(id, imageUrl, stream.AsStreamForRead());
                _fileStorage.SetMetadata(id, new BsonDocument(new Dictionary<string, BsonValue>() { { "updateAt", DateTime.Now } }));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

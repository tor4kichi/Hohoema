using NicoPlayerHohoema.Models.Db;
using NicoPlayerHohoema.Util;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.AppMap
{
	public class CachedVideoAppMapContainer : AppMapContainerBase
    {
        public const int DisplayCachedItemCount = 7;

		public CachedVideoAppMapContainer()
			: base(HohoemaPageType.CacheManagement, label:"キャッシュ動画")
		{
            HohoemaApp.MediaManager.CacheVideos.ObserveElementProperty(x => x.CachedAt)
                .Subscribe(async x => await Refresh())
                .AddTo(_CompositeDisposable);
        }

        protected override async Task OnRefreshing()
        {
            _DisplayItems.Clear();

            while (!HohoemaApp.MediaManager.IsInitialized)
            {
                await Task.Delay(50);
            }

            var cacheReq = HohoemaApp.MediaManager.CacheVideos
                .OrderBy(x => x.CachedAt)
                .Reverse()
                .Take(DisplayCachedItemCount).ToArray();
            foreach (var req in cacheReq)
            {
                var item = new CachedVideoAppMapItem(req);
                _DisplayItems.Add(item);
            }
        }

    }

	public class CachedVideoAppMapItem : VideoAppMapItemBase
    {
		public CachedVideoAppMapItem(NicoVideo nicoVideo)
		{
			PrimaryLabel = nicoVideo.Title;
            Parameter = nicoVideo.RawVideoId;

            foreach (var divided in nicoVideo.GetAllQuality())
            {
                if (divided.IsCached)
                {
                    SecondaryLabel = divided.Quality.ToString();
                    Quality = divided.Quality;

                    break;
                }
            }
		}
	}
}

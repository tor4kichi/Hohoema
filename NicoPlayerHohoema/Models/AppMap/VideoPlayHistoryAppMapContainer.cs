using Mntone.Nico2.Videos.Histories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.AppMap
{
	public class VideoPlayHistoryAppMapContainer : SelfGenerateAppMapContainerBase
	{
        public VideoPlayHistoryAppMapContainer()
			: base(HohoemaPageType.History, label:"視聴履歴")
		{

        }

		protected override async Task<IEnumerable<IAppMapItem>> GenerateItems(int count)
		{
			var playedHistories = await HohoemaApp.ContentFinder.GetHistory();
            if (playedHistories == null)
            {
                return Enumerable.Empty<IAppMapItem>();
            }

			List<IAppMapItem> items = new List<IAppMapItem>();
			var histories = playedHistories.Histories.Take(count);
			foreach (var history in histories)
			{
				var historyAppMapItem = new VideoPlayHistoryAppMapItem(history, HohoemaApp.Playlist);
				items.Add(historyAppMapItem);
			}

			return items;
		}

        
    }


	public class VideoPlayHistoryAppMapItem : IAppMapItem
	{
		public string PrimaryLabel { get; private set; }
		public string SecondaryLabel { get; private set; }
		public string Parameter { get; private set; }

        public HohoemaPlaylist HohoemaPlaylist { get; private set; }

        public void SelectedAction()
        {
            
        }

		public VideoPlayHistoryAppMapItem(History history, HohoemaPlaylist playlist)
		{
            HohoemaPlaylist = playlist;
            PrimaryLabel = history.Title;

			SecondaryLabel = null;

			Parameter = new VideoPlayPayload()
			{
				VideoId = history.ItemId
			}
			.ToParameterString() ;
		}
	}
}

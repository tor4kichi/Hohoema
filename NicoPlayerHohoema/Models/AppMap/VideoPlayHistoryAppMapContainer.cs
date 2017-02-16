using Mntone.Nico2.Videos.Histories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.AppMap
{
	public class VideoPlayHistoryAppMapContainer : AppMapContainerBase
    {
        public const int VideoPlayHistoryDisplayCount = 5;


        public VideoPlayHistoryAppMapContainer()
			: base(HohoemaPageType.History, label:"視聴履歴")
		{
            
        }

        protected override async Task OnRefreshing()
        {
            var playedHistories = await HohoemaApp.ContentFinder.GetHistory();
            if (playedHistories == null)
            {
                return;
            }

            _DisplayItems.Clear();

            List<IAppMapItem> items = new List<IAppMapItem>();
            var histories = playedHistories.Histories.Take(VideoPlayHistoryDisplayCount);
            foreach (var history in histories)
            {
                var historyAppMapItem = new VideoPlayHistoryAppMapItem(history, HohoemaApp.Playlist);
                _DisplayItems.Add(historyAppMapItem);
            }
        }
    }


	public class VideoPlayHistoryAppMapItem : IAppMapItem
	{
		public string PrimaryLabel { get; private set; }
		public string SecondaryLabel { get; private set; }
		public string Parameter { get; private set; }

        History _History;
        public HohoemaPlaylist HohoemaPlaylist { get; private set; }

        public void SelectedAction()
        {
            HohoemaPlaylist.PlayVideo(_History.ItemId, _History.Title);
        }

		public VideoPlayHistoryAppMapItem(History history, HohoemaPlaylist playlist)
		{
            _History = history;

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

using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.VideoInfoContent
{

	public class RelationVideoInfoContentViewModel : MediaInfoViewModel
	{
		public RelationVideoInfoContentViewModel(string videoId, Models.NiconicoContentFinder contentFinder, NGSettings ngSettings, PageManager pageManager)
		{
			VideoId = videoId;
			ContentFinder = contentFinder;
			NGSettings = ngSettings;
			PageManager = pageManager;


			RelatedVideos = new ObservableCollection<VideoInfoControlViewModel>();


		}


		public async Task LoadRelatedVideo()
		{
			var relatedVideos = await ContentFinder.GetRelatedVideos(VideoId, 0, 5);
			RelatedVideos.Clear();


			foreach (var v in relatedVideos.Video_info)
			{
				RelatedVideos.Add(new VideoInfoControlViewModel(v, NGSettings, PageManager));
			}

		}



		public ObservableCollection<VideoInfoControlViewModel> RelatedVideos { get; private set; }

		public string VideoId { get; private set; }
		public NiconicoContentFinder ContentFinder { get; private set; }
		public NGSettings NGSettings { get; private set; }
		public PageManager PageManager { get; private set; }
	}
}

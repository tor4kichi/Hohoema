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
		public RelationVideoInfoContentViewModel(string videoId, HohoemaApp app, PageManager pageManager)
		{
			VideoId = videoId;
			HohoemaApp = app;
			PageManager = pageManager;


			RelatedVideos = new ObservableCollection<VideoInfoControlViewModel>();


		}


		public async Task LoadRelatedVideo()
		{
			var relatedVideos = await HohoemaApp.ContentFinder.GetRelatedVideos(VideoId, 0, 5);
			RelatedVideos.Clear();


			foreach (var v in relatedVideos.Video_info)
			{
				var nicoVideo = await HohoemaApp.MediaManager.GetNicoVideo(VideoId);
				RelatedVideos.Add(new VideoInfoControlViewModel(v, nicoVideo, PageManager));
			}

		}



		public ObservableCollection<VideoInfoControlViewModel> RelatedVideos { get; private set; }

		public string VideoId { get; private set; }
		public HohoemaApp HohoemaApp { get; private set; }
		public PageManager PageManager { get; private set; }
	}
}

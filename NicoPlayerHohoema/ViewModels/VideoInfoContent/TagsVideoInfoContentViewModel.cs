using Mntone.Nico2.Videos.Thumbnail;
using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.VideoInfoContent
{
	public class TagsVideoInfoContentViewModel : MediaInfoViewModel
	{
		public TagsVideoInfoContentViewModel(ThumbnailResponse thumbnail, PageManager pageManager)
		{
			_ThumbnailResponse = thumbnail;
			_PageManager = pageManager;

			Tags = thumbnail.Tags.Value
				.Select(x => new TagViewModel(x))
				.ToList();
		}

		public List<TagViewModel> Tags { get; private set; }
		ThumbnailResponse _ThumbnailResponse;
		PageManager _PageManager;

	}
}

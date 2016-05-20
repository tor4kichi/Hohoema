using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using Mntone.Nico2.Mylist.MylistGroup;
using Mntone.Nico2.Videos.Ranking;
using Mntone.Nico2.Videos.Thumbnail;
using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
	public class VideoInfoControlViewModel : BindableBase
	{
		// とりあえずマイリストから取得したデータによる初期化
		public VideoInfoControlViewModel(MylistData data, NGSettings ngSettings, PageManager pageManager)
		{
			NGSettings = ngSettings;
			PageManager = pageManager;

			Title = data.Title;
			VideoId = data.ItemId;
			ViewCount = data.ViewCount;
			CommentCount = data.CommentCount;
			MylistCount = data.MylistCount;
			OwnerComment = data.Description;
			PostAt = data.CreateTime;
			ThumbnailImageUrl = data.ThumbnailUrl;
			MovieLength = data.Length;

			IsNotGoodVideo = false;
			NGVideoReason = "";
			IsForceDisplayNGVideo = false;
		}


		// 個別マイリストから取得したデータによる初期化
		public VideoInfoControlViewModel(Video_info data, NGSettings ngSettings, PageManager pageManager)
		{
			NGSettings = ngSettings;
			PageManager = pageManager;

			Title = data.Video.Title.DecodeUTF8();
			VideoId = data.Video.Id;
			ViewCount = uint.Parse(data.Video.View_counter);
			CommentCount = uint.Parse(data.Thread.Num_res);
			MylistCount = uint.Parse(data.Video.Mylist_counter);
			OwnerComment = data.Thread.Summary.DecodeUTF8();
			PostAt = DateTime.Parse(data.Video.Upload_time);
			ThumbnailImageUrl = new Uri(data.Video.Thumbnail_url);
			MovieLength = TimeSpan.FromSeconds(int.Parse(data.Video.Length_in_seconds));

			IsNotGoodVideo = false;
			NGVideoReason = "";
			IsForceDisplayNGVideo = false;
		}

		public VideoInfoControlViewModel(string title, string videoId, NGSettings ngSettings, NiconicoMediaManager mediaMan, PageManager pageManager)
		{
			NGSettings = ngSettings;
			PageManager = pageManager;
			MediaManager = mediaMan;

			Title = title;
			VideoId = videoId;
		}



		public async void LoadThumbnail()
		{
			if (MediaManager == null) { return; }

			var thumbnail = await MediaManager.GetThumbnail(VideoId);

			// NG判定
			var ngResult = NGSettings.IsNgVideo(thumbnail);
			IsNotGoodVideo = ngResult != null;
			NGVideoReason = ngResult?.GetReasonText() ?? "";
			IsForceDisplayNGVideo = false;



			Title = thumbnail.Title;
			ViewCount = thumbnail.ViewCount;
			CommentCount = thumbnail.CommentCount;
			MylistCount = thumbnail.MylistCount;
			OwnerComment = thumbnail.Description;
			PostAt = thumbnail.PostedAt.LocalDateTime;
			ThumbnailImageUrl = IsNotGoodVideo ? null : thumbnail.ThumbnailUrl;
			MovieLength = thumbnail.Length;
		}

		private string _Title;
		public string Title
		{
			get { return _Title; }
			set { SetProperty(ref _Title, value); }
		}

		private uint _ViewCount;
		public uint ViewCount
		{
			get { return _ViewCount; }
			set { SetProperty(ref _ViewCount, value); }
		}


		private uint _CommentCount;
		public uint CommentCount
		{
			get { return _CommentCount; }
			set { SetProperty(ref _CommentCount, value); }
		}

		private uint _MylistCount;
		public uint MylistCount
		{
			get { return _MylistCount; }
			set { SetProperty(ref _MylistCount, value); }
		}

		private string _OwnerComment;
		public string OwnerComment
		{
			get { return _OwnerComment; }
			set { SetProperty(ref _OwnerComment, value); }
		}

		private TimeSpan _MovieLength;
		public TimeSpan MovieLength
		{
			get { return _MovieLength; }
			set { SetProperty(ref _MovieLength, value); }
		}

		private Uri _ThumbnailImageUrl;
		public Uri ThumbnailImageUrl
		{
			get { return _ThumbnailImageUrl; }
			set { SetProperty(ref _ThumbnailImageUrl, value); }
		}

		private bool _IsNotGoodVideo;
		public bool IsNotGoodVideo
		{
			get { return _IsNotGoodVideo; }
			set { SetProperty(ref _IsNotGoodVideo, value); }
		}

		private string _NGVideoReason;
		public string NGVideoReason
		{
			get { return _NGVideoReason; }
			set { SetProperty(ref _NGVideoReason, value); }
		}

		private bool _IsForceDisplayNGVideo;
		public bool IsForceDisplayNGVideo
		{
			get { return _IsForceDisplayNGVideo; }
			set { SetProperty(ref _IsForceDisplayNGVideo, value); }
		}


		private DateTime _PostAt;
		public DateTime PostAt
		{
			get { return _PostAt; }
			set { SetProperty(ref _PostAt, value); }
		}


		
		public string VideoId { get; private set; }

		private DelegateCommand _ShowDetailCommand;
		public DelegateCommand ShowDetailCommand
		{
			get
			{
				return _ShowDetailCommand
					?? (_ShowDetailCommand = new DelegateCommand(() =>
					{
						PageManager.OpenPage(HohoemaPageType.VideoInfomation, VideoId);
					}));
			}
		}
		private DelegateCommand _PlayCommand;
		public DelegateCommand PlayCommand
		{
			get
			{
				return _PlayCommand
					?? (_PlayCommand = new DelegateCommand(() =>
					{
						PageManager.OpenPage(HohoemaPageType.VideoInfomation, VideoId);
					}));
			}
		}


		private DelegateCommand _ForceDisplayNGVideoCommand;
		public DelegateCommand ForceDisplayNGVideoCommand
		{
			get
			{
				return _ForceDisplayNGVideoCommand
					?? (_ForceDisplayNGVideoCommand = new DelegateCommand(() => 
					{
						IsForceDisplayNGVideo = true;
					}));
			}
		}

		private DelegateCommand _StopNGVideoDisplayCommand;
		public DelegateCommand StopNGVideoDisplayCommand
		{
			get
			{
				return _StopNGVideoDisplayCommand
					?? (_StopNGVideoDisplayCommand = new DelegateCommand(() =>
					{
						IsForceDisplayNGVideo = false;
					}));
			}
		}


		public NGSettings NGSettings { get; private set; }
		public PageManager PageManager { get; private set; }
		public NiconicoMediaManager MediaManager { get; private set; }
	}
}

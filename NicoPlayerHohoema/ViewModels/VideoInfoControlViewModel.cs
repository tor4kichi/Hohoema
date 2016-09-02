using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using Mntone.Nico2.Mylist.MylistGroup;
using Mntone.Nico2.Searches.Video;
using Mntone.Nico2.Videos.Ranking;
using Mntone.Nico2.Videos.Thumbnail;
using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NicoPlayerHohoema.ViewModels
{
	public class VideoInfoControlViewModel : HohoemaListingPageItemBase
	{
	//	private IScheduler scheduler;


		

		// とりあえずマイリストから取得したデータによる初期化
		public VideoInfoControlViewModel(MylistData data, NicoVideo nicoVideo, PageManager pageManager)
			: this(nicoVideo, pageManager)
		{
			Title = data.Title;
			RawVideoId = data.ItemId;
			ViewCount = data.ViewCount;
			CommentCount = data.CommentCount;
			MylistCount = data.MylistCount;
			OwnerComment = data.Description;
			PostAt = data.CreateTime;
			ThumbnailImageUrl = data.ThumbnailUrl.AbsoluteUri;
			MovieLength = data.Length;

			IsNotGoodVideo = false;
			NGVideoReason = "";
			IsForceDisplayNGVideo = false;

			//		scheduler = new SynchronizationContextScheduler(SynchronizationContext.Current);

			VideoId = RawVideoId;
			IsDeleted = nicoVideo.IsDeleted;
		}


		// 個別マイリストから取得したデータによる初期化
		public VideoInfoControlViewModel(VideoInfo data, NicoVideo nicoVideo, PageManager pageManager)
			: this(nicoVideo, pageManager)
		{
			Title = data.Video.Title;
			RawVideoId = data.Video.Id;
			ViewCount = (uint)data.Video.ViewCount;
			CommentCount = data.Thread.GetCommentCount();
			MylistCount = (uint)data.Video.MylistCount;
			
//			OwnerComment = data.Thread.GetDecodedSummary();
			PostAt = data.Video.UploadTime;
			ThumbnailImageUrl = data.Video.ThumbnailUrl.AbsoluteUri;
			MovieLength = data.Video.Length;

			IsNotGoodVideo = false;
			NGVideoReason = "";
			IsForceDisplayNGVideo = false;
			VideoId = RawVideoId;
			IsDeleted = nicoVideo.IsDeleted;
		}


		public VideoInfoControlViewModel(NicoVideo nicoVideo, PageManager pageManager)
		{
			PageManager = pageManager;
			NicoVideo = nicoVideo;
			_CompositeDisposable = new CompositeDisposable();

			Title = nicoVideo.Title;
			RawVideoId = nicoVideo.RawVideoId;
			VideoId = nicoVideo.VideoId;

			IsDeleted = nicoVideo.IsDeleted;

			IsOriginalQualityCached = NicoVideo.OriginalQuality.ObserveProperty(x => x.IsCacheRequested)
				.ToReactiveProperty()
				.AddTo(_CompositeDisposable);
			IsLowQualityCached = NicoVideo.LowQuality.ObserveProperty(x => x.IsCacheRequested)
				.ToReactiveProperty()
				.AddTo(_CompositeDisposable);


			if (NicoVideo.ThumbnailResponseCache.HasCache)
			{
				SetupFromThumbnail(NicoVideo.ThumbnailResponseCache.CachedItem);
			}


			IsForceDisplayNGVideo = false;
			IsStillNotWatch = true;
		}

		private void SetupFromThumbnail(ThumbnailResponse thumbnail)
		{
			Title = thumbnail.Title;
			ViewCount = thumbnail.ViewCount;
			CommentCount = thumbnail.CommentCount;
			MylistCount = thumbnail.MylistCount;
			OwnerComment = thumbnail.Description;
			PostAt = thumbnail.PostedAt.LocalDateTime;
			ThumbnailImageUrl = IsNotGoodVideo ? null : thumbnail.ThumbnailUrl.AbsoluteUri;
			MovieLength = thumbnail.Length;

			VideoId = thumbnail.Id;

			// NG判定
			var ngResult = NicoVideo.CheckUserNGVideo(thumbnail);
			IsNotGoodVideo = ngResult != null;
			NGVideoReason = ngResult?.GetReasonText() ?? "";
		}


		public async Task LoadThumbnail()
		{
			if (ThumbnailImageUrl == null)
			{
				if (NicoVideo == null) { return; }

				if (NicoVideo.IsDeleted) { return; }

				try
				{
					var thumbnail = await NicoVideo.GetThumbnailResponse();

					SetupFromThumbnail(thumbnail);

					if (NicoVideo.IsDeleted)
					{
						IsDeleted = true;
					}
				}
				catch
				{
					IsDeleted = true;
				}
			}
		}

		public override void Dispose()
		{
			_CompositeDisposable?.Dispose();
		}



		protected virtual VideoPlayPayload MakeVideoPlayPayload()
		{
			return new VideoPlayPayload()
			{
				VideoId = RawVideoId,
				Quality = null,
			};
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

		private string _ThumbnailImageUrl;
		public string ThumbnailImageUrl
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

		private bool _IsDeleted;
		public bool IsDeleted
		{
			get { return _IsDeleted; }
			set { SetProperty(ref _IsDeleted, value); }
		}


		
		public string VideoId { get; private set; }

		public string RawVideoId { get; private set; }

		public ReactiveProperty<bool> IsOriginalQualityCached { get; private set; }
		public ReactiveProperty<bool> IsLowQualityCached { get; private set; }



		private bool _IsStillNotWatch;
		public bool IsStillNotWatch
		{
			get { return _IsStillNotWatch; }
			set { SetProperty(ref _IsStillNotWatch, value); }
		}


		public override ICommand SelectedCommand
		{
			get
			{
				return PlayCommand;
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
						var payload = MakeVideoPlayPayload();

						PageManager.OpenPage(HohoemaPageType.VideoPlayer, payload.ToParameterString());
					}, () => 
					{
						return !IsNotGoodVideo || IsForceDisplayNGVideo;
					}));
			}
		}


		private DelegateCommand _ForceDisplayNGVideoCommand;
		public DelegateCommand ForceDisplayNGVideoCommand
		{
			get
			{
				return _ForceDisplayNGVideoCommand
					?? (_ForceDisplayNGVideoCommand = new DelegateCommand(async () => 
					{
						// Note: NG動画の強制表示させる前に動画再生が行われないようにTask.Delayを挟みます

						// NGVideoの解除操作が HohoemaVideoListViewModelBase.PlayCommand と競合しています
						// IsForceDisplayNGVideo=true にするタイミングを 
						// HohoemaVideoListViewModelBase.PlayCommand よりも後にして
						// 少しDelayさせます
						
						await Task.Delay(100);

						IsForceDisplayNGVideo = true;

						PlayCommand.RaiseCanExecuteChanged();
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

						PlayCommand.RaiseCanExecuteChanged();
					}));
			}
		}

		protected CompositeDisposable _CompositeDisposable { get; private set; }

		public NicoVideo NicoVideo { get; private set; }
		public PageManager PageManager { get; private set; }
	}
}

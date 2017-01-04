using BackgroundAudioShared;
using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using Mntone.Nico2.Mylist.MylistGroup;
using Mntone.Nico2.Searches.Video;
using Mntone.Nico2.Videos.Ranking;
using Mntone.Nico2.Videos.Thumbnail;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Db;
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
            OptionText = data.CreateTime.ToString();
            if (!string.IsNullOrWhiteSpace(data.ThumbnailUrl.OriginalString))
            {
                ImageUrlsSource.Add(data.ThumbnailUrl.OriginalString);
            }

            if (!nicoVideo.IsDeleted)
            {
                Description = $"再生:{data.ViewCount}";
            }

			ImageCaption = data.Length.ToString(); // TODO: ユーザーフレンドリィ時間

			VideoId = RawVideoId;
		}


		// 個別マイリストから取得したデータによる初期化
		public VideoInfoControlViewModel(VideoInfo data, NicoVideo nicoVideo, PageManager pageManager)
			: this(nicoVideo, pageManager)
		{
            
            Title = data.Video.Title;
            RawVideoId = data.Video.Id;
            OptionText = data.Video.UploadTime.ToString();
            if (!string.IsNullOrWhiteSpace(data.Video.ThumbnailUrl.OriginalString))
            {
                ImageUrlsSource.Add(data.Video.ThumbnailUrl.OriginalString);
            }

            if (!nicoVideo.IsDeleted)
            {
                Description = $"再生:{data.Video.ViewCount}";
            }

            ImageCaption = data.Video.Length.ToString(); // TODO: ユーザーフレンドリィ時間

            VideoId = RawVideoId;
        }


		public VideoInfoControlViewModel(NicoVideo nicoVideo, PageManager pageManager)
		{
			PageManager = pageManager;
			NicoVideo = nicoVideo;
			_CompositeDisposable = new CompositeDisposable();

			Title = nicoVideo.Title;
			RawVideoId = nicoVideo.RawVideoId;
			VideoId = nicoVideo.VideoId;

            if (nicoVideo.IsDeleted)
            {
                Description = "Deleted";
            }

            

            NicoVideo.OriginalQuality.ObserveProperty(x => x.IsCacheRequested)
				.Subscribe(origcached => 
                {
                    if (origcached)
                    {
                        VideoStatus |= VideoStatus.CachedHighLegacy;
                    }
                    else
                    {
                        VideoStatus &= ~VideoStatus.CachedHighLegacy;
                    }
                })
				.AddTo(_CompositeDisposable);

			NicoVideo.LowQuality.ObserveProperty(x => x.IsCacheRequested)
				.Subscribe(lowLegacyCached => 
                {
                    if (lowLegacyCached)
                    {
                        VideoStatus |= VideoStatus.CachedLowLegacy;
                    }
                    else
                    {
                        VideoStatus &= ~VideoStatus.CachedLowLegacy;
                    }
                })
				.AddTo(_CompositeDisposable);

			SetupFromThumbnail(nicoVideo);
		}

		public void SetupFromThumbnail(NicoVideo info)
		{
			// NG判定
			var ngResult = NicoVideo.CheckUserNGVideo();
			IsVisible = ngResult != null;

            Title = info.Title;
            OptionText = info.PostedAt.ToString();
            if (!string.IsNullOrWhiteSpace(info.ThumbnailUrl))
            {
                ImageUrlsSource.Add(info.ThumbnailUrl);
            }

            if (!info.IsDeleted)
            {
                Description = $"再生:{info.ViewCount}";
            }

            ImageCaption = info.VideoLength.ToString(); // TODO: ユーザーフレンドリィ時間
        }


		
		protected override void OnDispose()
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

       
		
		public string VideoId { get; private set; }

		public string RawVideoId { get; private set; }

        public VideoStatus VideoStatus { get; private set; }


		public override ICommand PrimaryCommand
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
					}));
			}
		}

		protected CompositeDisposable _CompositeDisposable { get; private set; }

		public NicoVideo NicoVideo { get; private set; }
		public PageManager PageManager { get; private set; }
	}


    [Flags]
    public enum VideoStatus
    {
        Watched = 0x0001,

        CachedLowLegacy = 0x0010,
        CachedHighLegacy = 0x0020,
        CachedLow = 0x0100,
        CachedMiddle = 0x0200,
        CachedHigh = 0x0400,

        Filtered = 0x1000,
    }
}

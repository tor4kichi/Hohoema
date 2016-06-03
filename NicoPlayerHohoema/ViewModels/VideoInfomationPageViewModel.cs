using Mntone.Nico2.Videos.WatchAPI;
using Prism.Mvvm;
using Prism.Windows.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Windows.UI.Xaml;
using NicoPlayerHohoema.Models;
using Mntone.Nico2;
using Windows.UI.Core;
using NicoPlayerHohoema.ViewModels.VideoInfoContent;
using Mntone.Nico2.Videos.Thumbnail;
using Prism.Commands;
using System.Reactive.Linq;

namespace NicoPlayerHohoema.ViewModels
{
	public class VideoInfomationPageViewModel : ViewModelBase, IDisposable
	{


		public VideoInfomationPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager, INavigationService ns)
		{
			_HohoemaApp = hohoemaApp;
			_PageManager = pageManager;
			_NavigationService = ns;

			
		}

		public void Dispose()
		{
		}



		public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (VideoId != null)
			{
				return;
			}

			if (e.Parameter is WatchApiResponse)
			{
				VideoInfo = e.Parameter as WatchApiResponse;
				VideoId = VideoInfo.flashvars.videoId;
			}
			else
			{
				if (e?.Parameter is string)
				{
					VideoId = e.Parameter as string;
				}
				else if (viewModelState.ContainsKey(nameof(VideoId)))
				{
					VideoId = (string)viewModelState[nameof(VideoId)];
				}



				var currentUIDispatcher = Window.Current.Dispatcher;

				try
				{
					if (VideoId == null) { return; }

					if (await _HohoemaApp.CheckSignedInStatus() == NiconicoSignInStatus.Success)
					{
						await _HohoemaApp.NiconicoContext.Video.GetWatchApiAsync(VideoId)
							.ContinueWith(async prevTask =>
							{
								await currentUIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
								{
									VideoInfo = prevTask.Result;
								});
							});
					}
					else
					{
						// ログインに失敗
						VideoInfo = null;
					}

				}
				catch (Exception exception)
				{
					// 動画情報の取得に失敗
					System.Diagnostics.Debug.Write(exception.Message);
					return;
				}
			}


			try
			{
				ThumbnailResponse = await _HohoemaApp.MediaManager.GetThumbnail(VideoId);

				UserName = ThumbnailResponse.UserName;
				UserIconUrl = ThumbnailResponse.UserIconUrl;
				SubmitDate = ThumbnailResponse.PostedAt.LocalDateTime;

				//			UserName = response.UserName;

				Title = ThumbnailResponse.Title;
				PlayCount = ThumbnailResponse.ViewCount;
				CommentCount = ThumbnailResponse.CommentCount;
				MylistCount = ThumbnailResponse.MylistCount;
				ThumbnailUrl = ThumbnailResponse.ThumbnailUrl;

				
			}
			catch (Exception exception)
			{
				System.Diagnostics.Debug.Write("動画サムネイル情報の取得または反映に失敗しました。");
				System.Diagnostics.Debug.Write(exception.Message);
			}

//			_PageManager.PageTitle = Title;



			var uri = await VideoDescriptionHelper.PartHtmlOutputToCompletlyHtml(VideoId, VideoInfo.videoDetail.description);

			RelationVideoInfoContentViewModel relatedVideoVM = new RelationVideoInfoContentViewModel(VideoId, _HohoemaApp.ContentFinder, _HohoemaApp.UserSettings.NGSettings, _PageManager);
			VideoInfoContentItems = new List<MediaInfoViewModel>()
			{
				new SummaryVideoInfoContentViewModel(ThumbnailResponse, uri, _PageManager),
				new TagsVideoInfoContentViewModel(ThumbnailResponse, _PageManager),
				relatedVideoVM,
//				new IchibaVideoInfoContentViewModel(VideoId, _HohoemaApp.ContentFinder)
			};

			await relatedVideoVM.LoadRelatedVideo();


			OnPropertyChanged(nameof(VideoInfoContentItems));




			base.OnNavigatedTo(e, viewModelState);
		}



		

		private DelegateCommand _NavigationBackCommand;
		public DelegateCommand NavigationBackCommand
		{
			get
			{
				return _NavigationBackCommand
					?? (_NavigationBackCommand = new DelegateCommand(() =>
					{
						_NavigationService.GoBack();
					}
					, () => _NavigationService.CanGoBack()
					));
			}
		}


		private DelegateCommand _PlayVideoCommand;
		public DelegateCommand PlayVideoCommand
		{
			get
			{
				return _PlayVideoCommand
					?? (_PlayVideoCommand = new DelegateCommand(() => 
					{
						_PageManager.OpenPage(HohoemaPageType.VideoPlayer, VideoId);
					}
					, () => VideoId != null
					));
			}
		}



		public string VideoId { get; private set; }

		private WatchApiResponse _VideoInfo;
		public WatchApiResponse VideoInfo
		{
			get { return _VideoInfo; }
			set { SetProperty(ref _VideoInfo, value); }
		}



		public List<MediaInfoViewModel> VideoInfoContentItems { get; private set; }
		

		public ThumbnailResponse ThumbnailResponse { get; private set; }


		private string _Title;
		public string Title
		{
			get { return _Title; }
			set { SetProperty(ref _Title, value); }
		}

		private string _UserName;
		public string UserName
		{
			get { return _UserName; }
			set { SetProperty(ref _UserName, value); }
		}



		private Uri _UserIconUrl;
		public Uri UserIconUrl
		{
			get { return _UserIconUrl; }
			set { SetProperty(ref _UserIconUrl, value); }
		}

		private DateTime _SubmitDate;
		public DateTime SubmitDate
		{
			get { return _SubmitDate; }
			set { SetProperty(ref _SubmitDate, value); }
		}



		private uint _PlayCount;
		public uint PlayCount
		{
			get { return _PlayCount; }
			set { SetProperty(ref _PlayCount, value); }
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

		private Uri _ThumbnailUrl;
		public Uri ThumbnailUrl
		{
			get { return _ThumbnailUrl; }
			set { SetProperty(ref _ThumbnailUrl, value); }
		}

		private HohoemaApp _HohoemaApp;
		private PageManager _PageManager;
		private INavigationService _NavigationService;
	}


	public enum MediaInfoDisplayType
	{
		Summary,
		Related,
		Ichiba,
	}

	abstract public class MediaInfoViewModel : BindableBase
	{
	}
}

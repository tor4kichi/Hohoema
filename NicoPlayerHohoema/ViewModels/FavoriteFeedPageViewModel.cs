using NicoPlayerHohoema.Models;
using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using System.Collections.ObjectModel;
using Prism.Commands;

namespace NicoPlayerHohoema.ViewModels
{
	public class FavoriteFeedPageViewModel : ViewModelBase
	{
		

		public FavoriteFeedPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
		{
			_HohoemaApp = hohoemaApp;
			_NGSettings = hohoemaApp.UserSettings.NGSettings;
			_MediaManager = hohoemaApp.MediaManager;
			_PageManager = pageManager;

			VideoInfoItems = new ObservableCollection<FavoriteVideoInfoControlViewModel>();
		}


		public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{

			if (e.Parameter is string)
			{
				_Parameter = FavoritePageParameter.FromJson(e.Parameter as string);
			}
			else
			{
				throw new NotSupportedException();
			}


			var feedList = _HohoemaApp.FavFeedManager.FindFavFeedList(_Parameter.ItemType, _Parameter.Id);

			if (feedList != null)
			{
				_PageManager.PageTitle = _PageManager.CurrentDefaultPageTitle() + feedList.Name;

				Title = feedList.Name;
				ItemType = feedList.FavoriteItemType;
				SourceId = feedList.Id;
				IsFavorite = !feedList.IsDeleted;

				await _HohoemaApp.FavFeedManager.UpdateFavFeedList(feedList);

				

				foreach (var item in feedList.Items.ToArray())
				{
					var nicoVideo = await _MediaManager.GetNicoVideo(item.VideoId);
					var feedItemVM = new FavoriteVideoInfoControlViewModel(item, nicoVideo, _PageManager);
					feedItemVM.LoadThumbnail();
					VideoInfoItems.Add(feedItemVM);
				}
			}
			else
			{
				// error, missing feedList.
			}
			base.OnNavigatedTo(e, viewModelState);

		}



		private DelegateCommand _UnfavCommand;
		public DelegateCommand UnfavCommand
		{
			get
			{
				return _UnfavCommand
					?? (_UnfavCommand = new DelegateCommand(async () =>
					{
						var result = await _HohoemaApp.FavFeedManager.RemoveFav(ItemType, SourceId);

						if (result == Mntone.Nico2.ContentManageResult.Success)
						{
							IsFavorite = false;
						}
					}));
			}
		}

		private DelegateCommand _FavCommand;
		public DelegateCommand FavCommand
		{
			get
			{
				return _FavCommand
					?? (_FavCommand = new DelegateCommand(async () =>
					{
						var result = await _HohoemaApp.FavFeedManager.AddFav(ItemType, SourceId);

						if (result == Mntone.Nico2.ContentManageResult.Success)
						{
							IsFavorite = true;
						}
					}));
			}
		}

		private bool _IsFavorite;
		public bool IsFavorite
		{
			get { return _IsFavorite; }
			set { SetProperty(ref _IsFavorite, value); }
		}

		private string _Title;
		public string Title
		{
			get { return _Title; }
			set { SetProperty(ref _Title, value); }
		}

		private FavoriteItemType _ItemType;
		public FavoriteItemType ItemType
		{
			get { return _ItemType; }
			set { SetProperty(ref _ItemType, value); }
		}

		private string _SourceId;
		public string SourceId
		{
			get { return _SourceId; }
			set { SetProperty(ref _SourceId, value); }
		}

		public ObservableCollection<FavoriteVideoInfoControlViewModel> VideoInfoItems { get; private set; }


		FavoritePageParameter _Parameter;

		HohoemaApp _HohoemaApp;
		NGSettings _NGSettings;
		NiconicoMediaManager _MediaManager;
		PageManager _PageManager;
	}
}

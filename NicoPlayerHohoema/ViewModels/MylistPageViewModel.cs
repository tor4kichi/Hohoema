using NicoPlayerHohoema.Models;
using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Prism.Mvvm;
using Prism.Commands;
using Mntone.Nico2.Mylist;
using System.Collections.ObjectModel;
using Mntone.Nico2;

namespace NicoPlayerHohoema.ViewModels
{
	public class MylistPageViewModel : ViewModelBase
	{
		public MylistPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
		{
			_HohoemaApp = hohoemaApp;
			_PageManager = pageManager;

			MylistItems = new ObservableCollection<VideoInfoControlViewModel>();
		}

		public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (e.Parameter is string)
			{
				MylistGroupId = e.Parameter as string;
			}


			if (MylistGroupId != null)
			{
				MylistItems.Clear();

				if (MylistGroupId == "0")
				{
					MylistTitle = "とりあえずマイリスト";
					MylistDescription = "";
					// TODO とりまのマイリストアイテム一覧取得
					var res = await _HohoemaApp.NiconicoContext.Mylist.GetMylistItemListAsync(MylistGroupId);
					
					foreach (var item in res)
					{
						var nicoVideo = await _HohoemaApp.MediaManager.GetNicoVideo(item.ItemId);
						MylistItems.Add(new VideoInfoControlViewModel(item, nicoVideo, _PageManager));
					}
				}
				else
				{
					var response = await _HohoemaApp.ContentFinder.GetMylist(MylistGroupId);

					foreach (var item in response.Video_info)
					{
						var nicoVideo = await _HohoemaApp.MediaManager.GetNicoVideo(item.Video.Id);
						MylistItems.Add(new VideoInfoControlViewModel(item, nicoVideo, _PageManager));
					}

					MylistTitle = StringExtention.DecodeUTF8(response.Name);
					MylistDescription = StringExtention.DecodeUTF8(response.Description);
				}
			}

			base.OnNavigatedTo(e, viewModelState);
		}


		private string _MylistTitle;
		public string MylistTitle
		{
			get { return _MylistTitle; }
			set { SetProperty(ref _MylistTitle, value); }
		}

		private string _MylistDescription;
		public string MylistDescription
		{
			get { return _MylistDescription; }
			set { SetProperty(ref _MylistDescription, value); }
		}

		public string MylistGroupId { get; private set; }

		public ObservableCollection<VideoInfoControlViewModel> MylistItems { get; private set; }

		HohoemaApp _HohoemaApp;
		PageManager _PageManager;
	}



	

	
}

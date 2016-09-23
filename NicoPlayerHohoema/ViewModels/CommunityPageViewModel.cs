using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using Prism.Windows.Navigation;
using Mntone.Nico2.Communities.Info;
using System.Diagnostics;

namespace NicoPlayerHohoema.ViewModels
{
	public class CommunityPageViewModel : HohoemaViewModelBase
	{

		public string CommunityId { get; private set; }

		public CommunityInfo CommunityInfo { get; private set; }

		public string CommunityName => CommunityInfo?.Name;

		public string CommunityDescription => CommunityInfo?.Description;

		public bool IsPublic => CommunityInfo?.IsPublic ?? false;

		public bool IsOfficial => CommunityInfo?.IsOfficial ?? false;

		public uint MaxUserCount => CommunityInfo?.UserMax ?? 0;

		public uint UserCount => CommunityInfo?.UserCount ?? 0;

		public uint CommunityLevel => CommunityInfo?.Level ?? 0;

		public DateTime CreatedAt => CommunityInfo?.CreateTime ?? DateTime.MinValue;

		public string ThumbnailUrl => CommunityInfo?.Thumbnail;

		public Uri TopUrl => CommunityInfo.TopUrl != null ? new Uri(CommunityInfo.TopUrl) : null;

		private bool _NowLoading;
		public bool NowLoading
		{
			get { return _NowLoading; }
			set { SetProperty(ref _NowLoading, value); }
		}

		private bool _IsFailed;
		public bool IsFailed
		{
			get { return _IsFailed; }
			set { SetProperty(ref _IsFailed, value); }
		}

		public CommunityPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, pageManager, isRequireSignIn:true)
		{

		}

		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			// ナビゲーションパラメータからコミュニティIDを取得
			IsFailed = false;
			try
			{
				NowLoading = true;

				CommunityId = null;
				if (e.Parameter is string)
				{
					CommunityId = e.Parameter as string;
				}

				// コミュニティ情報の取得
				if (!string.IsNullOrEmpty(CommunityId))
				{
					var res = await HohoemaApp.ContentFinder.GetCommunityInfo(CommunityId);

					if (res == null || !res.IsStatusOK) { return; }

					CommunityInfo = res.Community;

					OnPropertyChanged(nameof(CommunityName));
					OnPropertyChanged(nameof(IsPublic));
					OnPropertyChanged(nameof(CommunityDescription));
					OnPropertyChanged(nameof(IsOfficial));
					OnPropertyChanged(nameof(MaxUserCount));
					OnPropertyChanged(nameof(UserCount));
					OnPropertyChanged(nameof(CommunityLevel));
					OnPropertyChanged(nameof(CreatedAt));
					OnPropertyChanged(nameof(ThumbnailUrl));
					OnPropertyChanged(nameof(TopUrl));



					var detail = await HohoemaApp.ContentFinder.GetCommunityDetail(CommunityId);



				}




			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
				IsFailed = true;
			}
			finally
			{
				NowLoading = false;
			}

			// UpdateTitle
			if (!IsFailed)
			{
				UpdateTitle($"{CommunityName} のコミュニティ情報");
			}
		}
	}
}

using Mntone.Nico2;
using Mntone.Nico2.Videos.Thumbnail;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public class HohoemaApp : BindableBase
	{
		public HohoemaApp(IEventAggregator ea)
		{
			EventAggregator = ea;

			UserSettings = new HohoemaUserSettings();
			NiconicoPlayer = new NiconicoPlayer(this);
			NiconicoContext = new NiconicoContext();
		}

		public async Task LoadUserSettings()
		{
			UserSettings = await HohoemaUserSettings.LoadSettings();
		}

		public async Task SaveUserSettings()
		{
			await UserSettings?.Save();
		}

		public async Task<NiconicoSignInStatus> SignInFromUserSettings()
		{
			if (UserSettings.AccontSettings.IsValidMailOreTelephone && UserSettings.AccontSettings.IsValidPassword)
			{
				return await SignIn(UserSettings.AccontSettings.MailOrTelephone, UserSettings.AccontSettings.Password);
			}
			else
			{
				return NiconicoSignInStatus.Failed;
			}
		}


		public async Task<NiconicoSignInStatus> SignIn(string mailOrTelephone, string password)
		{
			NiconicoContext = new NiconicoContext(new NiconicoAuthenticationToken(mailOrTelephone, password));

			NiconicoContext.AdditionalUserAgent = HohoemaUserAgent;

			return await NiconicoContext.SignInAsync();
		}

		public async Task<NiconicoSignInStatus> SignOut()
		{
			if (NiconicoContext == null)
			{
				return NiconicoSignInStatus.Failed;
			}

			var result = await NiconicoContext.SignOutOffAsync();
			NiconicoContext.Dispose();

			NiconicoContext = null;

			return result;
		}

		public async Task<NiconicoSignInStatus> CheckSignedInStatus()
		{
			return await NiconicoContext.GetIsSignedInAsync();
		}


		public void PlayVideo(string videoUrl)
		{
			EventAggregator.GetEvent<Events.PlayNicoVideoEvent>()
				.Publish(videoUrl);
		}


		public NGResult IsNgVideo(ThumbnailResponse response)
		{
			var ng = UserSettings.NGSettings;

			// 動画IDによるNG判定
			if (ng.NGVideoIdEnable && ng.NGVideoIds.Count > 0)
			{
				var ngItem = ng.NGVideoIds.SingleOrDefault(x => x.VideoId == response.Id);

				if (ngItem != null)
				{
					return new NGResult()
					{
						NGReason = NGReason.VideoId,
						Content = ngItem.VideoId,
						NGDescription = ngItem.Description,
					};
				}
			}

			// 動画投稿者によるNG判定
			if (ng.NGVideoOwnerUserIdEnable && ng.NGVideoOwnerUserIds.Count > 0)
			{
				var ngItem = ng.NGVideoOwnerUserIds.SingleOrDefault(x => x.UserId == response.UserId);

				if (ngItem != null)
				{
					return new NGResult()
					{
						NGReason = NGReason.UserId,
						Content = ngItem.UserId.ToString(),
						NGDescription = ngItem.Description
					};
				}
			}

			// 動画タイトルによるNG判定
			if (ng.NGVideoTitleKeywordEnable && ng.NGVideoTitleKeywords.Count > 0)
			{
				var ngItem = ng.NGVideoTitleKeywords.FirstOrDefault(x => response.Title.Contains(x.Keyword));

				if (ngItem != null)
				{
					return new NGResult()
					{
						NGReason = NGReason.Keyword,
						Content = ngItem.Keyword,
					};
				}
			}

			return null;
		}



		public HohoemaUserSettings UserSettings { get; private set; }


		private NiconicoContext _NiconicoContext;
		public NiconicoContext NiconicoContext
		{
			get { return _NiconicoContext; }
			set { SetProperty(ref _NiconicoContext, value); }
		}

		public NiconicoPlayer NiconicoPlayer { get; private set; }

		public const string HohoemaUserAgent = "Hohoema_UWP";

		public IEventAggregator EventAggregator { get; private set; }
	}
	

	public class NGResult
	{
		public NGReason NGReason { get; set; }
		public string NGDescription { get; set; } = "";
		public string Content { get; set; }

		internal string GetReasonText()
		{
			switch (NGReason)
			{
				case NGReason.VideoId:
					return $"NG対象の動画ID : {Content}";
				case NGReason.UserId:
					return $"NG対象の投稿者ID : {Content}";
				case NGReason.Keyword:
					return $"NG対象のキーワード : {Content}";
				default:
					throw new NotSupportedException();
			}
		}
	}

	public enum NGReason
	{
		VideoId,
		UserId,
		Keyword,
	}
}

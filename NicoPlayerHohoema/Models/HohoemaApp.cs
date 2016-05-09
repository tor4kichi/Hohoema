using Mntone.Nico2;
using Mntone.Nico2.Videos.Thumbnail;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace NicoPlayerHohoema.Models
{
	public class HohoemaApp : BindableBase
	{
		public HohoemaApp(IEventAggregator ea)
		{
			EventAggregator = ea;

			UserSettings = new HohoemaUserSettings();
			MediaManager = new NiconicoMediaManager(this);
			ContentFinder = new NiconicoContentFinder(this);
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
			NiconicoContext?.Dispose();
			NiconicoContext = null;

			var context = new NiconicoContext(new NiconicoAuthenticationToken(mailOrTelephone, password));

			context.AdditionalUserAgent = HohoemaUserAgent;

			var result = await context.SignInAsync();

			if (result == NiconicoSignInStatus.Success)
			{
				NiconicoContext = context;
			}

			return result;
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
			if (NiconicoContext != null)
			{
				return await NiconicoContext.GetIsSignedInAsync();
			}
			else
			{
				return NiconicoSignInStatus.Failed;
			}
		}


		public void PlayVideo(string videoUrl)
		{
			EventAggregator.GetEvent<Events.PlayNicoVideoEvent>()
				.Publish(videoUrl);
		}


		

		public HohoemaUserSettings UserSettings { get; private set; }


		private NiconicoContext _NiconicoContext;
		public NiconicoContext NiconicoContext
		{
			get { return _NiconicoContext; }
			set { SetProperty(ref _NiconicoContext, value); }
		}

		public NiconicoMediaManager MediaManager { get; private set; }

		public NiconicoContentFinder ContentFinder { get; private set; }

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

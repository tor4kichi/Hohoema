using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase.PageNavigation;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Hohoema.Models.Domain.Application;

namespace Hohoema.ViewModels.Community
{
    public class CommunityNewsViewModel
	{
        static public async Task<CommunityNewsViewModel> Create(
			string communityId,
			string title, 
			string authorName, 
			DateTime postAt, 
			string contentHtml,
            PageManager pageManager,
            AppearanceSettings appearanceSettings
			)
		{
            ApplicationTheme appTheme;
            if (appearanceSettings.ApplicationTheme == ElementTheme.Dark)
            {
                appTheme = ApplicationTheme.Dark;
            }
            else if (appearanceSettings.ApplicationTheme == ElementTheme.Light)
            {
                appTheme = ApplicationTheme.Light;
            }
            else
            {
                appTheme = Views.Helpers.SystemThemeHelper.GetSystemTheme();
            }

            var id = $"{communityId}_{postAt.ToString("yy-MM-dd-H-mm")}";
			var uri = await HtmlFileHelper.PartHtmlOutputToCompletlyHtml(id, contentHtml, appTheme);
			return new CommunityNewsViewModel(communityId, title, authorName, postAt, uri, pageManager);
		}

        private CommunityNewsViewModel(
            string communityId,
            string title,
            string authorName,
            DateTime postAt,
            Uri htmlUri,
            PageManager pageManager
            )
        {
            CommunityId = communityId;
            Title = title;
            AuthorName = authorName;
            PostAt = postAt;
            ContentHtmlFileUri = htmlUri;
            PageManager = pageManager;
        }

        public string CommunityId { get; private set; }
		public string Title { get; private set; }
		public string AuthorName { get; private set; }
		public DateTime PostAt { get; private set; }
		public Uri ContentHtmlFileUri { get; private set; }

		public PageManager PageManager { get; private set; }

		


		private RelayCommand<Uri> _ScriptNotifyCommand;
		public RelayCommand<Uri> ScriptNotifyCommand
		{
			get
			{
				return _ScriptNotifyCommand
					?? (_ScriptNotifyCommand = new RelayCommand<Uri>((parameter) =>
					{
						System.Diagnostics.Debug.WriteLine($"script notified: {parameter}");

						PageManager.OpenPage(parameter);
					}));
			}
		}

	}
}

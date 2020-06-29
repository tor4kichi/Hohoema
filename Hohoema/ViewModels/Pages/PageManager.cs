using I18NPortable;
using Hohoema.Database;
using Hohoema.Interfaces;
using Hohoema.Models;
using Hohoema.Models.Helpers;
using Hohoema.Services;
using Hohoema.Services.Helpers;
using Hohoema.ViewModels.Pages;
using Hohoema.UseCase.Playlist;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Hohoema.Models.Pages;
using Hohoema.Models.Repository;
using Hohoema.Models.Repository.Niconico.Mylist;
using Hohoema.Models.Repository.Niconico;
using Hohoema.Models.Repository.VideoCache;
using Hohoema.Models.Repository.App;

namespace Hohoema.ViewModels.Pages
{
    public struct PageNavigationEventArgs
    {
        public string PageName { get; set; }
        public INavigationParameters Paramter { get; set; }
        public bool IsMainViewTarget { get; set; }
        public NavigationStackBehavior Behavior { get; set; }
    }

    public enum NavigationStackBehavior
    {
        Push,
        Root,
        NotRemember,
    }

    public class PageNavigationEvent : PubSubEvent<PageNavigationEventArgs> { }


    public class PageManager : BindableBase
    {
        public PageManager(
            IScheduler scheduler,
            IEventAggregator eventAggregator,
            HohoemaPlaylist playlist,
            CacheSettingsRepository cacheSettingsRepository,
            AppearanceSettingsRepository appearanceSettingsRepository
            )
        {
            Scheduler = scheduler;
            EventAggregator = eventAggregator;
            HohoemaPlaylist = playlist;
            _cacheSettingsRepository = cacheSettingsRepository;
            _appearanceSettingsRepository = appearanceSettingsRepository;
        }


        public HohoemaPlaylist HohoemaPlaylist { get; private set; }
        public IScheduler Scheduler { get; }
        public IEventAggregator EventAggregator { get; }

        public static readonly HashSet<HohoemaPageType> IgnoreRecordNavigationStack = new HashSet<HohoemaPageType>
        {
            HohoemaPageType.Splash,
            HohoemaPageType.PrologueIntroduction,
            HohoemaPageType.EpilogueIntroduction,
        };


        public static readonly HashSet<HohoemaPageType> DontNeedMenuPageTypes = new HashSet<HohoemaPageType>
        {
            HohoemaPageType.Splash,
            HohoemaPageType.PrologueIntroduction,
            HohoemaPageType.NicoAccountIntroduction,
            HohoemaPageType.VideoCacheIntroduction,
            HohoemaPageType.EpilogueIntroduction,
        };

        public static bool IsHiddenMenuPage(HohoemaPageType pageType)
        {
            return DontNeedMenuPageTypes.Contains(pageType);
        }


        /// <summary>
        /// バックナビゲーションの暴発防止等に対応したい場合に利用します
        /// </summary>
        public bool PreventBackNavigation { get; internal set; }


        private DelegateCommand<object> _OpenVideoListPageCommand;
        public DelegateCommand<object> OpenVideoListPageCommand => _OpenVideoListPageCommand
            ?? (_OpenVideoListPageCommand = new DelegateCommand<object>(parameter =>
            {
                
            }));



        private DelegateCommand<object> _OpenContentOwnerPageCommand;
        public DelegateCommand<object> OpenContentOwnerPageCommand => _OpenContentOwnerPageCommand
            ?? (_OpenContentOwnerPageCommand = new DelegateCommand<object>(parameter =>
            {
            
            }
            , parameter => parameter is INiconicoContent
            ));


        public INavigationParameters CurrentPageNavigationParameters { get; private set; }
        public HohoemaPageType CurrentPageType { get; private set; }

        public bool OpenPage(Uri uri)
		{
			var path = uri.AbsoluteUri;
			// is mylist url?
			if (path.StartsWith("http://www.nicovideo.jp/mylist/") || path.StartsWith("https://www.nicovideo.jp/mylist/"))
			{
				var mylistId = uri.AbsolutePath.Split('/').Last();
				System.Diagnostics.Debug.WriteLine($"open Mylist: {mylistId}");

                OpenPageWithId(HohoemaPageType.Mylist, mylistId);
				return true;
			}


			if (path.StartsWith("http://www.nicovideo.jp/watch/") || path.StartsWith("https://www.nicovideo.jp/watch/"))
			{
				// is nico video url?
				var videoId = uri.AbsolutePath.Split('/').Last();
				System.Diagnostics.Debug.WriteLine($"open Video: {videoId}");
                HohoemaPlaylist.Play(videoId);

                return true;
            }

			if (path.StartsWith("http://com.nicovideo.jp/community/") || path.StartsWith("https://com.nicovideo.jp/community/"))
			{
				var communityId = uri.AbsolutePath.Split('/').Last();
                OpenPageWithId(HohoemaPageType.Community, communityId);

                return true;
            }

            if (path.StartsWith("http://com.nicovideo.jp/user/") || path.StartsWith("https://com.nicovideo.jp/user/"))
            {
                var userId = uri.AbsolutePath.Split('/').Last();
                OpenPageWithId(HohoemaPageType.UserInfo, userId);

                return true;
            }

            if (path.StartsWith("http://ch.nicovideo.jp/") || path.StartsWith("https://ch.nicovideo.jp/"))
            {
                var elem = uri.AbsolutePath.Split('/');
                if (elem.Any(x => x == "article" || x == "blomaga"))
                {
                    return false;
                }
                else
                {
                    var channelId = elem.Last();
                    OpenPageWithId(HohoemaPageType.ChannelVideo, channelId);
                }

                return true;
            }

            Debug.WriteLine($"Urlを処理できませんでした : " + uri.OriginalString);

            return false;
        }

        public void OpenDebugPage()
        {
            EventAggregator.GetEvent<PageNavigationEvent>()
                .Publish(new PageNavigationEventArgs()
                {
                    PageName = nameof(Views.DebugPage),
                    IsMainViewTarget = true,
                    Behavior = NavigationStackBehavior.NotRemember,
                });
        }

		public void OpenPage(HohoemaPageType pageType, INavigationParameters parameter = null, NavigationStackBehavior stackBehavior = NavigationStackBehavior.Push)
		{
            EventAggregator.GetEvent<PageNavigationEvent>()
                .Publish(new PageNavigationEventArgs()
                {
                    PageName = _pageTypeToName[pageType],
                    Paramter = parameter,
                    IsMainViewTarget = true,
                    Behavior = stackBehavior,
                });
        }

        public void OpenPage(HohoemaPageType pageType, string parameterString, NavigationStackBehavior stackBehavior = NavigationStackBehavior.Push)
        {
            INavigationParameters parameter = new NavigationParameters(parameterString);
            EventAggregator.GetEvent<PageNavigationEvent>()
                .Publish(new PageNavigationEventArgs()
                {
                    PageName = _pageTypeToName[pageType],
                    Paramter = parameter,
                    IsMainViewTarget = true,
                    Behavior = stackBehavior,
                });
        }

        public void OpenPageWithId(HohoemaPageType pageType, string id, NavigationStackBehavior stackBehavior = NavigationStackBehavior.Push)
        {
            INavigationParameters parameter = new NavigationParameters($"id={id}");
            EventAggregator.GetEvent<PageNavigationEvent>()
                .Publish(new PageNavigationEventArgs()
                {
                    PageName = _pageTypeToName[pageType],
                    Paramter = parameter,
                    IsMainViewTarget = true,
                    Behavior = stackBehavior,
                });
        }


        static readonly Dictionary<HohoemaPageType, string> _pageTypeToName = Enum.GetValues(typeof(HohoemaPageType))
            .Cast<HohoemaPageType>().Select(x => (x, x + "Page")).ToDictionary(x => x.x, x => x.Item2);
        private readonly CacheSettingsRepository _cacheSettingsRepository;
        private readonly AppearanceSettingsRepository _appearanceSettingsRepository;

        public bool IsIgnoreRecordPageType(HohoemaPageType pageType)
		{
			return IgnoreRecordNavigationStack.Contains(pageType);
		}

		public void ForgetLastPage()
		{
            // TODO: ナビゲーション履歴の削除
			
		}



		/// <summary>
		/// 外部で戻る処理が行われた際にPageManager上での整合性を取ります
		/// </summary>
        /*
		public void OnNavigated(INavigationParameters parameters)
		{
            var navigationMode = parameters.GetNavigationMode();

            if (navigationMode == NavigationMode.Back || navigationMode == NavigationMode.Forward)
			{
                string pageTypeString = null;
                
                if (e.SourcePageType.Name.EndsWith("Page"))
                {
                    pageTypeString = e.SourcePageType.Name.Remove(e.SourcePageType.Name.IndexOf("Page"));
                }
                else if (e.SourcePageType.Name.EndsWith("TV"))
                {
                    pageTypeString = e.SourcePageType.Name.Remove(e.SourcePageType.Name.IndexOf("Page_TV"));
                }
                else if (e.SourcePageType.Name.EndsWith("Mobile"))
                {
                    pageTypeString = e.SourcePageType.Name.Remove(e.SourcePageType.Name.IndexOf("Page_Mobile"));
                }


                if (pageTypeString != null)
                { 
                    HohoemaPageType pageType;
					if (Enum.TryParse(pageTypeString, out pageType))
					{
						try
						{
							PageNavigating = true;

							CurrentPageType = pageType;
						}
						finally
						{
							PageNavigating = false;
						}

						System.Diagnostics.Debug.WriteLine($"navigated : {pageType.ToString()}");
					}
					else
					{
						throw new NotSupportedException();
					}

                    PageNavigationParameter = e.Parameter;
                }
                else
				{
					throw new Exception();
				}
			}
		}
        */

		/// <summary>
		/// 画面遷移の履歴を消去します
		/// </summary>
		/// <remarks>
		/// ログイン後にログイン画面の表示履歴を消す時や
		/// ログアウト後にログイン状態中の画面遷移を消すときに利用します。
		/// </remarks>
		public void ClearNavigateHistory()
		{
            Scheduler.Schedule(() =>
            {
                // TODO: ナビゲーションスタックのクリア
            });
			
		}

		public string CurrentDefaultPageTitle()
		{
            return CurrentPageType.Translate();
		}


        public void OpenIntroductionPage()
        {
            OpenPage(HohoemaPageType.PrologueIntroduction);
        }

        public void OpenStartupPage()
        {
            Scheduler.Schedule(() =>
            {
                if (Models.Helpers.InternetConnection.IsInternet())
                {
                    if (IsIgnoreRecordPageType(_appearanceSettingsRepository.StartupPageType))
                    {
                        _appearanceSettingsRepository.StartupPageType = HohoemaPageType.RankingCategoryList;
                    }

                    try
                    {
                        OpenPage(_appearanceSettingsRepository.StartupPageType);
                    }
                    catch
                    {
                        OpenPage(HohoemaPageType.RankingCategoryList);
                    }
                }
                else if (_cacheSettingsRepository.IsAcceptedCache)
                {
                    OpenPage(HohoemaPageType.CacheManagement);
                }
                else
                {
                    OpenPage(HohoemaPageType.Settings);
                }
            });
        }

		public static string PageTypeToTitle(HohoemaPageType pageType)
		{
            return pageType.Translate();
		}
    }
}

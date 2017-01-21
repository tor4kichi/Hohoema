using Mntone.Nico2.Videos.Ranking;
using NicoPlayerHohoema.Models.Settings;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.Foundation;
using Windows.UI.Core;
using NicoPlayerHohoema.Util;

namespace NicoPlayerHohoema.Models.AppMap
{
	public class AppMapManager : BindableBase, IBackgroundUpdateable
	{
		// Note: アプリのメニューやホーム画面で表示する内容の元になるモデルデータ


		//
		// Note: シリアライズ方針
		// 
		// AppMapManager上でRootAppMapContainerをシリアライズ・デシリアライズしてアイテムを管理します
		// データモデルが変更された場合にはRefreshを呼び出すことで更新されるようにします
		// 
		// シリアライズが必要になるデザインアップデートまでは
		// 常にアプリ起動時にリセットさせます

        
		public HohoemaApp HohoemaApp { get; private set; }

		public RootAppMapContainer Root { get; private set; }

        private AsyncLock _RefreshLock = new AsyncLock();

		public AppMapManager(HohoemaApp hohoemaApp)
		{
			HohoemaApp = hohoemaApp;

			Root = new RootAppMapContainer();
		}


		public async Task Refresh()
		{
            using (var releaser = await _RefreshLock.LockAsync())
            {
                await Root.Refresh();

                foreach (var selectable in Root.SelectableItems.ToArray())
                {
                    Root.Add(selectable);
                }
            }
        }

		public IAsyncAction BackgroundUpdate(CoreDispatcher uiDispatcher)
		{
			return uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => 
			{
				await Refresh();
			});
		}
	}

	public class RootAppMapContainer : SelectableAppMapContainerBase
	{
		public static IReadOnlyList<HohoemaPageType> SelectableListingPageTypes { get; private set; } =
			new List<HohoemaPageType>()
			{
				HohoemaPageType.Search,
				HohoemaPageType.RankingCategoryList,
				HohoemaPageType.FeedGroupManage,
				HohoemaPageType.FollowManage,
				HohoemaPageType.UserMylist,
				HohoemaPageType.CacheManagement,
				HohoemaPageType.History,
			};

		public AppMapSettings AppMapSettings { get; private set; }


		public RootAppMapContainer() 
			: base(HohoemaPageType.Portal)
		{
		}


		protected override async Task<IEnumerable<IAppMapItem>> MakeAllItems()
		{
			// 現在の設定を下にアイテムを生成する

			List<IAppMapItem> items = new List<IAppMapItem>();
			foreach (var pageType in SelectableListingPageTypes)
			{
				var container = PageTypeToContainer(pageType);
				
				if (container !=null)
				{
					await container.Refresh();

					// Note: デザインアップデートで削除する
					// アプリ設定等からコンテナを初期化する
//					UpdateFromAppSettings(container);

					items.Add(container);
				}
			}

//			return Task.FromResult(items.AsEnumerable());
			return items;
		}


		private IAppMapContainer PageTypeToContainer(HohoemaPageType pageType)
		{
			IAppMapContainer container = null;
			switch (pageType)
			{
				case HohoemaPageType.RankingCategoryList:
					container = new RankingCategoriesAppMapContainer();
					break;
				case HohoemaPageType.UserMylist:
					container = new UserMylistAppMapContainer();
					break;
				case HohoemaPageType.FollowManage:
					container = new FollowAppMapContainer();
					break;
				case HohoemaPageType.History:
					container = new VideoPlayHistoryAppMapContainer();
					break;
				case HohoemaPageType.Search:
					container = new SearchAppMapContainer();
					break;
				case HohoemaPageType.CacheManagement:
					container = new CachedVideoAppMapContainer();
					break;
				case HohoemaPageType.FeedGroupManage:
					container = new FeedAppMapContainer();
					break;
				default:
					break;
			}

			if (container == null)
			{
				throw new NotSupportedException();
			}

			return container;
		}

	}

	





}

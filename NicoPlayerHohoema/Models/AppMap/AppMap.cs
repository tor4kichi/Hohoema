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

namespace NicoPlayerHohoema.Models.AppMap
{
	public class AppMapManager : BindableBase
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


		public AppMapManager(HohoemaApp hohoemaApp)
		{
			HohoemaApp = hohoemaApp;

			Root = new RootAppMapContainer(HohoemaApp);
		}


		public async Task Refresh()
		{
			await Root.Refresh();

			foreach (var selectable in Root.SelectableItems.ToArray())
			{
				Root.Add(selectable);
			}
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
				HohoemaPageType.FavoriteManage,
				HohoemaPageType.UserMylist,
				HohoemaPageType.CacheManagement,
				HohoemaPageType.History,
			};

		public HohoemaApp HohoemaApp { get; private set; }

		public AppMapSettings AppMapSettings { get; private set; }


		public RootAppMapContainer(HohoemaApp hohoemaApp) 
			: base(HohoemaPageType.Portal)
		{
			HohoemaApp = hohoemaApp;
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
					UpdateFromAppSettings(container);

					items.Add(container);
				}
			}

//			return Task.FromResult(items.AsEnumerable());
			return items;
		}


		private void UpdateFromAppSettings(IAppMapContainer container)
		{
			switch (container.PageType)
			{
				case HohoemaPageType.RankingCategoryList:
					var selectableContainer = container as ISelectableAppMapContainer;
					foreach (var cat in HohoemaApp.UserSettings.RankingSettings.HighPriorityCategory)
					{
						var parameter = cat.ToParameterString();
						var item = selectableContainer.AllItems.SingleOrDefault(x => x.Parameter == parameter);
						if (item != null)
						{
							selectableContainer.Add(item);
						}
					}
					break;
				case HohoemaPageType.UserMylist:
					var userMylistContainer = container as ISelectableAppMapContainer;
					foreach (var item in userMylistContainer.AllItems.ToArray())
					{
						userMylistContainer.Add(item);
					}
					break;
				case HohoemaPageType.FavoriteManage:
					var favContainer = container as ISelectableAppMapContainer;
					foreach (var item in favContainer.AllItems.ToArray())
					{
						favContainer.Add(item);
					}
					break;
				case HohoemaPageType.History:
					break;
				case HohoemaPageType.Search:
					break;
				case HohoemaPageType.CacheManagement:
					break;
				case HohoemaPageType.FeedGroupManage:
					var feedGroupContainer = container as ISelectableAppMapContainer;
					foreach (var item in feedGroupContainer.AllItems.ToArray())
					{
						feedGroupContainer.Add(item);
					}
					break;
			}
		}

		private IAppMapContainer PageTypeToContainer(HohoemaPageType pageType)
		{
			IAppMapContainer container = null;
			switch (pageType)
			{
				case HohoemaPageType.RankingCategoryList:
					container = new RankingCategoriesAppMapContainer(HohoemaApp.UserSettings.RankingSettings);
					break;
				case HohoemaPageType.UserMylist:
					container = new UserMylistAppMapContainer(HohoemaApp.UserMylistManager);
					break;
				case HohoemaPageType.FavoriteManage:
					container = new FavAppMapContainer(HohoemaApp.FavManager);
					break;
				case HohoemaPageType.History:
					container = new VideoPlayHistoryAppMapContainer(HohoemaApp);
					break;
				case HohoemaPageType.Search:
					container = new SearchAppMapContainer();
					break;
				case HohoemaPageType.CacheManagement:
					container = new CachedVideoAppMapContainer(HohoemaApp);
					break;
				case HohoemaPageType.FeedGroupManage:
					container = new FeedAppMapContainer(HohoemaApp.FeedManager);
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

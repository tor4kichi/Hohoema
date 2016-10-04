using Mntone.Nico2.Videos.Ranking;
using NicoPlayerHohoema.Models.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace NicoPlayerHohoema.Models.AppMap
{
	public class RankingCategoriesAppMapContainer : SelectableAppMapContainerBase		
	{

		public RankingSettings RankingSettings { get; private set; }

		public RankingCategoriesAppMapContainer(RankingSettings rankingSettings)
			: base(HohoemaPageType.RankingCategoryList, label:"ランキング")
		{
			RankingSettings = rankingSettings;
		}

		public override ContainerItemDisplayType ItemDisplayType => ContainerItemDisplayType.Card;


		protected override Task<IEnumerable<IAppMapItem>> MakeAllItems()
		{
			var rankingCategories = Enum.GetValues(typeof(RankingCategory)).Cast<RankingCategory>();
				

			List<IAppMapItem> items = new List<IAppMapItem>();

			foreach (var cat in rankingCategories)
			{
				items.Add(new RankingCategoryAppMapItem(cat));
			}

//			foreach (var custom in RankingSettings.HighPriorityCategory.Where(x => x.RankingSource == RankingSource.SearchWithMostPopular))
//			{
//				items.Add(new RankingCategoryAppMapItem(custom.Parameter));
//			}

			return Task.FromResult(items.AsEnumerable());
		}
	}



	public class RankingCategoryAppMapItem : IAppMapItem
	{
		public string PrimaryLabel { get; private set; }
		public string SecondaryLabel => null;

		public HohoemaPageType PageType => HohoemaPageType.RankingCategory;
		public string Parameter { get; private set; }


		public RankingCategoryAppMapItem(RankingCategory cat)
		{
			PrimaryLabel = Util.RankingCategoryExtention.ToCultulizedText(cat);
			Parameter = new RankingCategoryInfo()
			{
				RankingSource = RankingSource.CategoryRanking,
				Parameter = cat.ToString(),
				DisplayLabel = PrimaryLabel
			}.ToParameterString();
				
		}

		public RankingCategoryAppMapItem(string keyword)
		{
			PrimaryLabel = keyword;
			Parameter = new RankingCategoryInfo()
			{
				RankingSource = RankingSource.SearchWithMostPopular,
				Parameter = keyword,
				DisplayLabel = PrimaryLabel
			}.ToParameterString();

		}
	}
}

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
		public RankingCategoriesAppMapContainer()
			: base(HohoemaPageType.RankingCategoryList, label:"ランキング")
		{
		}

		protected override Task<IEnumerable<IAppMapItem>> MakeAllItems()
		{
			var rankingCategories = Enum.GetValues(typeof(RankingCategory)).Cast<RankingCategory>();
				

			List<IAppMapItem> items = new List<IAppMapItem>();

			foreach (var cat in rankingCategories)
			{
				items.Add(new RankingCategoryAppMapItem(cat));
			}

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
	}
}

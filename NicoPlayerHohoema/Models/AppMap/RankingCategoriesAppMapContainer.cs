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
	public class RankingCategoriesAppMapContainer : AppMapContainerBase
    {

		public RankingSettings RankingSettings { get; private set; }

		public RankingCategoriesAppMapContainer()
			: base(HohoemaPageType.RankingCategoryList, label:"ランキング")
		{
			RankingSettings = HohoemaApp.UserSettings.RankingSettings;
		}

		public override ContainerItemDisplayType ItemDisplayType => ContainerItemDisplayType.Card;

        protected override Task OnRefreshing()
        {
            _DisplayItems.Clear();

            foreach (var cat in RankingSettings.HighPriorityCategory)
            {
                var parameter = cat.ToParameterString();
                var item = new RankingCategoryAppMapItem(cat.Category);
                if (item != null)
                {
                    _DisplayItems.Add(item);
                }
            }

            return Task.CompletedTask;
        }
	}



	public class RankingCategoryAppMapItem : IAppMapItem
	{
		public string PrimaryLabel { get; private set; }
		public string SecondaryLabel => null;

		public HohoemaPageType PageType => HohoemaPageType.RankingCategory;
		public string Parameter { get; private set; }

        public void SelectedAction()
        {

        }

		public RankingCategoryAppMapItem(RankingCategory cat)
		{
			PrimaryLabel = Util.RankingCategoryExtention.ToCultulizedText(cat);
			Parameter = new RankingCategoryInfo(cat)
                .ToParameterString();
		}
	}
}

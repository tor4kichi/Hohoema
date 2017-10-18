using Mntone.Nico2.Videos.Ranking;
using NicoPlayerHohoema.Helpers;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Collections;

namespace NicoPlayerHohoema.Models
{
	[DataContract]
	public class RankingSettings : SettingsBase
	{
		public RankingSettings()
			: base()
		{
			HighPriorityCategory = new ObservableCollection<RankingCategoryInfo>();
			MiddlePriorityCategory = new ObservableCollection<RankingCategoryInfo>();
			LowPriorityCategory = new ObservableCollection<RankingCategoryInfo>();
		}


		public void ResetCategoryPriority()
		{
			HighPriorityCategory.Clear();
			MiddlePriorityCategory.Clear();
			LowPriorityCategory.Clear();

			var types = (IEnumerable<RankingCategory>)Enum.GetValues(typeof(RankingCategory));
			foreach (var type in types)
			{
				MiddlePriorityCategory.Add(RankingCategoryInfo.CreateFromRankingCategory(type));
			}

            Save().ConfigureAwait(false);
		}


		public override void OnInitialize()
		{
			ResetCategoryPriority();
		}

		protected override void Validate()
		{

		}





		private RankingTarget _Target;

		[DataMember]
		public RankingTarget Target
		{
			get
			{
				return _Target;
			}
			set
			{
				SetProperty(ref _Target, value);
			}
		}


		private RankingTimeSpan _TimeSpan;

		[DataMember]
		public RankingTimeSpan TimeSpan
		{
			get
			{
				return _TimeSpan;
			}
			set
			{
				SetProperty(ref _TimeSpan, value);
			}
		}




		private RankingCategory _Category;

		[DataMember]
		public RankingCategory Category
		{
			get
			{
				return _Category;
			}
			set
			{
				SetProperty(ref _Category, value);
			}
		}

		[DataMember]
		public ObservableCollection<RankingCategoryInfo> HighPriorityCategory { get; private set; }

		[DataMember]
		public ObservableCollection<RankingCategoryInfo> MiddlePriorityCategory { get; private set; }

		[DataMember]
		public ObservableCollection<RankingCategoryInfo> LowPriorityCategory { get; private set; }




        public bool AddFavoritCategory(RankingCategory category)
        {
            var target = MiddlePriorityCategory.FirstOrDefault(x => x.Category == category);
            if (target != null)
            {
                MiddlePriorityCategory.Remove(target);
                HighPriorityCategory.Add(target);
                return true;
            }
            else if ((target = LowPriorityCategory.FirstOrDefault(x => x.Category == category)) != null)
            {
                LowPriorityCategory.Remove(target);
                HighPriorityCategory.Add(target);
                return true;
            }

            return false;
        }

        public bool AddDislikeCategory(RankingCategory category)
        {
            var target = MiddlePriorityCategory.FirstOrDefault(x => x.Category == category);
            if (target != null)
            {
                MiddlePriorityCategory.Remove(target);
                LowPriorityCategory.Add(target);
                return true;
            }
            else if ((target = HighPriorityCategory.FirstOrDefault(x => x.Category == category)) != null)
            {
                HighPriorityCategory.Remove(target);
                LowPriorityCategory.Add(target);
                return true;
            }

            return false;
        }

        public void ResetFavoriteCategory()
        {
            var items = HighPriorityCategory.ToArray();
            HighPriorityCategory.Clear();
            foreach (var item in items)
            {
                MiddlePriorityCategory.Add(item);
            }

            Save().ConfigureAwait(false);
        }

        public void ResetDislikeCategory()
        {
            var items = LowPriorityCategory.ToArray();
            LowPriorityCategory.Clear();
            foreach (var item in items)
            {
                MiddlePriorityCategory.Add(item);
            }

            Save().ConfigureAwait(false);
        }


        public bool IsDislikeRankingCategory(RankingCategory category)
		{
			var categoryString = category.ToString();
			return LowPriorityCategory.Any(x => x.Parameter == categoryString);
		}

	}

	public class RankingCategoryInfo : BindableBase, IEquatable<RankingCategoryInfo>
	{

		public static RankingCategoryInfo CreateFromRankingCategory(RankingCategory cat)
		{
            return new RankingCategoryInfo(cat);
        }


		public RankingCategoryInfo(RankingCategory category)
		{
            Category = category;
            Parameter = category.ToString();
            DisplayLabel = category.ToCultulizedText();
        }

        public string ToParameterString()
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject(this);
		}

		public static RankingCategoryInfo FromParameterString(string json)
		{
			return Newtonsoft.Json.JsonConvert.DeserializeObject<RankingCategoryInfo>(json);
		}

        public RankingCategory Category { get; private set; }

		private string _Parameter;
		public string Parameter
		{
			get { return _Parameter; }
			set
            {
                if (SetProperty(ref _Parameter, value))
                {
                    Category = (RankingCategory)Enum.Parse(typeof(RankingCategory), _Parameter);
                }
            }

		}

		private string _DisplayLabel;
		public string DisplayLabel
		{
			get { return _DisplayLabel; }
			set { SetProperty(ref _DisplayLabel, value); }
		}

		public bool Equals(RankingCategoryInfo other)
		{
			if (other == null) { return false; }

			return this.Parameter == other.Parameter;
		}
	}
}

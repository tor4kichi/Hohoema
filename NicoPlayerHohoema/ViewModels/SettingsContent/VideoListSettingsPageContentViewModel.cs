using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using Reactive.Bindings.Extensions;
using Prism.Commands;
using System.Reactive.Linq;
using System.Collections.ObjectModel;
using Reactive.Bindings;
using Prism.Mvvm;

namespace NicoPlayerHohoema.ViewModels
{
	public class VideoListSettingsPageContentViewModel : SettingsPageContentViewModel
	{
		public VideoListSettingsPageContentViewModel(HohoemaApp hohoemaApp, string title)
			: base(title)
		{
			_HohoemaApp = hohoemaApp;
			_NGSettings = _HohoemaApp.UserSettings.NGSettings;
			_RankingSettings = _HohoemaApp.UserSettings.RankingSettings;

			HandSortableCategories = new ObservableCollection<HandSortableCategoryListItemBase>();

			ResetCategoryPriority();



			// Dividerの順序をConstraintPositionによって拘束する

			HandSortableCategories.CollectionChangedAsObservable()
				// CollectionChangedイベントの中ではコレクションの変更ができないため、
				// 実行コンテキストを切り離すため、Delayをはさむ
				.Delay(TimeSpan.FromMilliseconds(50), UIDispatcherScheduler.Default)
				.Subscribe(x =>
				{
					if (x.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
					{
						TryCorrectDividerPlacement();
					}
				});


			CategoryPriorityResetCommand = new DelegateCommand(() =>
			{
				_RankingSettings.ResetCategoryPriority();

				_RankingSettings.Save();

				ResetCategoryPriority();
			});

			AddCustomRankingCategory = new DelegateCommand(() =>
			{
				HandSortableCategories.Insert(0,
					new HandSortableUserCategoryListItem(
						RankingCategoryInfo.CreateUserCustomizedRanking()
						, this
						)
					);
			});



			// NG Video


			// NG Video
			NGVideoIdEnable = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGVideoIdEnable);
			NGVideoIds = _NGSettings.NGVideoIds
				.ToReadOnlyReactiveCollection(x =>
					RemovableSettingsListItemHelper.VideoIdInfoToRemovableListItemVM(x, OnRemoveNGVideoIdFromList)
					);

			// NG Video Owner User Id
			NGVideoOwnerUserIdEnable = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGVideoOwnerUserIdEnable);
			NGVideoOwnerUserIds = _NGSettings.NGVideoOwnerUserIds
				.ToReadOnlyReactiveCollection(x =>
					RemovableSettingsListItemHelper.UserIdInfoToRemovableListItemVM(x, OnRemoveNGVideoOwnerUserIdFromList)
					);

			// NG Keyword on Video Title
			NGVideoTitleKeywordEnable = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGVideoTitleKeywordEnable);
			NGVideoTitleKeywords = _NGSettings.NGVideoTitleKeywords.ToReadOnlyReactiveCollection(
				x => new NGKeywordViewModel(x, OnRemoveNGTitleKeyword)
				);

			// NG動画タイトルキーワードを追加するコマンド
			AddNewNGVideoTitleKeywordCommand = new DelegateCommand(() =>
			{
				_NGSettings.NGVideoTitleKeywords.Add(new NGKeyword()
				{
					TestText = "",
					Keyword = ""
				});
			});

		}


		public override void OnLeave()
		{
			ApplyAllPriorityCategoriesToRankingSettings();

			_RankingSettings.Save();
		}

		private void ResetCategoryPriority()
		{
			HandSortableCategories.Clear();

			foreach (var catInfo in _RankingSettings.HighPriorityCategory)
			{
				HandSortableCategories.Add(CategoryInfoToVM(catInfo));
			}

			HandSortableCategories.Add(new DividerHandSortableCategoryListItem()
			{
				ConstraintPosition = 0,
				AborbText = "▲優先▲",
				BelowText = "▼通常▼"
			});

			foreach (var catInfo in _RankingSettings.MiddlePriorityCategory)
			{
				HandSortableCategories.Add(CategoryInfoToVM(catInfo));
			}


			HandSortableCategories.Add(new DividerHandSortableCategoryListItem()
			{
				ConstraintPosition = 1,
				AborbText = "▲通常▲",
				BelowText = "▼あまり見ない▼"
			});

			foreach (var catInfo in _RankingSettings.LowPriorityCategory)
			{
				HandSortableCategories.Add(CategoryInfoToVM(catInfo));
			}
		}


		private HandSortableCategoryListItem CategoryInfoToVM(RankingCategoryInfo info)
		{
			switch (info.RankingSource)
			{
				case RankingSource.CategoryRanking:
					return new HandSortableCategoryListItem(info);
				case RankingSource.SearchWithMostPopular:
					return new HandSortableUserCategoryListItem(info, this);
				default:
					throw new NotSupportedException($"not support {nameof(RankingSource)}.{info.RankingSource.ToString()}");
			}
		}

		bool _NowTryingCorrectalizeDividers = false;
		private void TryCorrectDividerPlacement()
		{
			if (_NowTryingCorrectalizeDividers)
			{
				return;
			}

			_NowTryingCorrectalizeDividers = true;

			while (!IsCorrectDividerSequence())
			{
				var dividers = HandSortableCategories.Where(x => x is DividerHandSortableCategoryListItem)
					.Cast<DividerHandSortableCategoryListItem>()
					.ToList();

				var dividerCount = dividers.Count();
				for (int i = 0; i < dividerCount; ++i)
				{
					var div = dividers.ElementAt(i);
					if (div.ConstraintPosition != i)
					{
						var targetDiv = dividers.ElementAt((int)div.ConstraintPosition);

						HandSortableCategories.Remove(div);

						var index = HandSortableCategories.IndexOf(targetDiv);
						HandSortableCategories.Insert(index + 1, div);
						break;
					}
				}
			}

			_NowTryingCorrectalizeDividers = false;
		}

		private bool IsCorrectDividerSequence()
		{
			var dividers = HandSortableCategories.Where(x => x is DividerHandSortableCategoryListItem)
				.Cast<DividerHandSortableCategoryListItem>();

			var dividerCount = dividers.Count();
			for (uint i = 0; i < dividerCount; ++i)
			{
				if (dividers.ElementAt((int)i).ConstraintPosition != i)
				{
					return false;
				}
			}

			return true;
		}


		internal void RemoveUserCustomizedRankingCategory(HandSortableUserCategoryListItem userListItem)
		{
			this.HandSortableCategories.Remove(userListItem);
		}

		private void ApplyAllPriorityCategoriesToRankingSettings()
		{
			var sourceList = HandSortableCategories.Distinct().ToList();

			var highGroup = HandSortableCategories
				.TakeWhile(x => !(x is DividerHandSortableCategoryListItem))
				.Where(x => x is HandSortableCategoryListItem)
				.ToList();

			var lowGroup = HandSortableCategories.Reverse()
				.TakeWhile(x => !(x is DividerHandSortableCategoryListItem))
				.Where(x => x is HandSortableCategoryListItem)
				.ToList();

			var middleGroup = HandSortableCategories
				.Except(highGroup)
				.Except(lowGroup)
				.Where(x => x is HandSortableCategoryListItem);


			_RankingSettings.HighPriorityCategory.Clear();
			foreach (var highPrioCat in highGroup.Cast<HandSortableCategoryListItem>())
			{
				_RankingSettings.HighPriorityCategory.Add(highPrioCat.CategoryInfo);
			}


			_RankingSettings.MiddlePriorityCategory.Clear();
			foreach (var midPrioCat in middleGroup.Cast<HandSortableCategoryListItem>())
			{
				_RankingSettings.MiddlePriorityCategory.Add(midPrioCat.CategoryInfo);
			}

			_RankingSettings.LowPriorityCategory.Clear();
			foreach (var lowPrioCat in lowGroup.Cast<HandSortableCategoryListItem>())
			{
				_RankingSettings.LowPriorityCategory.Add(lowPrioCat.CategoryInfo);
			}
		}

		

		private void OnRemoveNGVideoIdFromList(string videoId)
		{
			var removeTarget = _NGSettings.NGVideoIds.First(x => x.VideoId == videoId);
			_NGSettings.NGVideoIds.Remove(removeTarget);
		}


		private void OnRemoveNGVideoOwnerUserIdFromList(string userId)
		{
			var removeTarget = _NGSettings.NGVideoOwnerUserIds.First(x => x.UserId == userId);
			_NGSettings.NGVideoOwnerUserIds.Remove(removeTarget);
		}

		internal void OnRemoveNGTitleKeyword(NGKeyword keywordInfo)
		{
			_NGSettings.NGVideoTitleKeywords.Remove(keywordInfo);
		}


		
		public DelegateCommand AddCustomRankingCategory { get; private set; }

		public DelegateCommand CategoryPriorityResetCommand { get; private set; }

		public ObservableCollection<HandSortableCategoryListItemBase> HandSortableCategories { get; private set; }


		public DelegateCommand AddNewNGVideoTitleKeywordCommand { get; private set; }

		public ReactiveProperty<bool> NGVideoIdEnable { get; private set; }
		public ReadOnlyReactiveCollection<RemovableListItem<string>> NGVideoIds { get; private set; }

		public ReactiveProperty<bool> NGVideoOwnerUserIdEnable { get; private set; }
		public ReadOnlyReactiveCollection<RemovableListItem<string>> NGVideoOwnerUserIds { get; private set; }

		public ReactiveProperty<bool> NGVideoTitleKeywordEnable { get; private set; }
		public ReadOnlyReactiveCollection<NGKeywordViewModel> NGVideoTitleKeywords { get; private set; }



		NGSettings _NGSettings;
		RankingSettings _RankingSettings;
		HohoemaApp _HohoemaApp;
	}



	public class HandSortableCategoryListItemBase : BindableBase
	{
		public bool IsSortable { get; protected set; } = false;
	}

	public class HandSortableCategoryListItem : HandSortableCategoryListItemBase
	{
		public HandSortableCategoryListItem(RankingCategoryInfo info)
		{
			CategoryInfo = info;
			DisplayLabel = info.ToReactivePropertyAsSynchronized(x => x.DisplayLabel);
			IsSortable = true;
		}

		public ReactiveProperty<string> DisplayLabel { get; private set; }
		public RankingCategoryInfo CategoryInfo { get; set; }
	}

	public class HandSortableUserCategoryListItem : HandSortableCategoryListItem
	{
		public HandSortableUserCategoryListItem(RankingCategoryInfo info, VideoListSettingsPageContentViewModel parentVM)
			: base(info)
		{
			_ParentVM = parentVM;
			Parameter = info.ToReactivePropertyAsSynchronized(x => x.Parameter);
		}

		private DelegateCommand _RemoveUserCategoryCommand;
		public DelegateCommand RemoveUserCategoryCommand
		{
			get
			{
				return _RemoveUserCategoryCommand
					?? (_RemoveUserCategoryCommand = new DelegateCommand(() =>
					{
						_ParentVM.RemoveUserCustomizedRankingCategory(this);
					}));
			}
		}



		public ReactiveProperty<string> Parameter { get; private set; }

		VideoListSettingsPageContentViewModel _ParentVM;
	}

	public class DividerHandSortableCategoryListItem : HandSortableCategoryListItemBase
	{
		public uint ConstraintPosition { get; set; }

		public string AborbText { get; set; }
		public string BelowText { get; set; }
	}


}

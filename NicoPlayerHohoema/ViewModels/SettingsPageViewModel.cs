using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Prism.Mvvm;
using Reactive.Bindings;
using NicoPlayerHohoema.Models;
using Reactive.Bindings.Extensions;
using Prism.Commands;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Mntone.Nico2;
using System.Collections.ObjectModel;
using Mntone.Nico2.Videos.Ranking;

namespace NicoPlayerHohoema.ViewModels
{
	public class SettingsPageViewModel : ViewModelBase
	{
		public SettingsPageViewModel(HohoemaApp hohoemaApp)
		{
			HohoemaApp = hohoemaApp;
			SettingKindToVM = new Dictionary<HohoemaSettingsKind, SettingsPageContentViewModel>();

			SettingItems = ((IEnumerable<HohoemaSettingsKind>)Enum.GetValues(typeof(HohoemaSettingsKind)))
				.Select(x =>
				{
					return new HohoemaSettingsKindListItem(x, x.ToCulturelizedText());
				})
				.ToList();
			CurrentSettingsKind = new ReactiveProperty<HohoemaSettingsKindListItem>(SettingItems[0]);

			
			CurrentSettingsContent = CurrentSettingsKind
				.Select(x => KindToVM(x.Kind, x.Label))
				.Do(x =>
				{
					CurrentSettingsContent?.Value?.OnLeave();
					x?.OnEnter();
				})
				.ToReactiveProperty();
		}



		private SettingsPageContentViewModel KindToVM(HohoemaSettingsKind kind, string title)
		{
			SettingsPageContentViewModel vm = null;
			if (SettingKindToVM.ContainsKey(kind))
			{
				vm = SettingKindToVM[kind];
			}
			else
			{
				switch (kind)
				{
					case HohoemaSettingsKind.Ranking:
						vm = new RankingSettingsPageContentViewModel(HohoemaApp, title);
						break;
					case HohoemaSettingsKind.NG:
						vm = new NGSettingsPageContentViewModel(HohoemaApp, title);
						break;
					case HohoemaSettingsKind.MediaPlayer:
						vm = new PlayerSettingsPageContentViewModel(HohoemaApp, title);
						break;
					case HohoemaSettingsKind.Performance:
						vm = new PerformanceSettingsPageContentViewModel(HohoemaApp, title);
						break;
					default:
						break;
				}

				if (vm != null)
				{
					SettingKindToVM.Add(kind, vm);
				}
			}

			return vm;
		}


		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			HohoemaSettingsKind? selectRequestKind = null;

			if (e.Parameter is HohoemaSettingsKind)
			{
				selectRequestKind = (HohoemaSettingsKind)e.Parameter;
			}
			else if (viewModelState.ContainsKey(nameof(CurrentSettingsKind)))
			{
				selectRequestKind = (HohoemaSettingsKind)viewModelState[nameof(CurrentSettingsKind)];
			}


			if (selectRequestKind.HasValue)
			{
				var settingItem = SettingItems.Single(x => x.Kind == selectRequestKind);
				CurrentSettingsKind.Value = settingItem;
			}

			base.OnNavigatedTo(e, viewModelState);
		}

		public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			CurrentSettingsContent.Value?.OnLeave();

			if (suspending)
			{
				viewModelState.Add(nameof(CurrentSettingsKind), CurrentSettingsKind.Value.Kind);
			}

			base.OnNavigatingFrom(e, viewModelState, suspending);
		}





		public Dictionary<HohoemaSettingsKind, SettingsPageContentViewModel> SettingKindToVM { get; private set; }
		public ReactiveProperty<HohoemaSettingsKindListItem> CurrentSettingsKind { get; private set; }
		public ReactiveProperty<SettingsPageContentViewModel> CurrentSettingsContent { get; private set; }

		public List<HohoemaSettingsKindListItem> SettingItems { get; private set; }


		public HohoemaApp HohoemaApp { get; private set; }
	}


	public enum HohoemaSettingsKind
	{
		Ranking,
		NG,
		MediaPlayer,
		Performance,
	}


	public static class HohoemaSettingsKindExtention
	{
		public static string ToCulturelizedText(this HohoemaSettingsKind kind)
		{
			switch (kind)
			{
				case HohoemaSettingsKind.Ranking:
					return "ランキング";
				case HohoemaSettingsKind.NG:
					return "NG";
				case HohoemaSettingsKind.MediaPlayer:
					return "動画プレイヤー";
				case HohoemaSettingsKind.Performance:
					return "パフォーマンス";
				default:
					throw new NotSupportedException($"not support {nameof(HohoemaSettingsKind)}.{kind.ToString()}");
			}
		}
	}

	public class HohoemaSettingsKindListItem
	{
		public HohoemaSettingsKind Kind { get; private set; }
		public string Label { get; private set; }

		public HohoemaSettingsKindListItem(HohoemaSettingsKind kind, string label)
		{
			Kind = kind;
			Label = label;
		}
	}


	public abstract class SettingsPageContentViewModel : ViewModelBase
	{
		public string Title { get; private set; }

		public SettingsPageContentViewModel(string title)
		{
			Title = title;
		}


		virtual public void OnEnter() { }

		abstract public void OnLeave();

	}

	


	public class RankingSettingsPageContentViewModel : SettingsPageContentViewModel
	{
		public RankingSettingsPageContentViewModel(HohoemaApp hohoemaApp, string title)
			: base(title)
		{
			_HohoemaApp = hohoemaApp;

			HandSortableCategories = new ObservableCollection<HandSortableCategoryListItemBase>();

			_RankingSettings = _HohoemaApp.UserSettings.RankingSettings;

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
		}


		private void ResetCategoryPriority()
		{
			HandSortableCategories.Clear();

			foreach (var highPrioCat in _RankingSettings.HighPriorityCategory)
			{
				HandSortableCategories.Add(new HandSortableCategoryListItem()
				{
					Label = highPrioCat.ToCultulizedText(),
					Category = highPrioCat
				});
			}

			HandSortableCategories.Add(new DividerHandSortableCategoryListItem()
			{
				ConstraintPosition = 0,
				AborbText = "優先",
				BelowText = "通常"
			});

			foreach (var highPrioCat in _RankingSettings.MiddlePriorityCategory)
			{
				HandSortableCategories.Add(new HandSortableCategoryListItem()
				{
					Label = highPrioCat.ToCultulizedText(),
					Category = highPrioCat
				});
			}


			HandSortableCategories.Add(new DividerHandSortableCategoryListItem()
			{
				ConstraintPosition = 1,
				AborbText = "通常",
				BelowText = "非表示"
			});

			foreach (var highPrioCat in _RankingSettings.LowPriorityCategory)
			{
				HandSortableCategories.Add(new HandSortableCategoryListItem()
				{
					Label = highPrioCat.ToCultulizedText(),
					Category = highPrioCat
				});
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
			
			while(!IsCorrectDividerSequence())
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
			for (uint i = 0; i < dividerCount; ++i )
			{
				if (dividers.ElementAt((int)i).ConstraintPosition != i)
				{
					return false;
				}
			}

			return true;
		}


		public override void OnLeave()
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
			foreach (var highPrioCat in highGroup.Cast< HandSortableCategoryListItem>()) 
			{
				_RankingSettings.HighPriorityCategory.Add(highPrioCat.Category);
			}


			_RankingSettings.MiddlePriorityCategory.Clear();
			foreach (var midPrioCat in middleGroup.Cast<HandSortableCategoryListItem>())
			{
				_RankingSettings.MiddlePriorityCategory.Add(midPrioCat.Category);
			}

			_RankingSettings.LowPriorityCategory.Clear();
			foreach (var lowPrioCat in lowGroup.Cast<HandSortableCategoryListItem>())
			{
				_RankingSettings.LowPriorityCategory.Add(lowPrioCat.Category);
			}


			_RankingSettings.Save();
		}


		public DelegateCommand CategoryPriorityResetCommand { get; private set; }

		public ObservableCollection<HandSortableCategoryListItemBase> HandSortableCategories { get; private set; }

		RankingSettings _RankingSettings;
		HohoemaApp _HohoemaApp;
	}



	public class HandSortableCategoryListItemBase 
	{
		public bool IsSortable { get; protected set; }
	}

	public class HandSortableCategoryListItem : HandSortableCategoryListItemBase
	{
		public HandSortableCategoryListItem()
		{
			IsSortable = true;
		}

		public string Label { get; set; }
		public RankingCategory Category { get; set; }
	}

	public class DividerHandSortableCategoryListItem : HandSortableCategoryListItemBase
	{
		public uint ConstraintPosition { get; set; }

		public string AborbText { get; set; }
		public string BelowText { get; set; }
	}


	public class NGSettingsPageContentViewModel : SettingsPageContentViewModel
	{
		public NGSettingsPageContentViewModel(HohoemaApp hohoemaApp, string title)
			: base(title)
		{

		}

		public override void OnLeave()
		{
			throw new NotImplementedException();
		}
	}


	public class PlayerSettingsPageContentViewModel : SettingsPageContentViewModel
	{
		public PlayerSettingsPageContentViewModel(HohoemaApp hohoemaApp, string title)
			: base(title)
		{

		}

		public override void OnLeave()
		{
			throw new NotImplementedException();
		}

	}

	public class PerformanceSettingsPageContentViewModel : SettingsPageContentViewModel
	{
		public PerformanceSettingsPageContentViewModel(HohoemaApp hohoemaApp, string title)
			: base(title)
		{

		}

		public override void OnLeave()
		{
			throw new NotImplementedException();
		}

	}
}

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

			AddCustomRankingCategory = new DelegateCommand(() => 
			{
				HandSortableCategories.Insert(0, 
					new HandSortableUserCategoryListItem(
						RankingCategoryInfo.CreateUserCustomizedRanking()
						, this
						)
					);
			});
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
				AborbText = "優先",
				BelowText = "通常"
			});

			foreach (var catInfo in _RankingSettings.MiddlePriorityCategory)
			{
				HandSortableCategories.Add(CategoryInfoToVM(catInfo));
			}


			HandSortableCategories.Add(new DividerHandSortableCategoryListItem()
			{
				ConstraintPosition = 1,
				AborbText = "通常",
				BelowText = "非表示"
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

		public override void OnLeave()
		{
			ApplyAllPriorityCategoriesToRankingSettings();

			_RankingSettings.Save();
		}




		public DelegateCommand AddCustomRankingCategory { get; private set; }

		public DelegateCommand CategoryPriorityResetCommand { get; private set; }

		public ObservableCollection<HandSortableCategoryListItemBase> HandSortableCategories { get; private set; }

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
		public HandSortableUserCategoryListItem(RankingCategoryInfo info, RankingSettingsPageContentViewModel parentVM)
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

		RankingSettingsPageContentViewModel _ParentVM;
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
			_HohoemaApp = hohoemaApp;
			_NGSettings = _HohoemaApp.UserSettings.NGSettings;

			// NG Video
			NGVideoIdEnable = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGVideoIdEnable);
			NGVideoIds = _NGSettings.NGVideoIds
				.ToReadOnlyReactiveCollection(x => 
					IdInfoToRemovableListItemVM(x, OnRemoveNGVideoIdFromList)
					);

			// NG Video Owner User Id
			NGVideoOwnerUserIdEnable = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGVideoOwnerUserIdEnable);
			NGVideoOwnerUserIds = _NGSettings.NGVideoOwnerUserIds
				.ToReadOnlyReactiveCollection(x =>
					IdInfoToRemovableListItemVM(x, OnRemoveNGVideoOwnerUserIdFromList)
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






			// NG Comment User Id
			NGCommentUserIdEnable = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGCommentUserIdEnable);
			NGCommentUserIds = _NGSettings.NGCommentUserIds
				.ToReadOnlyReactiveCollection(x =>
					IdInfoToRemovableListItemVM(x, OnRemoveNGCommentUserIdFromList)
					);

			NGCommentKeywordEnable = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGCommentKeywordEnable);
			NGCommentKeywords = _NGSettings.NGCommentKeywords.ToReadOnlyReactiveCollection(
				x => new NGKeywordViewModel(x, OnRemoveNGCommentKeyword)
				);


			NGCommentGlassMowerEnable = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGCommentGlassMowerEnable);


			NGCommentScoreTypes = ((IEnumerable<NGCommentScore>)Enum.GetValues(typeof(NGCommentScore))).ToList();

			SelectedNGCommentScore = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGCommentScoreType);

			AddNewNGCommentKeywordCommand = new DelegateCommand(() => 
			{
				_NGSettings.NGCommentKeywords.Add(new NGKeyword()
				{
					TestText = "",
					Keyword = ""
				});
			});
		}


		private void OnRemoveNGVideoIdFromList(uint videoId)
		{
			var removeTarget = _NGSettings.NGVideoIds.First(x => x.Id == videoId);
			_NGSettings.NGVideoIds.Remove(removeTarget);
		}


		private void OnRemoveNGVideoOwnerUserIdFromList(uint userId)
		{
			var removeTarget = _NGSettings.NGVideoOwnerUserIds.First(x => x.Id == userId);
			_NGSettings.NGVideoOwnerUserIds.Remove(removeTarget);
		}

		internal void OnRemoveNGTitleKeyword(NGKeyword keywordInfo)
		{
			_NGSettings.NGVideoTitleKeywords.Remove(keywordInfo);
		}


		internal void OnRemoveNGCommentKeyword(NGKeyword keywordInfo)
		{
			_NGSettings.NGCommentKeywords.Remove(keywordInfo);
		}

		private void OnRemoveNGCommentUserIdFromList(uint userId)
		{
			var removeTarget = _NGSettings.NGCommentUserIds.First(x => x.Id == userId);
			_NGSettings.NGCommentUserIds.Remove(removeTarget);
		}


		private RemovableListItem<uint> IdInfoToRemovableListItemVM(IdInfo info, Action<uint> removeAction)
		{
			var roundedDesc = info.Description.Substring(0, Math.Min(info.Description.Length - 1, 10));
			return new RemovableListItem<uint>(info.Id, $"{info.Id} | {roundedDesc}", removeAction);
		}

		public override void OnLeave()
		{
			_NGSettings.Save();
		}


		

		public DelegateCommand AddNewNGVideoTitleKeywordCommand { get; private set; }

		public ReactiveProperty<bool> NGVideoIdEnable { get; private set; }
		public ReadOnlyReactiveCollection<RemovableListItem<uint>> NGVideoIds { get; private set; }

		public ReactiveProperty<bool> NGVideoOwnerUserIdEnable { get; private set; }
		public ReadOnlyReactiveCollection<RemovableListItem<uint>> NGVideoOwnerUserIds { get; private set; }

		public ReactiveProperty<bool> NGVideoTitleKeywordEnable { get; private set; }
		public ReadOnlyReactiveCollection<NGKeywordViewModel> NGVideoTitleKeywords { get; private set; }


		public DelegateCommand AddNewNGCommentKeywordCommand { get; private set; }

		public ReactiveProperty<bool> NGCommentUserIdEnable { get; private set; }
		public ReadOnlyReactiveCollection<RemovableListItem<uint>> NGCommentUserIds { get; private set; }

		public ReactiveProperty<bool> NGCommentKeywordEnable { get; private set; }
		public ReadOnlyReactiveCollection<NGKeywordViewModel> NGCommentKeywords { get; private set; }

		public ReactiveProperty<bool> NGCommentGlassMowerEnable { get; private set; }

		public List<NGCommentScore> NGCommentScoreTypes { get; private set; }
		public ReactiveProperty<NGCommentScore> SelectedNGCommentScore { get; private set; }


		NGSettings _NGSettings;
		HohoemaApp _HohoemaApp;
	}


	public class RemovableListItem<T>
	{
		public T Source { get; private set; }
		public Action<T> OnRemove { get; private set; }

		public string Content { get; private set; }
		public RemovableListItem(T source, string content, Action<T> onRemovedAction)
		{
			Source = source;
			Content = content;
			OnRemove = onRemovedAction;

			RemoveCommand = new DelegateCommand(() => 
			{
				onRemovedAction(Source);
			});
		}


		public DelegateCommand RemoveCommand { get; private set; }
	}


	public class NGKeywordViewModel : IDisposable
	{
		public NGKeywordViewModel(NGKeyword ngTitleInfo, Action<NGKeyword> onRemoveAction)
		{
			_NGKeywordInfo = ngTitleInfo;
			_OnRemoveAction = onRemoveAction;

			TestText = new ReactiveProperty<string>(_NGKeywordInfo.TestText);
			Keyword = new ReactiveProperty<string>(_NGKeywordInfo.Keyword);

			TestText.Subscribe(x => 
			{
				_NGKeywordInfo.TestText = x;
			});

			Keyword.Subscribe(x =>
			{
				_NGKeywordInfo.Keyword = x;
			});

			IsValidKeyword =
				Observable.CombineLatest(
					TestText,
					Keyword
					)
					.Where(x => x[0].Length > 0)
					.Select(x =>
					{
						var result = -1 != TestText.Value.IndexOf(Keyword.Value);
						return result;
					})
					.ToReactiveProperty();

			IsInvalidKeyword = IsValidKeyword.Select(x => !x)
				.ToReactiveProperty();

			RemoveKeywordCommand = new DelegateCommand(() => 
			{
				_OnRemoveAction(this._NGKeywordInfo);
			});
		}

		public void Dispose()
		{
			TestText?.Dispose();
			Keyword?.Dispose();

			IsValidKeyword?.Dispose();
			IsInvalidKeyword?.Dispose();
			
		}


		public ReactiveProperty<string> TestText { get; private set; }
		public ReactiveProperty<string> Keyword { get; private set; }

		public ReactiveProperty<bool> IsValidKeyword { get; private set; }
		public ReactiveProperty<bool> IsInvalidKeyword { get; private set; }


		public DelegateCommand RemoveKeywordCommand { get; private set; }
		
		NGKeyword _NGKeywordInfo;
		Action<NGKeyword> _OnRemoveAction;
	}



	public class PlayerSettingsPageContentViewModel : SettingsPageContentViewModel
	{
		public PlayerSettingsPageContentViewModel(HohoemaApp hohoemaApp, string title)
			: base(title)
		{

		}

		public override void OnLeave()
		{
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
		}

	}
}

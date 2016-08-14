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
using NicoPlayerHohoema.Views.Service;

namespace NicoPlayerHohoema.ViewModels
{
	public class VideoListSettingsPageContentViewModel : SettingsPageContentViewModel
	{
		public VideoListSettingsPageContentViewModel(HohoemaApp hohoemaApp, PageManager pageManager, string title, RankingChoiceDialogService rankingChoiceDialog)
			: base(title)
		{
			_HohoemaApp = hohoemaApp;
			_NGSettings = _HohoemaApp.UserSettings.NGSettings;
			_RankingSettings = _HohoemaApp.UserSettings.RankingSettings;
			_RankingChoiceDialogService = rankingChoiceDialog;


			SelectableCategories = new ObservableCollection<RankingCategorySettingsListItem>(
				_RankingSettings.MiddlePriorityCategory
				.Select(x => new RankingCategorySettingsListItem(x, this))
				.ToList()
				);

			FavCategories = new ObservableCollection<RankingCategorySettingsListItem>(
				_RankingSettings.HighPriorityCategory
				.Select(x => new RankingCategorySettingsListItem(x, this))
				.ToList()
				);


			AddFavRankingCategory = new DelegateCommand(async () =>
			{
				var items = _RankingSettings.MiddlePriorityCategory.ToArray();
				var choiceItem = await _RankingChoiceDialogService.ShowDialog(items);

				if (choiceItem != null)
				{
					if (choiceItem.RankingSource == RankingSource.CategoryRanking)
					{
						var removeTarget = SelectableCategories.SingleOrDefault(x => x.CategoryInfo == choiceItem);
						SelectableCategories.Remove(removeTarget);
					}

					FavCategories.Add(new RankingCategorySettingsListItem(choiceItem, this));
				}

				ApplyAllPriorityCategoriesToRankingSettings();
			});


			// 入れ替え説明テキストの表示フラグ
			FavCategories.ObserveProperty(x => x.Count)
				.Subscribe(x => IsDisplayReorderText = x >= 2);


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
				new SelectableItem<UserIdInfo>(x, (y) => 
					{
						pageManager.OpenPage(HohoemaPageType.UserInfo, x.UserId);
					})
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

			_RankingSettings.Save().ConfigureAwait(false);
			_NGSettings.Save().ConfigureAwait(false);
		}

		



		internal void UnfavRankingCategory(RankingCategorySettingsListItem userListItem)
		{
			FavCategories.Remove(userListItem);

			if (userListItem.CategoryInfo.RankingSource == RankingSource.CategoryRanking)
			{
				SelectableCategories.Add(userListItem);
			}

			ApplyAllPriorityCategoriesToRankingSettings();
		}

		private void ApplyAllPriorityCategoriesToRankingSettings()
		{
			_RankingSettings.HighPriorityCategory.Clear();
			foreach (var highPrioCat in FavCategories)
			{
				_RankingSettings.HighPriorityCategory.Add(highPrioCat.CategoryInfo);
			}


			_RankingSettings.MiddlePriorityCategory.Clear();
			foreach (var midPrioCat in SelectableCategories)
			{
				_RankingSettings.MiddlePriorityCategory.Add(midPrioCat.CategoryInfo);
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


		
		public DelegateCommand AddFavRankingCategory { get; private set; }

		public DelegateCommand CategoryPriorityResetCommand { get; private set; }

		public ObservableCollection<RankingCategorySettingsListItem> FavCategories { get; private set; }
		public ObservableCollection<RankingCategorySettingsListItem> SelectableCategories { get; private set; }

		private bool _IsDisplayReorderText;
		public bool IsDisplayReorderText
		{
			get { return _IsDisplayReorderText; }
			set { SetProperty(ref _IsDisplayReorderText, value); }
		}

		public DelegateCommand AddNewNGVideoTitleKeywordCommand { get; private set; }

		public ReactiveProperty<bool> NGVideoIdEnable { get; private set; }
		public ReadOnlyReactiveCollection<RemovableListItem<string>> NGVideoIds { get; private set; }

		public ReactiveProperty<bool> NGVideoOwnerUserIdEnable { get; private set; }
		public ReadOnlyReactiveCollection<SelectableItem<UserIdInfo>> NGVideoOwnerUserIds { get; private set; }

		public ReactiveProperty<bool> NGVideoTitleKeywordEnable { get; private set; }
		public ReadOnlyReactiveCollection<NGKeywordViewModel> NGVideoTitleKeywords { get; private set; }



		NGSettings _NGSettings;
		RankingSettings _RankingSettings;
		HohoemaApp _HohoemaApp;
		RankingChoiceDialogService _RankingChoiceDialogService;
	}



	

	

	public class RankingCategorySettingsListItem : BindableBase
	{
		public RankingCategorySettingsListItem(RankingCategoryInfo info, VideoListSettingsPageContentViewModel parentVM)
		{
			_ParentVM = parentVM;
			CategoryInfo = info;
			DisplayLabel = info.ToReactivePropertyAsSynchronized(x => x.DisplayLabel);
			Parameter = info.ToReactivePropertyAsSynchronized(x => x.Parameter);
		}

		private DelegateCommand _UnfavCategoryCommand;
		public DelegateCommand UnfavCategoryCommand
		{
			get
			{
				return _UnfavCategoryCommand
					?? (_UnfavCategoryCommand = new DelegateCommand(() =>
					{
						_ParentVM.UnfavRankingCategory(this);
					}));
			}
		}



		public ReactiveProperty<string> DisplayLabel { get; private set; }
		public RankingCategoryInfo CategoryInfo { get; set; }
		public ReactiveProperty<string> Parameter { get; private set; }

		VideoListSettingsPageContentViewModel _ParentVM;
	}
}

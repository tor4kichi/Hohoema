using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NicoPlayerHohoema.Models;
using Prism.Mvvm;
using Prism.Commands;
using NicoPlayerHohoema.Util;
using Reactive.Bindings;
using System.Collections.ObjectModel;
using Prism.Windows.Navigation;
using System.Reactive.Linq;
using Reactive.Bindings.Extensions;
using NicoPlayerHohoema.Views.Service;

namespace NicoPlayerHohoema.ViewModels
{
	public class FeedGroupManagePageViewModel : HohoemaViewModelBase
	{

		public ReactiveProperty<bool> IsSelectionModeEnable { get; private set; }
		public ReactiveProperty<FeedGroupListItem> SelectedFeedGroupItem { get; private set; }

		public ObservableCollection<FeedGroupListItem> FeedGroupItems { get; private set; }
		public ReadOnlyReactiveProperty<bool> HasFeedGroupItems { get; private set; }

		public ReactiveProperty<string> NewFeedGroupName { get; private set; }
		public ReactiveCommand AddFeedGroupCommand { get; private set; }


		private TextInputDialogService _TextInputDialogService;

		public FeedGroupManagePageViewModel(HohoemaApp hohoemaApp, PageManager pageManager, TextInputDialogService textInputDialog) 
			: base(hohoemaApp, pageManager, isRequireSignIn:true)
		{
			_TextInputDialogService = textInputDialog;

			FeedGroupItems = new ObservableCollection<FeedGroupListItem>();
			HasFeedGroupItems = FeedGroupItems.ObserveProperty(x => x.Count)
				.Select(x => x > 0)
				.ToReadOnlyReactiveProperty()
				.AddTo(_CompositeDisposable);
			IsSelectionModeEnable = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);
			SelectedFeedGroupItem = new ReactiveProperty<FeedGroupListItem>()
				.AddTo(_CompositeDisposable);



			NewFeedGroupName = new ReactiveProperty<string>("")
				.AddTo(_CompositeDisposable);

			AddFeedGroupCommand = NewFeedGroupName
				.Select(x =>
				{
					if (string.IsNullOrWhiteSpace(x)) { return false; }

					if (!HohoemaApp.FeedManager.CanAddLabel(x)) { return false; }

					return true;
				})
				.ToReactiveCommand()
				.AddTo(_CompositeDisposable);

			AddFeedGroupCommand.Subscribe(async _ => 
			{
				var feedGroup = await HohoemaApp.FeedManager.AddFeedGroup(NewFeedGroupName.Value);

				PageManager.OpenPage(HohoemaPageType.FeedGroup, feedGroup.Id);
			})
				.AddTo(_CompositeDisposable);
		}


		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (HohoemaApp.FeedManager == null)
			{
				return;
			}

			var items = HohoemaApp.FeedManager.FeedGroups
				.Select(x => new FeedGroupListItem(x, PageManager))
				.ToList();

			foreach (var feedItemListItem in items)
			{
				FeedGroupItems.Add(feedItemListItem);
			}

			base.OnNavigatedTo(e, viewModelState);
		}


		private DelegateCommand<FeedGroupListItem> _OpenFeedVideoPageCommand;
		public DelegateCommand<FeedGroupListItem> OpenFeedVideoPageCommand
		{
			get
			{
				return _OpenFeedVideoPageCommand
					?? (_OpenFeedVideoPageCommand = new DelegateCommand<FeedGroupListItem>((listItem) =>
					{
						PageManager.OpenPage(HohoemaPageType.FeedVideoList, listItem.FeedGroup.Id);
					}));
			}
		}


		
		private DelegateCommand _CreateFeedGroupCommand;
		public DelegateCommand CreateFeedGroupCommand
		{
			get
			{
				return _CreateFeedGroupCommand
					?? (_CreateFeedGroupCommand = new DelegateCommand(async () =>
					{
						var newFeedGroupName = await _TextInputDialogService.GetTextAsync(
							"フィードグループを作成"
							, "フィードグループ名"
							, validater: (name) =>
							{
								if (String.IsNullOrWhiteSpace(name)) { return false; }

								if (!HohoemaApp.FeedManager.CanAddLabel(name)) { return false; }

								return true;
							});

						if (newFeedGroupName != null)
						{
							var feedGroup = await HohoemaApp.FeedManager.AddFeedGroup(newFeedGroupName);

							PageManager.OpenPage(HohoemaPageType.FeedGroup, feedGroup.Id);
						}
					}));
			}
		}


		private DelegateCommand _RemoveFeedGroupCommand;
		public DelegateCommand RemoveFeedGroupCommand
		{
			get
			{
				return _RemoveFeedGroupCommand
					?? (_RemoveFeedGroupCommand = new DelegateCommand(() =>
					{
					}));
			}
		}


	}


	public class FeedGroupListItem : HohoemaListingPageItemBase
	{
		PageManager _PageManager;
		public FeedGroup FeedGroup { get; private set; }


		public string Label { get; private set; }

		public int UnreadItemCount { get; private set; }

		public List<FeedItemSourceViewModel> SourceItems { get; private set; }

		public FeedGroupListItem(FeedGroup feedGroup, PageManager pageManager)
		{
			FeedGroup = feedGroup;
			_PageManager = pageManager;

			Label = feedGroup.Label;
			UnreadItemCount = feedGroup.GetUnreadItemCount();
			SourceItems = FeedGroup.FeedSourceList
				.Select(x => new FeedItemSourceViewModel()
				{
					Name = x.Name,
					ItemType = x.FavoriteItemType
				})
				.ToList();
		}

		private DelegateCommand _SelectedCommand;
		public override ICommand SelectedCommand
		{
			get
			{
				return _SelectedCommand
					?? (_SelectedCommand = new DelegateCommand(() => 
					{
						_PageManager.OpenPage(HohoemaPageType.FeedVideoList, FeedGroup.Id);
					}));
			}
		}


		private DelegateCommand _SecondaryActionCommand;
		public DelegateCommand SecondaryActionCommand
		{
			get
			{
				return _SecondaryActionCommand
					?? (_SecondaryActionCommand = new DelegateCommand(() =>
					{
						_PageManager.OpenPage(HohoemaPageType.FeedGroup, FeedGroup.Id);
					}));
			}
		}

		public override void Dispose()
		{
			
		}
	}


	public class FeedItemSourceViewModel
	{
		public string Name { get; set; }
		public FavoriteItemType ItemType { get; set; }
	}
}

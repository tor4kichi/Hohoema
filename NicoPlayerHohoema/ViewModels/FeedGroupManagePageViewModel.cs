using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NicoPlayerHohoema.Models;
using Prism.Mvvm;
using Prism.Commands;
using NicoPlayerHohoema.Helpers;
using Reactive.Bindings;
using System.Collections.ObjectModel;
using Prism.Windows.Navigation;
using System.Reactive.Linq;
using Reactive.Bindings.Extensions;
using NicoPlayerHohoema.Views.Service;
using System.Threading;

namespace NicoPlayerHohoema.ViewModels
{
	public class FeedGroupManagePageViewModel : HohoemaViewModelBase
	{
        private Services.HohoemaDialogService _HohoemaDialogService;


        public ReactiveProperty<bool> IsSelectionModeEnable { get; private set; }
		public ReactiveProperty<FeedGroupListItem> SelectedFeedGroupItem { get; private set; }

		public ObservableCollection<FeedGroupListItem> FeedGroupItems { get; private set; }
		public ReadOnlyReactiveProperty<bool> HasFeedGroupItems { get; private set; }


		public FeedGroupManagePageViewModel(HohoemaApp hohoemaApp, PageManager pageManager, Services.HohoemaDialogService dialogService) 
			: base(hohoemaApp, pageManager)
		{
            _HohoemaDialogService = dialogService;

			FeedGroupItems = new ObservableCollection<FeedGroupListItem>();
			HasFeedGroupItems = FeedGroupItems.ObserveProperty(x => x.Count)
				.Select(x => x > 0)
				.ToReadOnlyReactiveProperty()
				.AddTo(_CompositeDisposable);
			IsSelectionModeEnable = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);
			SelectedFeedGroupItem = new ReactiveProperty<FeedGroupListItem>()
				.AddTo(_CompositeDisposable);

			

            var feedManager = hohoemaApp.FeedManager;
            feedManager.FeedGroupAdded += FeedManager_FeedGroupAdded;
            feedManager.FeedGroupRemoved += FeedManager_FeedGroupRemoved;
        }

        private void FeedManager_FeedGroupRemoved(Database.Feed feedGroup)
        {
            var removedFeedGroup = FeedGroupItems.FirstOrDefault(x => x.FeedGroup.Id == feedGroup.Id);
            if (removedFeedGroup != null)
            {
                FeedGroupItems.Remove(removedFeedGroup);
            }
        }

        private void FeedManager_FeedGroupAdded(Database.Feed feedGroup)
        {
            FeedGroupItems.Add(new FeedGroupListItem(feedGroup, PageManager));
        }

        
        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (HohoemaApp.FeedManager == null)
			{
				return;
			}

			base.OnNavigatedTo(e, viewModelState);
		}

        protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			

			var items = HohoemaApp.FeedManager.GetAllFeedGroup()
				.Select(x => new FeedGroupListItem(x, PageManager))
				.ToList();

			foreach (var feedItemListItem in items)
			{
				FeedGroupItems.Add(feedItemListItem);
			}

			await base.NavigatedToAsync(cancelToken, e, viewModelState);
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
						var newFeedGroupName = await _HohoemaDialogService.GetTextAsync(
							"フィードグループを作成"
							, "フィードグループ名"
							, validater: (name) =>
							{
								if (String.IsNullOrWhiteSpace(name)) { return false; }

								return true;
							});

						if (newFeedGroupName != null)
						{
							var feedGroup = HohoemaApp.FeedManager.AddFeedGroup(newFeedGroupName);

							PageManager.OpenPage(HohoemaPageType.FeedGroup, feedGroup.Id);
						}
					}));
			}
		}


		private DelegateCommand<FeedGroupListItem> _RemoveFeedGroupCommand;
		public DelegateCommand<FeedGroupListItem> RemoveFeedGroupCommand
		{
			get
			{
				return _RemoveFeedGroupCommand
					?? (_RemoveFeedGroupCommand = new DelegateCommand<FeedGroupListItem>((feedGroupVM) =>
					{
                        var result = HohoemaApp.FeedManager.RemoveFeedGroup(feedGroupVM.FeedGroup);
                        if (result)
                        {
                            var removeItem = FeedGroupItems.FirstOrDefault(x => x.FeedGroup.Id == feedGroupVM.FeedGroup.Id);
                            if (removeItem != null)
                            {
                                FeedGroupItems.Remove(removeItem);
                            }
                        }
					}));
			}
		}

		

	}


    public class FeedGroupListItem : HohoemaListingPageItemBase, Interfaces.IFeedGroup
    {
        PageManager _PageManager;
        public Database.Feed FeedGroup { get; private set; }

        public List<Database.Bookmark> SourceItems { get; private set; }

        public ReactiveProperty<bool> NowUpdate { get; private set; }

        public FeedGroupListItem(Database.Feed feedGroup, PageManager pageManager)
        {
            FeedGroup = feedGroup;
            _PageManager = pageManager;

            Label = feedGroup.Label;
            SourceItems = FeedGroup.Sources
                .ToList();
            OptionText = FeedGroup.UpdateAt.ToString();
            NowUpdate = new ReactiveProperty<bool>(false);
        }

        private DelegateCommand _SelectedCommand;
        public ICommand PrimaryCommand
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

        private DelegateCommand _OpenEditCommand;
        public DelegateCommand OpenEditCommand
        {
            get
            {
                return _OpenEditCommand
                    ?? (_OpenEditCommand = new DelegateCommand(() =>
                    {
                        _PageManager.OpenPage(HohoemaPageType.FeedGroup, FeedGroup.Id);
                    }));
            }
        }

        public string Id => FeedGroup.Id.ToString();

        public void UpdateStarted()
        {
            NowUpdate.Value = true;
        }

        public void UpdateCompleted()
        {
            NowUpdate.Value = false;
            OptionText = FeedGroup.UpdateAt.ToString();
        }
    }
}

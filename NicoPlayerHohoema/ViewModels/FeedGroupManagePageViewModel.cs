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
using System.Threading;

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
			: base(hohoemaApp, pageManager)
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

			

			base.OnNavigatedTo(e, viewModelState);
		}

		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			

			var items = HohoemaApp.FeedManager.FeedGroups
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

							RefreshAllFeedGroupCommand.RaiseCanExecuteChanged();

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
					?? (_RemoveFeedGroupCommand = new DelegateCommand<FeedGroupListItem>(async (feedGroupVM) =>
					{
                        var result = await HohoemaApp.FeedManager.RemoveFeedGroup(feedGroupVM.FeedGroup);
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

		private DelegateCommand _RefreshAllFeedGroupCommand;
		public DelegateCommand RefreshAllFeedGroupCommand
		{
			get
			{
				return _RefreshAllFeedGroupCommand
					?? (_RefreshAllFeedGroupCommand = new DelegateCommand(async () =>
					{

						foreach (var feedGroup in FeedGroupItems)
						{
							feedGroup.UpdateStarted();
						}

						foreach (var feedGroup in FeedGroupItems)
						{
							await feedGroup.FeedGroup.Refresh();

							feedGroup.UpdateCompleted();	
						}
					}, 
					() => HohoemaApp.FeedManager.FeedGroups.Count > 0
					));
			}
		}


	}


	public class FeedGroupListItem : HohoemaListingPageItemBase, IFeedGroup
	{
		PageManager _PageManager;
		public IFeedGroup FeedGroup { get; private set; }


		
		public List<FeedItemSourceViewModel> SourceItems { get; private set; }

		public ReactiveProperty<bool> NowUpdate { get; private set; }

		public FeedGroupListItem(IFeedGroup feedGroup, PageManager pageManager)
		{
			FeedGroup = feedGroup;
			_PageManager = pageManager;

			Label = feedGroup.Label;
            Description = feedGroup.GetUnreadItemCount().ToString();
			SourceItems = FeedGroup.FeedSourceList
				.Select(x => new FeedItemSourceViewModel()
				{
					Name = x.Name,
					ItemType = x.FollowItemType
				})
				.ToList();
            OptionText = FeedGroup.UpdateTime.ToString();
			NowUpdate = new ReactiveProperty<bool>(false);


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

        private DelegateCommand _UpdateCommand;
        public DelegateCommand UpdateCommand
        {
            get
            {
                return _UpdateCommand
                    ?? (_UpdateCommand = new DelegateCommand(async () =>
                    {
                        UpdateStarted();
                        try
                        {
                            await FeedGroup.Refresh();
                        }
                        finally
                        {
                            UpdateCompleted();
                        }
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

        public List<FeedItem> FeedItems => throw new NotImplementedException();

        public FeedManager FeedManager => throw new NotImplementedException();

        public IReadOnlyList<IFeedSource> FeedSourceList => throw new NotImplementedException();

        public HohoemaApp HohoemaApp => throw new NotImplementedException();

        public Guid Id => throw new NotImplementedException();

        public bool IsNeedRefresh => throw new NotImplementedException();

        public DateTime UpdateTime => throw new NotImplementedException();

        public void UpdateStarted()
		{
			NowUpdate.Value = true;
		}

		public void UpdateCompleted()
		{
			NowUpdate.Value = false;
            Description = FeedGroup.GetUnreadItemCount().ToString();
            OptionText = FeedGroup.UpdateTime.ToString();
		}

        public IFeedSource AddMylistFeedSource(string name, string mylistGroupId)
        {
            throw new NotImplementedException();
        }

        public IFeedSource AddTagFeedSource(string tag)
        {
            throw new NotImplementedException();
        }

        public IFeedSource AddUserFeedSource(string name, string userId)
        {
            throw new NotImplementedException();
        }

        public bool ExistFeedSource(FollowItemType itemType, string id)
        {
            throw new NotImplementedException();
        }

        public void ForceMarkAsRead()
        {
            throw new NotImplementedException();
        }

        public int GetUnreadItemCount()
        {
            throw new NotImplementedException();
        }

        public bool MarkAsRead(string videoId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> LoadFeedStream(FileAccessor<List<FeedItem>> fileAccessor)
        {
            throw new NotImplementedException();
        }

        public Task Refresh()
        {
            throw new NotImplementedException();
        }

        public void RemoveUserFeedSource(IFeedSource feedSource)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Rename(string newLabel)
        {
            throw new NotImplementedException();
        }
    }


	public class FeedItemSourceViewModel
	{
		public string Name { get; set; }
		public FollowItemType ItemType { get; set; }
	}
}

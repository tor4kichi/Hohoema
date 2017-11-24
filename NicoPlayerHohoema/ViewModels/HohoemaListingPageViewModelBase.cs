using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Helpers;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Threading;
using Windows.UI.Xaml;
using NicoPlayerHohoema.Views.Service;
using Microsoft.Practices.Unity;

namespace NicoPlayerHohoema.ViewModels
{
	public abstract class HohoemaListingPageViewModelBase<ITEM_VM> : HohoemaViewModelBase
		where ITEM_VM : HohoemaListingPageItemBase
	{

        private AsyncLock _ItemsUpdateLock = new AsyncLock();

		public HohoemaListingPageViewModelBase(HohoemaApp app, PageManager pageManager, bool useDefaultPageTitle = true)
			: base(app, pageManager, useDefaultPageTitle: useDefaultPageTitle)
		{
			NowLoading = new ReactiveProperty<bool>(true)
				.AddTo(_CompositeDisposable);
            
            SelectedItems = new ObservableCollection<ITEM_VM>();

            
            HasItem = new ReactiveProperty<bool>(false);

			HasError = new ReactiveProperty<bool>(false);

			MaxItemsCount = new ReactiveProperty<int>(0)
				.AddTo(_CompositeDisposable);
			LoadedItemsCount = new ReactiveProperty<int>(0)
				.AddTo(_CompositeDisposable);
			SelectedItemsCount = SelectedItems.ObserveProperty(x => x.Count)
				.ToReactiveProperty(0)
				.AddTo(_CompositeDisposable);

            IsItemSelected = SelectedItems.ObserveProperty(x => x.Count)
                .Select(x => x > 0)
                .ToReactiveProperty()
                .AddTo(_CompositeDisposable);


            NowRefreshable = new ReactiveProperty<bool>(false);

			// 複数選択モード
			IsSelectionModeEnable = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);

			IsSelectionModeEnable.Where(x => !x)
				.Subscribe(x => ClearSelection())
				.AddTo(_CompositeDisposable);

			// 複数選択モードによって再生コマンドの呼び出しを制御する
			SelectItemCommand = IsSelectionModeEnable
				.Select(x => !x)
				.ToReactiveCommand<object>()
				.AddTo(_CompositeDisposable);


			

//			var SelectionItemsChanged = SelectedItems.ToCollectionChanged().ToUnit();
/*
            SelectionItemsChanged.Subscribe(_ => 
            {
                if (!IsSelectionModeEnable.Value)
                {
                    var item = SelectedItems.FirstOrDefault();
                    if (item != null)
                    {
                        if (item.PrimaryCommand.CanExecute(null))
                        {
                            item.PrimaryCommand.Execute(null);
                        }
                    }
                }
            });
            */
#if DEBUG
            SelectedItems.CollectionChangedAsObservable()
				.Subscribe(x => 
				{
					Debug.WriteLine("Selected Count: " + SelectedItems.Count);
				})
                .AddTo(_CompositeDisposable);
#endif

            // 読み込み厨または選択中はソートを変更できない
            CanChangeSort = Observable.CombineLatest(
                NowLoading,
                IsSelectionModeEnable
                )
                .Select(x => !x.Any(y => y))
                .ToReactiveProperty()
                .AddTo(_CompositeDisposable);


        }

        public DateTime LatestUpdateTime = DateTime.Now;

        protected override void OnDispose()
		{
            if (IncrementalLoadingItems != null)
            {
                IncrementalLoadingItems.BeginLoading -= BeginLoadingItems;
                IncrementalLoadingItems.DoneLoading -= CompleteLoadingItems;
                if (IncrementalLoadingItems.Source is HohoemaIncrementalSourceBase<ITEM_VM>)
                {
                    (IncrementalLoadingItems.Source as HohoemaIncrementalSourceBase<ITEM_VM>).Error -= HohoemaIncrementalSource_Error;
                }
                IncrementalLoadingItems.Dispose();
                IncrementalLoadingItems = null;
            }
        }
		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{

			if (CheckNeedUpdateOnNavigateTo(e.NavigationMode))
			{
				IncrementalLoadingItems?.Clear();
				IncrementalLoadingItems = null;
			}
			else
			{
				ChangeCanIncmentalLoading(true);
			}

			base.OnNavigatedTo(e, viewModelState);
		}


		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (!NowSignIn && PageIsRequireSignIn)
			{
				IncrementalLoadingItems = null;
				return;
			}

			await ListPageNavigatedToAsync(cancelToken, e, viewModelState);

			if (IncrementalLoadingItems == null
				|| CheckNeedUpdateOnNavigateTo(e.NavigationMode))
			{
				await ResetList();
			}
		}

		protected override Task OnResumed()
		{
			ChangeCanIncmentalLoading(true);

			return base.OnResumed();
		}

		protected virtual Task ListPageNavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			return Task.CompletedTask;
		}


		protected override void OnHohoemaNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			base.OnHohoemaNavigatingFrom(e, viewModelState, suspending);

            if (!suspending)
			{
				ChangeCanIncmentalLoading(false);
			}
        }


		private void ChangeCanIncmentalLoading(bool enableLoading)
		{
			if (IncrementalLoadingItems != null)
			{
				IncrementalLoadingItems.IsPuaseLoading = !enableLoading;
			}
		}

		protected async Task UpdateList()
		{
			// TODO: 表示中のアイテムすべての状態を更新
			// 主にキャッシュ状態の更新が目的

			var source = IncrementalLoadingItems?.Source;

			if (source != null)
			{
				MaxItemsCount.Value = await source.ResetSource();
			}


		}

		protected async Task ResetList()
		{
            using (var releaser = await _ItemsUpdateLock.LockAsync())
            {
                HasItem.Value = true;
                LoadedItemsCount.Value = 0;

                IsSelectionModeEnable.Value = false;

                SelectedItems.Clear();

                if (IncrementalLoadingItems != null)
                {
                    if (IncrementalLoadingItems.Source is HohoemaIncrementalSourceBase<ITEM_VM>)
                    {
                        (IncrementalLoadingItems.Source as HohoemaIncrementalSourceBase<ITEM_VM>).Error -= HohoemaIncrementalSource_Error;
                    }
                    IncrementalLoadingItems.BeginLoading -= BeginLoadingItems;
                    IncrementalLoadingItems.DoneLoading -= CompleteLoadingItems;
                    IncrementalLoadingItems.Dispose();
                    IncrementalLoadingItems = null;
                    RaisePropertyChanged(nameof(IncrementalLoadingItems));
                }

                try
                {

                    var source = GenerateIncrementalSource();

                    if (source == null)
                    {
                        HasItem.Value = false;
                        return;
                    }

                    MaxItemsCount.Value = await source.ResetSource();

                    IncrementalLoadingItems = new IncrementalLoadingCollection<IIncrementalSource<ITEM_VM>, ITEM_VM>(source);
                    RaisePropertyChanged(nameof(IncrementalLoadingItems));

                    IncrementalLoadingItems.BeginLoading += BeginLoadingItems;
                    IncrementalLoadingItems.DoneLoading += CompleteLoadingItems;

                    if (IncrementalLoadingItems.Source is HohoemaIncrementalSourceBase<ITEM_VM>)
                    {
                        (IncrementalLoadingItems.Source as HohoemaIncrementalSourceBase<ITEM_VM>).Error += HohoemaIncrementalSource_Error;
                    }

                    PostResetList();
                }
                catch
                {
                    IncrementalLoadingItems = null;
                    NowLoading.Value = false;
                    HasItem.Value = true;
                    HasError.Value = true;
                    Debug.WriteLine("failed GenerateIncrementalSource.");
                }
            }
		}

		private void HohoemaIncrementalSource_Error()
		{
			HasError.Value = true;
		}

		private void BeginLoadingItems()
		{
			HasError.Value = false;
			NowLoading.Value = true;
		}

		private async void CompleteLoadingItems()
		{
			NowLoading.Value = false;

			LoadedItemsCount.Value = IncrementalLoadingItems?.Count ?? 0;
			HasItem.Value = LoadedItemsCount.Value > 0;

            var count = IncrementalLoadingItems.Source.OneTimeLoadCount;
            var head = LoadedItemsCount.Value - count;
            head = head < 0 ? 0 : head;
            
            /*
            foreach (var item in IncrementalLoadingItems.Skip((int)head).Take((int)count).ToArray())
            {
                if (item is HohoemaListingPageItemBase)
                {
                    await (item as HohoemaListingPageItemBase).DeferredUpdate();
                }
            }
            */
        }

		protected virtual void PostResetList() { }

		protected abstract IIncrementalSource<ITEM_VM> GenerateIncrementalSource();

		protected virtual bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
        {
            if (mode == NavigationMode.New)
            {
                return true;
            }

            var elpasedTime = DateTime.Now - LatestUpdateTime;
            if (elpasedTime > TimeSpan.FromMinutes(30))
            {
                return true;
            }

            return false;
        }

		protected void ClearSelection()
		{
			SelectedItems.Clear();
		}


		#region Selection


		public ReactiveProperty<bool> IsSelectionModeEnable { get; private set; }
		

		public ReactiveCommand<object> SelectItemCommand { get; private set; }

                
        private DelegateCommand _EnableSelectionCommand;
		public DelegateCommand EnableSelectionCommand
		{
			get
			{
				return _EnableSelectionCommand
					?? (_EnableSelectionCommand = new DelegateCommand(() => 
					{
						IsSelectionModeEnable.Value = true;
					}));
			}
		}


		private DelegateCommand _DisableSelectionCommand;
		public DelegateCommand DisableSelectionCommand
		{
			get
			{
				return _DisableSelectionCommand
					?? (_DisableSelectionCommand = new DelegateCommand(() =>
					{
						IsSelectionModeEnable.Value = false;
					}));
			}
		}

		#endregion



		private DelegateCommand _RefreshCommand;
		public DelegateCommand RefreshCommand
		{
			get
			{
				return _RefreshCommand
					?? (_RefreshCommand = new DelegateCommand(async () => 
					{
						await ResetList();
					}));
			}
		}

		public ReactiveProperty<int> MaxItemsCount { get; private set; }
		public ReactiveProperty<int> LoadedItemsCount { get; private set; }
		public ReactiveProperty<int> SelectedItemsCount { get; private set; }

        public ReactiveProperty<bool> IsItemSelected { get; private set; }

        public ObservableCollection<ITEM_VM> SelectedItems { get; private set; }

		public IncrementalLoadingCollection<IIncrementalSource<ITEM_VM>, ITEM_VM> IncrementalLoadingItems { get; private set; }

		public ReactiveProperty<bool> NowLoading { get; private set; }
        public ReactiveProperty<bool> CanChangeSort { get; private set; }

        public ReactiveProperty<bool> NowRefreshable { get; private set; }

		public ReactiveProperty<bool> HasItem { get; private set; }

		public ReactiveProperty<bool> HasError { get; private set; }


		public bool PageIsRequireSignIn { get; private set; }
		public bool NowSignedIn { get; private set; }

	}
}

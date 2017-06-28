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
using NicoPlayerHohoema.Views.Service;
using System.Threading;
using System.Windows.Input;

namespace NicoPlayerHohoema.ViewModels
{
	public class SettingsPageViewModel : HohoemaViewModelBase
	{
		public SettingsPageViewModel(
			HohoemaApp hohoemaApp
			, PageManager pageManager
			, RankingChoiceDialogService rakingChoiceDialog
			, EditAutoCacheConditionDialogService editAutoCacheDialog
			, AcceptCacheUsaseDialogService cacheAcceptDialogService
			, ToastNotificationService toastService
			)
			: base(hohoemaApp, pageManager)
		{
			RankingChoiceDialogService = rakingChoiceDialog;
			EditAutoCacheConditionDialogService = editAutoCacheDialog;
			AcceptCacheUsaseDialogService = cacheAcceptDialogService;
			ToastNotificationService = toastService;

			SettingItems = ((IEnumerable<HohoemaSettingsKind>)Enum.GetValues(typeof(HohoemaSettingsKind)))
                .Where(x => Util.DeviceTypeHelper.IsXbox ? x != HohoemaSettingsKind.Share : true)
				.Select(x => KindToVM(x))
				.ToList();

            CurrentSettingsContent = new ReactiveProperty<SettingsPageContentViewModel>();

            
            CurrentSettingsContent.Subscribe(x =>
            {
                _PrevSettingsContent?.Leaved();

                /*
                if (x != null)
                {
                    AddSubsitutionBackNavigateAction("settings_content_selection"
                        , () => 
                        {
                            CurrentSettingsContent.Value = null;

                            return true;
                        });
                }
                else
                {
                    RemoveSubsitutionBackNavigateAction("settings_content_selection");
                }
                */

                _PrevSettingsContent = x;
                x?.Entered();                
            });
            

        }



        private SettingsPageContentViewModel KindToVM(HohoemaSettingsKind kind)
		{
			SettingsPageContentViewModel vm = null;
			switch (kind)
			{
                case HohoemaSettingsKind.Player:
                    vm = new PlayerSeetingPageContentViewModel(HohoemaApp);
                    break;
                case HohoemaSettingsKind.Filtering:
                    vm = new FilteringSettingsPageContentViewModel(HohoemaApp, PageManager, RankingChoiceDialogService);
                    break;
				case HohoemaSettingsKind.Cache:
					vm = new CacheSettingsPageContentViewModel(HohoemaApp, EditAutoCacheConditionDialogService, AcceptCacheUsaseDialogService);
					break;
				case HohoemaSettingsKind.Appearance:
					vm = new AppearanceSettingsPageContentViewModel(HohoemaApp, ToastNotificationService);
					break;
				case HohoemaSettingsKind.Share:
					vm = new ShareSettingsPageContentViewModel();
					break;
                case HohoemaSettingsKind.Feedback:
                    vm = new FeedbackSettingsPageContentViewModel();
                    break;
                case HohoemaSettingsKind.About:
                    vm = new AboutSettingsPageContentViewModel();
                    break;
                default:
					break;
			}
			

			return vm;
		}


		protected override Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			HohoemaSettingsKind? selectRequestKind = null;

			if (e.Parameter is HohoemaSettingsKind)
			{
				selectRequestKind = (HohoemaSettingsKind)e.Parameter;
			}
			else if (e.Parameter is string)
			{
				HohoemaSettingsKind kind;
				if(Enum.TryParse(e.Parameter as string, out kind))
				{
					selectRequestKind = kind;
				}
			}

			if (selectRequestKind.HasValue)
			{
                SelectContent(selectRequestKind.Value);
			}


			return Task.CompletedTask;
		}

		protected override void OnHohoemaNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			if (suspending)
			{
//				viewModelState[nameof(CurrentSettingsKind)] = CurrentSettingsKind.Value.Kind.ToString();
            }
            else
            {
                // Note: ページアンロード中にPivotのSelectedItemが操作されると
                // Xaml側で例外がスローされてしまうようなので
                // サスペンド処理時はCurrentSettingsContentを変更しない
                CurrentSettingsContent.Value = null;
            }

            base.OnHohoemaNavigatingFrom(e, viewModelState, suspending);
		}


        private void SelectContent(HohoemaSettingsKind kind)
        {
            CurrentSettingsContent.Value = SettingItems.FirstOrDefault(x => x.Kind == kind);
        }



        private SettingsPageContentViewModel _PrevSettingsContent;
        public ReactiveProperty<SettingsPageContentViewModel> CurrentSettingsContent { get; private set; }

		public List<SettingsPageContentViewModel> SettingItems { get; private set; }

		public EditAutoCacheConditionDialogService EditAutoCacheConditionDialogService { get; private set;}
		public RankingChoiceDialogService RankingChoiceDialogService { get; private set; }
		public AcceptCacheUsaseDialogService AcceptCacheUsaseDialogService { get; private set; }
		public ToastNotificationService ToastNotificationService { get; private set; }
	}


	public enum HohoemaSettingsKind
	{
		Player,
		Filtering,
		Cache,
		Appearance,
        Share,
        Feedback,
        About,
    }

	public abstract class SettingsPageContentViewModel : ViewModelBase, IDisposable
	{
		public string Title { get; private set; }
        public HohoemaSettingsKind Kind { get; private set; }

        protected CompositeDisposable _FocusingDisposable;


        public SettingsPageContentViewModel(string title, HohoemaSettingsKind kind)
		{
            Title = title;
            Kind = kind;
        }

        
        internal void Entered()
        {
            _FocusingDisposable = new CompositeDisposable();

            OnEnter(_FocusingDisposable);
        }

        protected virtual void OnEnter(ICollection<IDisposable> focusingDispsable)
        {
        }

        internal void Leaved()
        {
            _FocusingDisposable?.Dispose();

            OnLeave();
        }

		protected virtual void OnLeave()
        {
        }

        public void Dispose()
        {
            _FocusingDisposable?.Dispose();
        }
    }

	


	
	


	public class RemovableListItem<T> : IRemovableListItem

    {
		public T Source { get; private set; }
		public Action<T> OnRemove { get; private set; }

		public string Label { get; private set; }
		public RemovableListItem(T source, string content, Action<T> onRemovedAction)
		{
			Source = source;
			Label = content;
			OnRemove = onRemovedAction;

			RemoveCommand = new DelegateCommand(() => 
			{
				onRemovedAction(Source);
			});
		}


		public ICommand RemoveCommand { get; private set; }
	}


	public class NGKeywordViewModel : IRemovableListItem, IDisposable
    {
		public NGKeywordViewModel(NGKeyword ngTitleInfo, Action<NGKeyword> onRemoveAction)
		{
			_NGKeywordInfo = ngTitleInfo;
			_OnRemoveAction = onRemoveAction;

            Label = _NGKeywordInfo.Keyword;
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

            RemoveCommand = new DelegateCommand(() => 
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

        public string Label { get; private set; }
		public ICommand RemoveCommand { get; private set; }
		
		NGKeyword _NGKeywordInfo;
		Action<NGKeyword> _OnRemoveAction;
	}

	public static class RemovableSettingsListItemHelper
	{
		public static RemovableListItem<string> VideoIdInfoToRemovableListItemVM(VideoIdInfo info, Action<string> removeAction)
		{
			var roundedDesc = info.Description.Substring(0, Math.Min(info.Description.Length - 1, 10));
			return new RemovableListItem<string>(info.VideoId, $"{info.VideoId} | {roundedDesc}", removeAction);
		}

		public static RemovableListItem<string> UserIdInfoToRemovableListItemVM(UserIdInfo info, Action<string> removeAction)
		{
			var roundedDesc = info.Description.Substring(0, Math.Min(info.Description.Length - 1, 10));
			return new RemovableListItem<string>(info.UserId, $"{info.UserId} | {roundedDesc}", removeAction);
		}
	}

	public interface IRemovableListItem
    {
        string Label { get; }
        ICommand RemoveCommand { get; }
    }

	
}

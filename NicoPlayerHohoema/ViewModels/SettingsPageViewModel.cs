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

namespace NicoPlayerHohoema.ViewModels
{
	public class SettingsPageViewModel : ViewModelBase
	{
		public SettingsPageViewModel(HohoemaApp hohoemaApp, RankingChoiceDialogService rakingChoiceDialog)
		{
			HohoemaApp = hohoemaApp;
			RankingChoiceDialogService = rakingChoiceDialog;
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
					case HohoemaSettingsKind.VideoList:
						vm = new VideoListSettingsPageContentViewModel(HohoemaApp, title, RankingChoiceDialogService);
						break;
					case HohoemaSettingsKind.Comment:
						vm = new CommentSettingsPageContentViewModel(HohoemaApp, title);
						break;
					case HohoemaSettingsKind.VideoPlay:
						vm = new VideoPlaySettingsPageContentViewModel(HohoemaApp, title);
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

		public RankingChoiceDialogService RankingChoiceDialogService { get; private set; }
		public HohoemaApp HohoemaApp { get; private set; }
	}


	public enum HohoemaSettingsKind
	{
		VideoList,
		VideoPlay,
		Comment,
	}


	public static class HohoemaSettingsKindExtention
	{
		public static string ToCulturelizedText(this HohoemaSettingsKind kind)
		{
			switch (kind)
			{
				case HohoemaSettingsKind.VideoList:
					return "動画リスト";
				case HohoemaSettingsKind.Comment:
					return "コメント";
				case HohoemaSettingsKind.VideoPlay:
					return "動画再生";
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

	

	
}

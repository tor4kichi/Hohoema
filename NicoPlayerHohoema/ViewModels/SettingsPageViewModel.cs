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
		Account,
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
	}

	


	public class RankingSettingsPageContentViewModel : SettingsPageContentViewModel
	{
		public RankingSettingsPageContentViewModel(HohoemaApp hohoemaApp, string title)
			: base(title)
		{

		}
	}


	public class NGSettingsPageContentViewModel : SettingsPageContentViewModel
	{
		public NGSettingsPageContentViewModel(HohoemaApp hohoemaApp, string title)
			: base(title)
		{

		}
	}


	public class PlayerSettingsPageContentViewModel : SettingsPageContentViewModel
	{
		public PlayerSettingsPageContentViewModel(HohoemaApp hohoemaApp, string title)
			: base(title)
		{

		}
	}

	public class PerformanceSettingsPageContentViewModel : SettingsPageContentViewModel
	{
		public PerformanceSettingsPageContentViewModel(HohoemaApp hohoemaApp, string title)
			: base(title)
		{

		}
	}
}

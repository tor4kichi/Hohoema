using Reactive.Bindings.Extensions;
using Prism.Commands;
using System.Reactive.Linq;
using System.Collections.ObjectModel;
using Reactive.Bindings;
using Prism.Mvvm;
using NicoPlayerHohoema.Views.Service;
using NicoPlayerHohoema.Models;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace NicoPlayerHohoema.ViewModels
{
	public class CacheSettingsPageContentViewModel : SettingsPageContentViewModel
	{
		// Note: ログインしていない場合は利用できない

		public CacheSettingsPageContentViewModel(HohoemaApp hohoemaApp, string title, EditAutoCacheConditionDialogService editDialogService)
			: base(title)
		{
			_HohoemaApp = hohoemaApp;
			_CacheSettings = _HohoemaApp.UserSettings.CacheSettings;
			_EditDialogService = editDialogService;

			IsAutoCacheOnPlayEnable = _CacheSettings.ToReactivePropertyAsSynchronized(x => x.IsAutoCacheOnPlayEnable);
				
			IsUserAcceptRegalNotice = _CacheSettings.ToReactivePropertyAsSynchronized(x => x.IsUserAcceptRegalNotice);
			IsShowRegalNotice = new ReactiveProperty<bool>(!_CacheSettings.IsUserAcceptRegalNotice);

			IsTempAcceptChecked = new ReactiveProperty<bool>(_CacheSettings.IsUserAcceptRegalNotice);
			AcceptCommand = IsTempAcceptChecked
				.ToReactiveCommand();

			AcceptCommand.Subscribe(_ => 
			{
				_CacheSettings.IsUserAcceptRegalNotice = true;
				IsShowRegalNotice.Value = false;
			});

			AddAutoCacheConditionCommand = new DelegateCommand(() => 
			{
				_CacheSettings.CacheOnPlayTagConditions.Add(new TagCondition()
				{
					Label = "NewCondition"
				});
			});

			AutoCacheConditions = _CacheSettings.CacheOnPlayTagConditions.ToReadOnlyReactiveCollection(
				x => new AutoCacheConditionViewModel(_CacheSettings, x)
				);

			EditAutoCacheConditionCommnad = new DelegateCommand<AutoCacheConditionViewModel>(async (conditionVM) => 
			{
				await EditAutoCacheCondition(conditionVM);
			});


			Observable.Merge(
				IsUserAcceptRegalNotice.ToUnit(),
				IsAutoCacheOnPlayEnable.ToUnit()
				)
				.Subscribe(async _ => 
				{
					await _CacheSettings.Save().ConfigureAwait(false);
				});
		}

		private async Task EditAutoCacheCondition(AutoCacheConditionViewModel conditionVM)
		{
			var serializedText = Newtonsoft.Json.JsonConvert.SerializeObject(conditionVM.TagCondition);

			if (!await _EditDialogService.ShowDialog(conditionVM))
			{
				// 編集前の状態に復帰
				try
				{
					var previousState = Newtonsoft.Json.JsonConvert.DeserializeObject<TagCondition>(serializedText);

					conditionVM.TagCondition.Label = previousState.Label;
					conditionVM.TagCondition.IncludeTags.Clear();
					foreach (var tag in previousState.IncludeTags)
					{
						conditionVM.TagCondition.IncludeTags.Add(tag);
					}
					conditionVM.TagCondition.ExcludeTags.Clear();
					foreach (var tag in previousState.ExcludeTags)
					{
						conditionVM.TagCondition.ExcludeTags.Add(tag);
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.ToString());
				}
			}
			else
			{
				await _CacheSettings.Save();
			}
		}

		public override void OnLeave()
		{
			_CacheSettings.Save().ConfigureAwait(false);
		}

		public ReactiveProperty<bool> IsTempAcceptChecked { get; private set; }
		public ReactiveProperty<bool> IsShowRegalNotice { get; private set; }
		public ReactiveProperty<bool> IsUserAcceptRegalNotice { get; private set; }
		public ReactiveProperty<bool> IsAutoCacheOnPlayEnable { get; private set; }


		public ReactiveCommand AcceptCommand { get; private set; }

		public DelegateCommand AddAutoCacheConditionCommand { get; private set; }

		public DelegateCommand<AutoCacheConditionViewModel> EditAutoCacheConditionCommnad { get; private set; }

		public ReadOnlyReactiveCollection<AutoCacheConditionViewModel> AutoCacheConditions { get; private set; }

		EditAutoCacheConditionDialogService _EditDialogService;
		CacheSettings _CacheSettings;
		HohoemaApp _HohoemaApp;
	}


	public class AutoCacheConditionViewModel : BindableBase
	{
		public AutoCacheConditionViewModel(CacheSettings cacheSettings, TagCondition tagCondition)
		{
			TagCondition = tagCondition;

			Label = tagCondition.ToReactivePropertyAsSynchronized(x => x.Label);

			IncludeTags = tagCondition.IncludeTags;
			ExcludeTags = tagCondition.ExcludeTags;

			IncludeTagText = new ReactiveProperty<string>("");
			ExcludeTagText = new ReactiveProperty<string>("");

			AddIncludeTagCommand = IncludeTagText.Select(x => x.Length > 0)
				.ToReactiveCommand();
			AddIncludeTagCommand.Subscribe(x =>
			{
				if (AddIncludeTag(IncludeTagText.Value))
				{
					IncludeTagText.Value = "";
				}
			});

			AddExcludeTagCommand = ExcludeTagText.Select(x => x.Length > 0)
				.ToReactiveCommand();
			AddExcludeTagCommand.Subscribe(x =>
			{
				if (AddExcludeTag(ExcludeTagText.Value))
				{
					ExcludeTagText.Value = "";
				}
			});

			RemoveIncludeTagCommand = new DelegateCommand<string>(x => RemoveIncludeTag(x));
			RemoveExcludeTagCommand = new DelegateCommand<string>(x => RemoveExcludeTag(x));
		}


		private bool AddIncludeTag(string tag)
		{
			if (IncludeTags.Contains(tag))
			{
				return false;
			}

			IncludeTags.Add(tag);

			return true;
		}

		private bool RemoveIncludeTag(string tag)
		{
			if (!IncludeTags.Contains(tag))
			{
				return false;
			}

			return IncludeTags.Remove(tag);
		}




		private bool AddExcludeTag(string tag)
		{
			if (ExcludeTags.Contains(tag))
			{
				return false;
			}

			ExcludeTags.Add(tag);

			return true;
		}

		private bool RemoveExcludeTag(string tag)
		{
			if (!ExcludeTags.Contains(tag))
			{
				return false;
			}

			return ExcludeTags.Remove(tag);
		}

		public ReactiveProperty<string> IncludeTagText { get; private set; }
		public ReactiveProperty<string> ExcludeTagText { get; private set; }

		public ReactiveCommand AddIncludeTagCommand { get; private set; }
		public ReactiveCommand AddExcludeTagCommand { get; private set; }
		public DelegateCommand<string> RemoveIncludeTagCommand { get; private set; }
		public DelegateCommand<string> RemoveExcludeTagCommand { get; private set; }

		public ReactiveProperty<string> Label { get; private set; }
		public ObservableCollection<string> IncludeTags { get; private set; }
		public ObservableCollection<string> ExcludeTags { get; private set; }


		public TagCondition TagCondition { get; private set; }
	}
}

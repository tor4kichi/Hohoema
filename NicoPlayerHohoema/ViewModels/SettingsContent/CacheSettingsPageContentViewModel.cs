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
using Windows.System;
using System.IO;
using System.Linq;

namespace NicoPlayerHohoema.ViewModels
{
	public class CacheSettingsPageContentViewModel : SettingsPageContentViewModel
	{
		// Note: ログインしていない場合は利用できない

		public ReactiveProperty<bool> IsUserAcceptRegalNotice { get; private set; }
		public ReactiveProperty<bool> IsAutoCacheOnPlayEnable { get; private set; }



		public DelegateCommand ReadCacheAcceptTextCommand { get; private set; }
		public DelegateCommand AddAutoCacheConditionCommand { get; private set; }
		public DelegateCommand<AutoCacheConditionViewModel> EditAutoCacheConditionCommnad { get; private set; }

		public ReadOnlyReactiveCollection<AutoCacheConditionViewModel> AutoCacheConditions { get; private set; }

		public ReactiveProperty<string> CacheSaveFolderPath { get; private set; }
		public DelegateCommand OpenCurrentCacheFolderCommand { get; private set; }
		public ReactiveProperty<string> CacheFolderStateDescription { get; private set; }

		public DelegateCommand ChangeCacheFolderCommand { get; private set; }

		public ReactiveProperty<bool> IsEnableCache { get; private set; }
		public ReactiveProperty<bool> IsCacheFolderSelectedButNotExist { get; private set; }
		public DelegateCommand CheckExistCacheFolderCommand { get; private set; }

		HohoemaApp _HohoemaApp;
		CacheSettings _CacheSettings;
		EditAutoCacheConditionDialogService _EditDialogService;
		AcceptCacheUsaseDialogService _AcceptCacheUsaseDialogService;

		public CacheSettingsPageContentViewModel(
			HohoemaApp hohoemaApp
			, EditAutoCacheConditionDialogService editDialogService
			, AcceptCacheUsaseDialogService cacheConfirmDialogService
			)
			: base("キャッシュ", HohoemaSettingsKind.Cache)
		{
			_HohoemaApp = hohoemaApp;
			_CacheSettings = _HohoemaApp.UserSettings.CacheSettings;
			_EditDialogService = editDialogService;
			_AcceptCacheUsaseDialogService = cacheConfirmDialogService;

			IsAutoCacheOnPlayEnable = _CacheSettings.ToReactivePropertyAsSynchronized(x => x.IsAutoCacheOnPlayEnable);
			IsUserAcceptRegalNotice = _CacheSettings.ToReactivePropertyAsSynchronized(x => x.IsUserAcceptedCache);
			IsCacheFolderSelectedButNotExist = new ReactiveProperty<bool>(false);

			ReadCacheAcceptTextCommand = new DelegateCommand(async () =>
			{
				await cacheConfirmDialogService.ShowAcceptCacheTextDialog();
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

			CacheFolderStateDescription = new ReactiveProperty<string>("");
			CacheSaveFolderPath = new ReactiveProperty<string>("");

			OpenCurrentCacheFolderCommand = new DelegateCommand(async () =>
			{
				await RefreshCacheSaveFolderStatus();

				var folder = await _HohoemaApp.GetVideoCacheFolder();
				if (folder != null)
				{
					await Launcher.LaunchFolderAsync(folder);
				}
			});

			IsEnableCache = _HohoemaApp.UserSettings
				.CacheSettings.ToReactivePropertyAsSynchronized(x => x.IsEnableCache);

			IsEnableCache
				.Where(x => x)
				.Where(_ => false == _HohoemaApp.UserSettings.CacheSettings.IsUserAcceptedCache)
				.Subscribe(async x => 
			{
				// ユーザーがキャッシュ機能利用に対する承諾を行っていない場合に
				// 確認のダイアログを表示する
				var result = await _AcceptCacheUsaseDialogService.ShowConfirmAcceptCacheDialog();

				if (result)
				{
					_HohoemaApp.UserSettings.CacheSettings.IsUserAcceptedCache = true;

					await RefreshCacheSaveFolderStatus();
				}
				else
				{
					IsEnableCache.Value = false;
				}
			});

			IsEnableCache.Subscribe(async _ =>
			{
				await RefreshCacheSaveFolderStatus();
			});

			ChangeCacheFolderCommand = new DelegateCommand(async () => 
			{
				if (await _HohoemaApp.ChangeUserDataFolder())
				{
					await RefreshCacheSaveFolderStatus();
				}
			});

			
		}

		public override async void OnEnter()
		{
			base.OnEnter();
			await RefreshCacheSaveFolderStatus();
		}

		public override void OnLeave()
		{
			_CacheSettings.Save().ConfigureAwait(false);
		}


		private async Task RefreshCacheSaveFolderStatus()
		{
			var cacheFolderAccessState = await _HohoemaApp.GetVideoCacheFolderState();
			
			CacheSaveFolderPath.Value = "";
			switch (cacheFolderAccessState)
			{
				case CacheFolderAccessState.NotAccepted:
					CacheFolderStateDescription.Value = "キャッシュ利用の同意が必要です。 上の「キャッシュを有効にする」ボタンを押すと同意文書が表示されます。";
					break;
				case CacheFolderAccessState.NotEnabled:
					CacheFolderStateDescription.Value = "キャッシュの有効化が必要です";
					break;
				case CacheFolderAccessState.NotSelected:
					CacheFolderStateDescription.Value = "フォルダを選択するとキャッシュ機能が使えるようになります";
					break;
				case CacheFolderAccessState.SelectedButNotExist:
					CacheFolderStateDescription.Value = "選択されたフォルダが確認できません。外付けストレージを再接続するか、キャッシュ先フォルダを再選択してください。";
					CacheSaveFolderPath.Value = "?????";
					break;
				case CacheFolderAccessState.Exist:
					CacheFolderStateDescription.Value = "キャッシュ利用の準備ができました";
					break;
				default:
					break;
			}

			var folder = await _HohoemaApp.GetVideoCacheFolder();
			if (folder != null)
			{
				CacheSaveFolderPath.Value = $"{folder.Path}";
			}


			IsCacheFolderSelectedButNotExist.Value = cacheFolderAccessState == CacheFolderAccessState.SelectedButNotExist;
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

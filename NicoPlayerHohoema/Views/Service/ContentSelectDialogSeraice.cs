using NicoPlayerHohoema.Util;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Views.Service
{
	// 一覧または手入力でテキストを入力してもらう

	public struct ContentSelectDialogDefaultSet
	{
		public string DialogTitle { get; set; }
		public string ChoiceListTitle { get; set; }
		public List<SelectDialogPayload> ChoiceList { get; set; }

		public string TextInputTitle { get; set; }
		public Func<string, Task<List<SelectDialogPayload>>> GenerateCandiateList { get; set; }
	}

	public sealed class ContentSelectDialogService
	{
		public Task<SelectDialogPayload> ShowDialog(ContentSelectDialogDefaultSet dialogContentSet)
		{
			var choiceListContainer = new ChoiceFromListSelectableContainer(dialogContentSet.ChoiceListTitle, dialogContentSet.ChoiceList);
			var customTextContainer = new TextInputSelectableContainer(dialogContentSet.TextInputTitle, dialogContentSet.GenerateCandiateList);

			var containers = new ISelectableContainer[] { choiceListContainer, customTextContainer };

			ISelectableContainer firstSelected = null;
			if (choiceListContainer.Items.Count > 0)
			{
				firstSelected = choiceListContainer;
			}
			else
			{
				firstSelected = customTextContainer;
			}

			return ShowDialog(dialogContentSet.DialogTitle, containers, firstSelected);

		
		}

		public async Task<SelectDialogPayload> ShowDialog(string dialogTitle, IEnumerable<ISelectableContainer> containers, ISelectableContainer firstSelected = null)
		{
			var context = new ContentSelectDialogContext(dialogTitle, containers, firstSelected);

			SelectDialogPayload resultContent = null;
			try
			{
				var dialog = new Views.Service.ContentSelectDialog()
				{
					DataContext = context
				};
				
				var dialogResult = await dialog.ShowAsync();
				if (dialogResult == Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
				{
					resultContent = context.GetResult();
				}
			}
			finally
			{
				context?.Dispose();
			}

			return resultContent;
		}
	}

	public class ContentSelectDialogContext : BindableBase, IDisposable
	{
		public List<ISelectableContainer> SelectableContainerList { get; private set; }

		public ReactiveProperty<ISelectableContainer> SelectedContainer { get; private set; }
		public ISelectableContainer _Prev;

		public ReactiveProperty<bool> IsValidItemSelected { get; private set; }

		private IDisposable _ContainerItemChangedEventDisposer;


		public string Title { get; private set; }

		public ContentSelectDialogContext(string dialogTitle, IEnumerable<ISelectableContainer> containers, ISelectableContainer firstSelected = null)
		{
			Title = dialogTitle;
			SelectableContainerList = containers.ToList();

			SelectedContainer = new ReactiveProperty<ISelectableContainer>(firstSelected);
			IsValidItemSelected = new ReactiveProperty<bool>(false);

			_ContainerItemChangedEventDisposer = SelectedContainer.Subscribe(x => 
			{
				if (_Prev != null)
				{
					_Prev.SelectionItemChanged -= SelectableContainer_SelectionItemChanged;
				}

				if (x != null)
				{
					x.SelectionItemChanged += SelectableContainer_SelectionItemChanged;
				}

				_Prev = x;


				if (x != null)
				{
					SelectableContainer_SelectionItemChanged(x);
				}

			});
			
			
		}

		private void SelectableContainer_SelectionItemChanged(ISelectableContainer obj)
		{
			IsValidItemSelected.Value = obj.IsValidatedSelection;
		}

		public SelectDialogPayload GetResult()
		{
			return SelectedContainer.Value?.GetResult();
		}

		public void Dispose()
		{
			SelectedContainer?.Dispose();
			IsValidItemSelected?.Dispose();
			_ContainerItemChangedEventDisposer?.Dispose();
		}
	}

	public interface ISelectableContainer : INotifyPropertyChanged, IDisposable
	{
		string Label { get; }
		SelectDialogPayload GetResult();
		bool IsValidatedSelection { get; }
		event Action<ISelectableContainer> SelectionItemChanged;
	}

	public abstract class SelectableContainer : BindableBase, ISelectableContainer
	{
		private bool _IsSelected;
		public bool IsSelected
		{
			get { return _IsSelected; }
			set { SetProperty(ref _IsSelected, value); }
		}

		private string _Label;
		public string Label
		{
			get { return _Label; }
			set { SetProperty(ref _Label, value); }
		}

		public SelectableContainer(string label)
		{
			Label = label;
		}



		public abstract SelectDialogPayload GetResult();

		public virtual void Dispose()
		{
			
		}

		public abstract bool IsValidatedSelection { get; }
		public abstract event Action<ISelectableContainer> SelectionItemChanged;


	}

	public class ChoiceFromListSelectableContainer : SelectableContainer
	{
		public ChoiceFromListSelectableContainer(string label, IEnumerable<SelectDialogPayload> selectableItems)
			: base(label)
		{
			Items = selectableItems.ToList();
			SelectedItem = null;
		}


		public List<SelectDialogPayload> Items { get; private set; }



		private bool _IsValidatedSelection;
		public override bool IsValidatedSelection => _IsValidatedSelection;

		private SelectDialogPayload _SelectedItem;
		public SelectDialogPayload SelectedItem
		{
			get { return _SelectedItem; }
			set
			{
				if (SetProperty(ref _SelectedItem, value))
				{
					_IsValidatedSelection = _SelectedItem != null;
					OnPropertyChanged(nameof(IsValidatedSelection));

					SelectionItemChanged?.Invoke(this);
				}
			}
		}

		public override event Action<ISelectableContainer> SelectionItemChanged;

		public override SelectDialogPayload GetResult()
		{
			return SelectedItem;
		}
	}

	public class TextInputSelectableContainer : SelectableContainer
	{

		IDisposable _TextErrorSubscriptionDisposer;

		public Func<string, Task<List<SelectDialogPayload>>> GenerateCandidateList { get; private set; }
		AsyncLock _UpdateCandidateListLock = new AsyncLock();

		CompositeDisposable _CompositeDisposable;

		public TextInputSelectableContainer(string label, Func<string, Task<List<SelectDialogPayload>>> generateCandiateList, string defaultText = "")
			: base(label)
		{
			_CompositeDisposable = new CompositeDisposable();
			IsSelectFromCandidate = generateCandiateList != null;
			GenerateCandidateList = generateCandiateList;
			NowUpdateCandidateList = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);

			Text = new ReactiveProperty<string>(defaultText)
				.AddTo(_CompositeDisposable);
			CandidateItems = new ObservableCollection<SelectDialogPayload>();
			SelectedItem = new ReactiveProperty<SelectDialogPayload>()
				.AddTo(_CompositeDisposable);

			if (IsSelectFromCandidate)
			{
				var dispatcher = Windows.UI.Xaml.Window.Current.Dispatcher;
				Text
					.Throttle(TimeSpan.FromSeconds(0.5))
					.Where(x => !string.IsNullOrWhiteSpace(x))
					.Subscribe(async x => 
				{
					using (var releaser = await _UpdateCandidateListLock.LockAsync())
					{
						NowUpdateCandidateList.Value = true;

						var list = await GenerateCandidateList.Invoke(x);

						// 表示候補を取得
						await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
						{
							SelectedItem.Value = null;

							CandidateItems.Clear();

							if (list == null) { return; }

							foreach (var item in list)
							{
								CandidateItems.Add(item);
							}

							// 候補が一つだけの場合は予め選択
							if (CandidateItems.Count == 1)
							{
								SelectedItem.Value = CandidateItems.First();
							}
						});

						NowUpdateCandidateList.Value = false;
					}
				})
				.AddTo(_CompositeDisposable);

				SelectedItem.Subscribe(x => 
				{
					_IsValidatedSelection = x != null;
					OnPropertyChanged(nameof(IsValidatedSelection));

					SelectionItemChanged?.Invoke(this);
				})
				.AddTo(_CompositeDisposable);
			}
			else
			{
				Text.Subscribe(x => 
				{
					_IsValidatedSelection = !string.IsNullOrEmpty(x);
					OnPropertyChanged(nameof(IsValidatedSelection));

					SelectionItemChanged?.Invoke(this);
				})
				.AddTo(_CompositeDisposable);
			}
		}


		public override void Dispose()
		{
			_CompositeDisposable.Dispose();
			base.Dispose();
		}

		private bool _IsValidatedSelection;
		public override bool IsValidatedSelection => _IsValidatedSelection;

		public ReactiveProperty<string> Text { get; private set; }


		public bool IsSelectFromCandidate { get; private set; }
		public ObservableCollection<SelectDialogPayload> CandidateItems { get; private set; }
		public ReactiveProperty<SelectDialogPayload> SelectedItem { get; private set; }
		public ReactiveProperty<bool> NowUpdateCandidateList { get; private set; }


		public override event Action<ISelectableContainer> SelectionItemChanged;

		public override SelectDialogPayload GetResult()
		{
			if (IsSelectFromCandidate)
			{
				return SelectedItem.Value;
			}
			else
			{
				return new SelectDialogPayload()
				{
					Label = Text.Value,
					Id = Text.Value
				};
			}
		}
	}

	public class SelectDialogPayload
	{
		public string Label { get; set; }
		public string Id { get; set; }
	}
}

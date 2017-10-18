using NicoPlayerHohoema.Helpers;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Dialogs
{
    public class TextInputSelectableContainer : SelectableContainerBase
	{

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
					RaisePropertyChanged(nameof(IsValidatedSelection));

					SelectionItemChanged?.Invoke(this);
				})
				.AddTo(_CompositeDisposable);
			}
			else
			{
				Text.Subscribe(x => 
				{
					_IsValidatedSelection = !string.IsNullOrEmpty(x);
					RaisePropertyChanged(nameof(IsValidatedSelection));

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
}

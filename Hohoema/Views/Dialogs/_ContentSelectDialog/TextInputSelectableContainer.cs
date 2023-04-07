using Hohoema.Helpers;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Dialogs
{
    public class TextInputSelectableContainer : SelectableContainerBase
	{
        private SynchronizationContextScheduler _CurrentWindowContextScheduler;
        public SynchronizationContextScheduler CurrentWindowContextScheduler
        {
            get
            {
                return _CurrentWindowContextScheduler
                    ?? (_CurrentWindowContextScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current));
            }
        }

        public Func<string, Task<List<SelectDialogPayload>>> GenerateCandidateList { get; private set; }
        Helpers.AsyncLock _UpdateCandidateListLock = new Helpers.AsyncLock();

		CompositeDisposable _CompositeDisposable;

		public TextInputSelectableContainer(string label, Func<string, Task<List<SelectDialogPayload>>> generateCandiateList, string defaultText = "")
			: base(label)
		{
			_CompositeDisposable = new CompositeDisposable();
			IsSelectFromCandidate = generateCandiateList != null;
			GenerateCandidateList = generateCandiateList;
			NowUpdateCandidateList = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false)
				.AddTo(_CompositeDisposable);

			Text = new ReactiveProperty<string>(CurrentWindowContextScheduler, defaultText)
				.AddTo(_CompositeDisposable);
			CandidateItems = new ObservableCollection<SelectDialogPayload>();
			SelectedItem = new ReactiveProperty<SelectDialogPayload>(CurrentWindowContextScheduler)
				.AddTo(_CompositeDisposable);

			if (IsSelectFromCandidate)
			{
				var dispatcher = Windows.UI.Xaml.Window.Current.Dispatcher;
#pragma warning disable IDISP004 // Don't ignore created IDisposable.
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
#pragma warning restore IDISP004 // Don't ignore created IDisposable.

#pragma warning disable IDISP004 // Don't ignore created IDisposable.
                SelectedItem.Subscribe(x => 
				{
					_IsValidatedSelection = x != null;
					OnPropertyChanged(nameof(IsValidatedSelection));

					SelectionItemChanged?.Invoke(this);
				})
				.AddTo(_CompositeDisposable);
#pragma warning restore IDISP004 // Don't ignore created IDisposable.
            }
			else
			{
#pragma warning disable IDISP004 // Don't ignore created IDisposable.
                Text.Subscribe(x => 
				{
					_IsValidatedSelection = !string.IsNullOrEmpty(x);
					OnPropertyChanged(nameof(IsValidatedSelection));

					SelectionItemChanged?.Invoke(this);
				})
				.AddTo(_CompositeDisposable);
#pragma warning restore IDISP004 // Don't ignore created IDisposable.
            }
		}


		public override void Dispose()
		{
			_CompositeDisposable.Dispose();
            Text?.Dispose();
            SelectedItem?.Dispose();
            NowUpdateCandidateList?.Dispose();
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

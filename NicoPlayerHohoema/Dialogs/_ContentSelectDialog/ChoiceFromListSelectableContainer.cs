using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace NicoPlayerHohoema.Dialogs
{
    public class ChoiceFromListSelectableContainer : SelectableContainerBase
	{
		public ChoiceFromListSelectableContainer(string label, IEnumerable<SelectDialogPayload> selectableItems)
			: base(label)
		{
			Items = selectableItems.ToList();
			SelectedItem = Items.FirstOrDefault();
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
					RaisePropertyChanged(nameof(IsValidatedSelection));

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
}

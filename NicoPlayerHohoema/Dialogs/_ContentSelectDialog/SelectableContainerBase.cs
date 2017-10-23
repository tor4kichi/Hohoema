using Prism.Mvvm;
using System;

namespace NicoPlayerHohoema.Dialogs
{
    public abstract class SelectableContainerBase : BindableBase, ISelectableContainer
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

		public SelectableContainerBase(string label)
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
}

using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NicoPlayerHohoema.ViewModels
{
	
	public class SelectableItem<T> : HohoemaListingPageItemBase
	{
		public SelectableItem(T source, Action<T> selectedAction)
		{
			Source = source;
			SelectedAction = selectedAction;
		}


		public T Source { get; private set; }

		public Action<T> SelectedAction { get; private set; }

		private DelegateCommand _SelectedCommand;
        public ICommand PrimaryCommand
		{
			get
			{
				return _SelectedCommand
					?? (_SelectedCommand = new DelegateCommand(() =>
					{
						SelectedAction(Source);
					}));
			}
		}
	}

	public class SelectableItem : BindableBase
	{
		public SelectableItem(Action selectedAction)
		{
			SelectedAction = selectedAction;
		}

		public Action SelectedAction { get; private set; }

		private DelegateCommand _SelectedCommand;
		public DelegateCommand SelectedCommand
		{
			get
			{
				return _SelectedCommand
					?? (_SelectedCommand = new DelegateCommand(() =>
					{
						SelectedAction();
					}));
			}
		}
	}
}

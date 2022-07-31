using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hohoema.Presentation.ViewModels
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

		private RelayCommand _SelectedCommand;
        public ICommand PrimaryCommand
		{
			get
			{
				return _SelectedCommand
					?? (_SelectedCommand = new RelayCommand(() =>
					{
						SelectedAction(Source);
					}));
			}
		}
	}

	public class SelectableItem : ObservableObject
	{
		public SelectableItem(Action selectedAction)
		{
			SelectedAction = selectedAction;
		}

		public Action SelectedAction { get; private set; }

		private RelayCommand _SelectedCommand;
		public RelayCommand SelectedCommand
		{
			get
			{
				return _SelectedCommand
					?? (_SelectedCommand = new RelayCommand(() =>
					{
						SelectedAction();
					}));
			}
		}
	}
}

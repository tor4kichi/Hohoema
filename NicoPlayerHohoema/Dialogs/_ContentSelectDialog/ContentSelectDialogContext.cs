using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace NicoPlayerHohoema.Dialogs
{

    public class ContentSelectDialogContext : BindableBase, IDisposable
	{
		public List<ISelectableContainer> SelectableContainerList { get; private set; }

		public ReactiveProperty<ISelectableContainer> SelectedContainer { get; private set; }
		public ISelectableContainer _Prev;

		public ReactiveProperty<bool> IsValidItemSelected { get; private set; }

		private IDisposable _ContainerItemChangedEventDisposer;

        public bool IsSingleContainer { get; private set; }

		public string Title { get; private set; }

		public ContentSelectDialogContext(string dialogTitle, IEnumerable<ISelectableContainer> containers, ISelectableContainer firstSelected = null)
		{
			Title = dialogTitle;
			SelectableContainerList = containers.ToList();
            IsSingleContainer = SelectableContainerList.Count == 1;

            SelectedContainer = new ReactiveProperty<ISelectableContainer>(firstSelected);
            IsValidItemSelected = SelectedContainer.Select(x => x?.IsValidatedSelection ?? false)
                .ToReactiveProperty();

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
			

            foreach (var container in SelectableContainerList)
            {
                (container as SelectableContainerBase).PropertyChanged += ContentSelectDialogContext_PropertyChanged;
            }

            if (firstSelected != null)
            {
                (firstSelected as SelectableContainerBase).IsSelected = true;
            }
        }

        private void ContentSelectDialogContext_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsSelected")
            {
                var container = sender as SelectableContainerBase;
                if (container.IsSelected)
                {
                    SelectedContainer.Value = container;
                }
            }
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
}

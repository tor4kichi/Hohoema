using Hohoema.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Hohoema.Dialogs;

public sealed partial class ContentSelectDialog : ContentDialog
	{
		public ContentSelectDialog()
		{
			this.InitializeComponent();
		}

		
	}


	public class ContentSelectContainerTemplateSelector : DataTemplateSelector
	{
		public DataTemplate List { get; set; }
		public DataTemplate InputText { get; set; }


		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			if (item is ChoiceFromListSelectableContainer)
			{
				return List;
			}
			else if (item is TextInputSelectableContainer)
			{
				return InputText;
			}

			return base.SelectTemplateCore(item, container);
		}
	}

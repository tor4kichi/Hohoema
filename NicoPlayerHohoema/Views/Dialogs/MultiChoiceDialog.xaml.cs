using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace NicoPlayerHohoema.Dialogs
{ 
	public sealed partial class MultiChoiceDialog : ContentDialog
	{
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(nameof(Items)
                    , typeof(IList)
                    , typeof(MultiChoiceDialog)
                    , new PropertyMetadata(null)
                );

        public IList Items
        {
            get { return (IList)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }


        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register(nameof(SelectedItems)
                    , typeof(IList)
                    , typeof(MultiChoiceDialog)
                    , new PropertyMetadata(null)
                );

        public IList SelectedItems
        {
            get { return (IList)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }


        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register(nameof(DisplayMemberPath)
                    , typeof(string)
                    , typeof(MultiChoiceDialog)
                    , new PropertyMetadata(null)
                );

        public string DisplayMemberPath
        {
            get { return (string)GetValue(DisplayMemberPathProperty); }
            set { SetValue(DisplayMemberPathProperty, value); }
        }

        public MultiChoiceDialog()
		{
			this.InitializeComponent();
        }

	}
}

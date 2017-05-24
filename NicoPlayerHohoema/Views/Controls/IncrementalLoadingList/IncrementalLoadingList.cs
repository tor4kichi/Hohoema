using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace NicoPlayerHohoema.Views.Controls
{
    public partial class IncrementalLoadingList : Control
    {
        public const string SelectionEnabledStateName = @"SelectionEnabled";
        public const string SelectionDisabledStateName = @"SelectionDisabled";



        public IncrementalLoadingList()
        {
            this.DefaultStyleKey = typeof(IncrementalLoadingList);
            this.Loaded += IncrementalLoadingList_Loaded;
        }

        private void IncrementalLoadingList_Loaded(object sender, RoutedEventArgs e)
        {
            SetSelectionVisualState();
        }

        private void SetSelectionVisualState()
        {
            if (IsSelectionEnabled)
            {
                VisualStateManager.GoToState(this, SelectionEnabledStateName, true);
            }
            else
            {
                VisualStateManager.GoToState(this, SelectionDisabledStateName, true);
            }
        }
    }
}

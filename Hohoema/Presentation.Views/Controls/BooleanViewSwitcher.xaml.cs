using System;
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

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Hohoema.Presentation.Views.Controls
{
    public sealed partial class BooleanViewSwitcher : UserControl
    {
        public BooleanViewSwitcher()
        {
            this.InitializeComponent();
        }

        public bool IsSwitchView
        {
            get { return (bool)GetValue(IsSwitchViewProperty); }
            set { SetValue(IsSwitchViewProperty, value); }
        }

        public static readonly DependencyProperty IsSwitchViewProperty =
            DependencyProperty.Register("IsSwitchView", typeof(bool), typeof(BooleanViewSwitcher), new PropertyMetadata(false));



        public DataTemplate DefaultViewTemplate
        {
            get { return (DataTemplate)GetValue(DefaultViewTemplateProperty); }
            set { SetValue(DefaultViewTemplateProperty, value); }
        }

        public static readonly DependencyProperty DefaultViewTemplateProperty =
            DependencyProperty.Register("DefaultViewTemplate", typeof(DataTemplate), typeof(BooleanViewSwitcher), new PropertyMetadata(null));




        public DataTemplate SwitchedViewTemplate
        {
            get { return (DataTemplate)GetValue(SwitchedViewTemplateProperty); }
            set { SetValue(SwitchedViewTemplateProperty, value); }
        }

        public static readonly DependencyProperty SwitchedViewTemplateProperty =
            DependencyProperty.Register("SwitchedViewTemplate", typeof(DataTemplate), typeof(BooleanViewSwitcher), new PropertyMetadata(null));


    }
}

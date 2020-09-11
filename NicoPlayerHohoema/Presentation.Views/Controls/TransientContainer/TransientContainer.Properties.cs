using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace Hohoema.Presentation.Views.Controls
{
    [ContentProperty(Name = "Content")]
    public sealed partial class TransientContainer : Control
    {
        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(nameof(Content)
                    , typeof(object)
                    , typeof(TransientContainer)
                    , new PropertyMetadata(default(object))
                );


        public object Content
        {
            get { return (object)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }



        public static readonly DependencyProperty ContentTemplateProperty =
            DependencyProperty.Register(nameof(ContentTemplate)
                    , typeof(DataTemplate)
                    , typeof(TransientContainer)
                    , new PropertyMetadata(default(DataTemplate))
                );

        public DataTemplate ContentTemplate
        {
            get { return (DataTemplate)GetValue(ContentTemplateProperty); }
            set { SetValue(ContentTemplateProperty, value); }
        }


        public static readonly DependencyProperty DisplayDurationProperty =
            DependencyProperty.Register(nameof(DisplayDuration)
                    , typeof(TimeSpan)
                    , typeof(TransientContainer)
                    , new PropertyMetadata(TimeSpan.FromSeconds(2.5)));


        public TimeSpan DisplayDuration
        {
            get { return (TimeSpan)GetValue(DisplayDurationProperty); }
            set { SetValue(DisplayDurationProperty, value); }
        }


        public static readonly DependencyProperty IsAutoHideEnabledProperty =
           DependencyProperty.Register(nameof(IsAutoHideEnabled)
                   , typeof(bool)
                   , typeof(TransientContainer)
                   , new PropertyMetadata(true)
               );


        public bool IsAutoHideEnabled
        {
            get { return (bool)GetValue(IsAutoHideEnabledProperty); }
            set { SetValue(IsAutoHideEnabledProperty, value); }
        }

        
    }
}

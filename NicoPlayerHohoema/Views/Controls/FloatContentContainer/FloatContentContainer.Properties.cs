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

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace NicoPlayerHohoema.Views.Controls
{
    public partial class FloatContentContainer : Control
    {
        public static readonly DependencyProperty IsFillFloatContentProperty =
            DependencyProperty.Register("IsFillFloatContent"
                    , typeof(bool)
                    , typeof(FloatContentContainer)
                    , new PropertyMetadata(default(bool), OnIsFillFloatContentPropertyChanged)
                );

        public static readonly DependencyProperty FloatContentVisiblityProperty =
           DependencyProperty.Register("FloatContentVisiblity"
                   , typeof(Visibility)
                   , typeof(FloatContentContainer)
                   , new PropertyMetadata(Visibility.Visible, OnFloatContentVisiblityPropertyChanged)
               );


        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register("Content"
                    , typeof(object)
                    , typeof(FloatContentContainer)
                    , new PropertyMetadata(default(DataTemplate))
                );

        public static readonly DependencyProperty ContentTemplateProperty =
            DependencyProperty.Register("ContentTemplate"
                    , typeof(DataTemplate)
                    , typeof(FloatContentContainer)
                    , new PropertyMetadata(default(DataTemplate))
                );

        public static readonly DependencyProperty FloatContentProperty =
            DependencyProperty.Register("FloatContent"
                    , typeof(object)
                    , typeof(FloatContentContainer)
                    , new PropertyMetadata(default(DataTemplate))
                );

        public static readonly DependencyProperty FloatContentTemplateProperty =
            DependencyProperty.Register("FloatContentTemplate"
                    , typeof(DataTemplate)
                    , typeof(FloatContentContainer)
                    , new PropertyMetadata(default(DataTemplate))
                );


        public bool IsFillFloatContent
        {
            get { return (bool)GetValue(IsFillFloatContentProperty); }
            set { SetValue(IsFillFloatContentProperty, value); }
        }

        public Visibility FloatContentVisiblity
        {
            get { return (Visibility)GetValue(FloatContentVisiblityProperty); }
            set { SetValue(FloatContentVisiblityProperty, value); }
        }


        public object Content
        {
            get { return (object)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public DataTemplate ContentTemplate
        {
            get { return (DataTemplate)GetValue(ContentTemplateProperty); }
            set { SetValue(ContentTemplateProperty, value); }
        }

        public object FloatContent
        {
            get { return (object)GetValue(FloatContentProperty); }
            set { SetValue(FloatContentProperty, value); }
        }

        public DataTemplate FloatContentTemplate
        {
            get { return (DataTemplate)GetValue(FloatContentTemplateProperty); }
            set { SetValue(FloatContentTemplateProperty, value); }
        }
    }
}

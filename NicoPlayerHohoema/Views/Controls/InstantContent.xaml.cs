using Microsoft.Toolkit.Uwp.UI.Animations;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views.Controls
{
    public sealed partial class InstantContent : UserControl
    {

        public static string ContentDisplayedVisualStateName = @"ContentDisplayed";
        public static string ContentKeepShowingVisualStateName = @"ContentKeepShowing";
        public static string ContentHiddenVisualStateName = @"ContentHidden";


        string CurrentStateName = ContentHiddenVisualStateName;
        bool _CurrentKeepShowing => CurrentStateName == ContentKeepShowingVisualStateName;

        public static readonly DependencyProperty DisplayContentProperty =
            DependencyProperty.Register(nameof(DisplayContent)
                    , typeof(object)
                    , typeof(InstantContent)
                    , new PropertyMetadata(default(object))
                );
                

        public object DisplayContent
        {
            get { return (object)GetValue(DisplayContentProperty); }
            set { SetValue(DisplayContentProperty, value); }
        }



        public static readonly DependencyProperty DisplayContentTemplateProperty =
            DependencyProperty.Register(nameof(DisplayContentTemplate)
                    , typeof(DataTemplate)
                    , typeof(InstantContent)
                    , new PropertyMetadata(default(DataTemplate))
                );

        public DataTemplate DisplayContentTemplate
        {
            get { return (DataTemplate)GetValue(DisplayContentTemplateProperty); }
            set { SetValue(DisplayContentTemplateProperty, value); }
        }


        public static readonly DependencyProperty DisplayDurationProperty =
            DependencyProperty.Register(nameof(DisplayDuration)
                    , typeof(TimeSpan)
                    , typeof(InstantContent)
                    , new PropertyMetadata(TimeSpan.FromSeconds(2.5), (x, y) =>
                    {
                        var _this = x as InstantContent;
                        if (y.NewValue != null && _this.IsEnabled)
                        {
                            _this.ResetAnimation();
                        }
                    }));


        public TimeSpan DisplayDuration
        {
            get { return (TimeSpan)GetValue(DisplayDurationProperty); }
            set { SetValue(DisplayDurationProperty, value); }
        }


        AnimationSet FadeOutAnimation;


        public InstantContent()
        {
            this.InitializeComponent();

            ResetAnimation();

            this.ObserveDependencyProperty(IsEnabledProperty)
                .Subscribe(_ => 
                {
                    if (this.IsEnabled)
                    {
                        ResetAnimation();
                    }
                    else
                    {
                        
                    }
                });
            this.ObserveDependencyProperty(DisplayContentProperty)
                .Subscribe(async _ =>
                {
                    if (this.IsEnabled && _IsLoaded)
                    {
                        if (FadeOutAnimation.State == AnimationSetState.Running)
                        {
                            FadeOutAnimation.Stop();
                            ContentContainer.Opacity = 1.0;
                            await Task.Delay(10);
                        }

                        FadeOutAnimation.StartAsync();
                    }
                });

            Loaded += InstantContent_Loaded;
        }

        bool _IsLoaded = false;
        private void InstantContent_Loaded(object sender, RoutedEventArgs e)
        {
            _IsLoaded = true;
        }

        void ResetAnimation()
        {
            FadeOutAnimation?.Dispose();
            FadeOutAnimation = ContentContainer.Fade(1.0f, 100)
                .Then()
                .Fade(0.0f, 100, DisplayDuration.TotalMilliseconds);

        }
    }
}

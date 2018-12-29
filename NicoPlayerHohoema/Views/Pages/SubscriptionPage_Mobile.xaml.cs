using Microsoft.Toolkit.Uwp.UI.Animations;
using NicoPlayerHohoema.Models.Helpers;
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

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace NicoPlayerHohoema.Views
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class SubscriptionPage_Mobile : Page
    {
        public SubscriptionPage_Mobile()
        {
            this.InitializeComponent();

            SubscriptionSelectStateGroup.CurrentStateChanged += SubscriptionSelectStateGroup_CurrentStateChanged;

            Loaded += SubscriptionPage_Mobile_InitializeSubscriptionSelectedAnimation;
        }




        #region Subscription Content Animations

        const float ContentOffsetAmount = 50;

        private void SubscriptionPage_Mobile_InitializeSubscriptionSelectedAnimation(object sender, RoutedEventArgs e)
        {
            IndivisualItemViewContent.Offset(ContentOffsetAmount, duration: 1)
                .Fade(0, duration: 1)
                .Start();
            IndivisualItemViewBackground.Fade(0, duration:1)
                .Start();
        }

        AsyncLock _SelectionVisualAnimationLock = new AsyncLock();
        private async void SubscriptionSelectStateGroup_CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            using (_ = await _SelectionVisualAnimationLock.LockAsync())
            {
                if (e.NewState?.Name == SubscriptionSelected.Name)
                {
                    IndivisualItemView.Visibility = Visibility.Visible;

                    await IndivisualItemViewBackground.Fade(0.3f, duration: 50)
                        .StartAsync();
                    await IndivisualItemViewContent.Offset(0, duration: 150)
                        .Fade(1.0f, duration:150)
                        .StartAsync();
                }
                else 
                {
                    await IndivisualItemViewContent.Offset(ContentOffsetAmount, duration: 150)
                        .Fade(0.0f, duration: 150)
                        .StartAsync();
                    await IndivisualItemViewBackground.Fade(0.0f, duration: 50)
                        .StartAsync();
                    
                    IndivisualItemView.Visibility = Visibility.Collapsed;
                }
            }
        }



        #endregion
    }
}

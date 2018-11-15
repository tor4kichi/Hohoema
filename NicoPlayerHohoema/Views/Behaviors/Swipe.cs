using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.Views.Behaviors
{
    public sealed class Swipe : Behavior<UIElement>
    {
        protected override void OnAttached()
        {
            ResetManipulationEvent();

            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.ManipulationStarted -= AssociatedObject_ManipulationStarted;
            AssociatedObject.ManipulationCompleted -= AssociatedObject_ManipulationCompleted;
            AssociatedObject.ManipulationDelta -= AssociatedObject_ManipulationDelta;

            base.OnDetaching();
        }


        bool _IsSkipSwipe = false;
        private void AssociatedObject_ManipulationStarted(object sender, Windows.UI.Xaml.Input.ManipulationStartedRoutedEventArgs e)
        {
            NowSwiping = true;
            _IsSkipSwipe = false;

            // 画面端の遊びチェック
            bool isOutOfEdgeBounce = false;
            var play = SwipeEdgeOfPlay;
            if (e.Container is FrameworkElement)
            {
                var fe = e.Container as FrameworkElement;
                var width = fe.ActualWidth;
                var height = fe.ActualHeight;

                Rect swipeRect = new Rect(play.Left, play.Top, width - play.Right - play.Left, height - play.Bottom - play.Top);
                if (!swipeRect.Contains(e.Position))
                {
                    isOutOfEdgeBounce = true;

                    System.Diagnostics.Debug.WriteLine("Swipe out of bounce");
                }
            }

            if (!isOutOfEdgeBounce)
            {
                InitializeSwipe(e.Cumulative);
                e.Handled = true;
            }
            else
            {
                _IsSkipSwipe = true;
            }
        }

        private void AssociatedObject_ManipulationDelta(object sender, Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs e)
        {
            if (_IsSkipSwipe) { return; }

            UpdateSwipe(e.Cumulative);
            e.Handled = true;
        }

        private void AssociatedObject_ManipulationCompleted(object sender, Windows.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs e)
        {
            NowSwiping = false;

            if (_IsSkipSwipe) { return; }

            UpdateSwipe(e.Cumulative);

            CompleteSwipe(e.Position);
            e.Handled = true;
        }

        private void InitializeSwipe(Windows.UI.Input.ManipulationDelta cumulative)
        {
            SwipeAmount = 0;
        }

        private void UpdateSwipe(Windows.UI.Input.ManipulationDelta cumulative)
        {
            double swipeAmount = 0.0;
            switch (AssociatedObject.ManipulationMode)
            {
                case Windows.UI.Xaml.Input.ManipulationModes.None:
                    break;
                case Windows.UI.Xaml.Input.ManipulationModes.TranslateX:
                    swipeAmount = cumulative.Translation.X;
                    break;
                case Windows.UI.Xaml.Input.ManipulationModes.TranslateY:
                    swipeAmount = cumulative.Translation.Y;
                    break;
                case Windows.UI.Xaml.Input.ManipulationModes.TranslateRailsX:
                    swipeAmount = cumulative.Translation.X;
                    break;
                case Windows.UI.Xaml.Input.ManipulationModes.TranslateRailsY:
                    swipeAmount = cumulative.Translation.Y;
                    break;
                case Windows.UI.Xaml.Input.ManipulationModes.Rotate:
                    swipeAmount = cumulative.Rotation;
                    break;
                case Windows.UI.Xaml.Input.ManipulationModes.Scale:
                    swipeAmount = cumulative.Scale;
                    break;
                case Windows.UI.Xaml.Input.ManipulationModes.TranslateInertia:
                    break;
                case Windows.UI.Xaml.Input.ManipulationModes.RotateInertia:
                    break;
                case Windows.UI.Xaml.Input.ManipulationModes.ScaleInertia:
                    break;
                case Windows.UI.Xaml.Input.ManipulationModes.All:
                    break;
                case Windows.UI.Xaml.Input.ManipulationModes.System:
                    break;
                default:
                    break;
            }

            // スワイプの遊び量よりもスワイプ移動量が低い場合はスワイプとして扱わない
            var play = SwipeAmountOfPlay;
            if (play >= Math.Abs(swipeAmount))
            {
                
            }
            else
            {
                // 遊び量を超えるスワイプ移動量を検出した
                // 遊び量を引いて見かけに合わせたスワイプ移動量を求める
                SwipeAmount = (swipeAmount - play) * SwipeAmountScale;

                System.Diagnostics.Debug.WriteLine("Swipe: " + SwipeAmount);
            }
        }

        private void CompleteSwipe(Point position)
        {
            if (SwipeAmount != 0.0)
            {
                var amount = SwipeAmountConverter?.Convert(SwipeAmount, typeof(double), null, null);
                if (SwipeCommand?.CanExecute(amount) ?? false)
                {
                    SwipeCommand.Execute(amount);
                }
            }
        }


        private const double DefaultSwipeAmountOfPlay = 10.0;


        // 
        public static readonly DependencyProperty SwipeAmountOfPlayProperty =
           DependencyProperty.Register(nameof(SwipeAmountOfPlay)
                   , typeof(double)
                   , typeof(Swipe)
                   , new PropertyMetadata(DefaultSwipeAmountOfPlay)
               );


        /// <summary>
        /// スワイプ運動量の遊び
        /// 遊び以下のスワイプはDeltaや完了を通知しなくなります
        /// SwipeAmountOfPlay は SwipeAmountScale の影響を受けません
        /// </summary>
        public double SwipeAmountOfPlay
        {
            get { return (double)GetValue(SwipeAmountOfPlayProperty); }
            set { SetValue(SwipeAmountOfPlayProperty, value); }
        }

        public static readonly DependencyProperty SwipeDeltaRefreshLatencyProperty =
          DependencyProperty.Register(nameof(SwipeDeltaRefreshLatency)
                  , typeof(TimeSpan)
                  , typeof(Swipe)
                  , new PropertyMetadata(TimeSpan.FromMilliseconds(32), OnSwipeDeltaRefreshLatencyPropertyChanged)
              );

        public TimeSpan SwipeDeltaRefreshLatency
        {
            get { return (TimeSpan)GetValue(SwipeDeltaRefreshLatencyProperty); }
            set { SetValue(SwipeDeltaRefreshLatencyProperty, value); }
        }

        public static void OnSwipeDeltaRefreshLatencyPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            var source = (Swipe)sender;
        }


        public static readonly DependencyProperty NowSwipingProperty =
          DependencyProperty.Register(nameof(NowSwiping)
                  , typeof(bool)
                  , typeof(Swipe)
                  , new PropertyMetadata(false)
              );

        public bool NowSwiping
        {
            get { return (bool)GetValue(NowSwipingProperty); }
            set { SetValue(NowSwipingProperty, value); }
        }




        public static readonly DependencyProperty SwipeAmountProperty =
          DependencyProperty.Register(nameof(SwipeAmount)
                  , typeof(double)
                  , typeof(Swipe)
                  , new PropertyMetadata(0.0)
              );

        public double SwipeAmount
        {
            get { return (double)GetValue(SwipeAmountProperty); }
            set { SetValue(SwipeAmountProperty, value); }
        }


        public static readonly DependencyProperty SwipeAmountScaleProperty =
          DependencyProperty.Register(nameof(SwipeAmountScale)
                  , typeof(double)
                  , typeof(Swipe)
                  , new PropertyMetadata(1.0)
              );

        public double SwipeAmountScale
        {
            get { return (double)GetValue(SwipeAmountScaleProperty); }
            set { SetValue(SwipeAmountScaleProperty, value); }
        }



        public static readonly DependencyProperty SwipeAmountConverterProperty =
          DependencyProperty.Register(nameof(SwipeAmountConverter)
                  , typeof(IValueConverter)
                  , typeof(Swipe)
                  , new PropertyMetadata(null)
              );

        public IValueConverter SwipeAmountConverter
        {
            get { return (IValueConverter)GetValue(SwipeAmountConverterProperty); }
            set { SetValue(SwipeAmountConverterProperty, value); }
        }






        public static readonly DependencyProperty SwipeCommandProperty =
          DependencyProperty.Register(nameof(SwipeCommand)
                  , typeof(ICommand)
                  , typeof(Swipe)
                  , new PropertyMetadata(null)
              );

        public ICommand SwipeCommand
        {
            get { return (ICommand)GetValue(SwipeCommandProperty); }
            set { SetValue(SwipeCommandProperty, value); }
        }


        public static readonly DependencyProperty IsEnabledProperty =
          DependencyProperty.Register(nameof(IsEnabled)
                  , typeof(bool)
                  , typeof(Swipe)
                  , new PropertyMetadata(true, OnIsEnabledPropertyChanged)
              );

        public bool IsEnabled
        {
            get { return (bool)GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

        public static void OnIsEnabledPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            var source = (Swipe)sender;

            source.ResetManipulationEvent();
        }




        

        public static readonly DependencyProperty SwipeEdgeOfPlayProperty =
          DependencyProperty.Register(nameof(SwipeEdgeOfPlay)
                  , typeof(Thickness)
                  , typeof(Swipe)
                  , new PropertyMetadata(new Thickness())
              );

        public Thickness SwipeEdgeOfPlay
        {
            get { return (Thickness)GetValue(SwipeEdgeOfPlayProperty); }
            set { SetValue(SwipeEdgeOfPlayProperty, value); }
        }


        private void ResetManipulationEvent()
        {
            if (IsEnabled)
            {
                AssociatedObject.ManipulationStarted += AssociatedObject_ManipulationStarted;
                AssociatedObject.ManipulationCompleted += AssociatedObject_ManipulationCompleted;
                AssociatedObject.ManipulationDelta += AssociatedObject_ManipulationDelta;
            }
            else
            {
                if (AssociatedObject != null)
                {
                    AssociatedObject.ManipulationStarted -= AssociatedObject_ManipulationStarted;
                    AssociatedObject.ManipulationCompleted -= AssociatedObject_ManipulationCompleted;
                    AssociatedObject.ManipulationDelta -= AssociatedObject_ManipulationDelta;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uno.Threading;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Views.Extensions
{
    

    public partial class ListViewBase : DependencyObject
    {
        public interface IDeferInitialize
        {
            bool IsInitialized { get; set; }
            Task DeferInitializeAsync(CancellationToken ct = default);
        }



        public static readonly DependencyProperty DeferInitializeProperty =
           DependencyProperty.RegisterAttached(
               "DeferInitialize",
               typeof(bool),
               typeof(ListViewBase),
               new PropertyMetadata(false, DeferInitializePropertyChanged)
           );

        public static void SetDeferInitialize(UIElement element, bool value)
        {
            element.SetValue(DeferInitializeProperty, value);
        }
        public static bool GetDeferInitialize(UIElement element)
        {
            return (bool)element.GetValue(DeferInitializeProperty);
        }


        private static void DeferInitializePropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            if (s is Windows.UI.Xaml.Controls.ListViewBase target)
            {
                if ((bool)e.NewValue)
                {
                    target.ContainerContentChanging += Target_ContainerContentChanging;
                    target.Loaded += Target_Loaded;
                    target.Unloaded += Target_Unloaded;
                }
                else
                {
                    target.ContainerContentChanging -= Target_ContainerContentChanging;
                    target.Loaded -= Target_Loaded;
                    target.Unloaded -= Target_Unloaded;
                }
            }
        }

        private static async void Target_Loaded(object sender, RoutedEventArgs e)
        {
            if (_LastItemsSource != null && _LastItemsSource == (sender as Windows.UI.Xaml.Controls.ListViewBase).ItemsSource)
            {
                using (await _lock.LockAsync(default))
                {
                    _CancelledItems.Reverse();
                    var items = _CancelledItems.ToArray();
                    _CancelledItems.Clear();
                    foreach (var item in items)
                    {
                        try
                        {
                            await item.DeferInitializeAsync(_cts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            _CancelledItems.Add(item);
                        }
                    }

                    _LastItemsSource = null;
                }
            }
        }

        private static void Target_Unloaded(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }

        static FastAsyncLock _lock = new FastAsyncLock();

        static CancellationTokenSource _cts = new CancellationTokenSource();
        static List<IDeferInitialize> _CancelledItems = new List<IDeferInitialize>();
        static object _LastItemsSource;
        private static void Target_ContainerContentChanging(Windows.UI.Xaml.Controls.ListViewBase sender, Windows.UI.Xaml.Controls.ContainerContentChangingEventArgs args)
        {
            var dispatcher = sender.Dispatcher;
            if (args.Item is IDeferInitialize updatable)
            {

                if (updatable.IsInitialized) { return; }
                updatable.IsInitialized = true;

                _ = dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => 
                {
                    try
                    {
                        using (await _lock.LockAsync(_cts.Token))
                        {
                            await updatable.DeferInitializeAsync(_cts.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _CancelledItems.Add(updatable);
                    }
                });

                _LastItemsSource = sender.ItemsSource;
                // Handled = trueを指定すると、UIの描画が始まる模様
                // データ受信などが完了してない状態ではHandledを変更しない
                //args.Handled = true;
            }
        }


    }
}

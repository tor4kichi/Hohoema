#nullable enable
using Microsoft.Xaml.Interactivity;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Views.Behaviors;

// ここを参考にしました
// http://www.jasonpoon.ca/2015/01/08/resizing-webview-to-its-content/

public class WebViewAutoResizeToContent : Behavior<WebView>
	{
		protected override void OnAttached()
		{
			base.OnAttached();

			this.AssociatedObject.Loaded += AssociatedObject_Loaded;
        this.AssociatedObject.Unloaded += AssociatedObject_Unloaded;
    }

    IDisposable _disposable;
    FrameworkElement _parent;
    private void AssociatedObject_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
        _parent = this.AssociatedObject.Parent as FrameworkElement;

        var currentDispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _disposable = Observable.FromEventPattern<SizeChangedEventHandler, SizeChangedEventArgs>(
            h => _parent.SizeChanged += h,
            h => _parent.SizeChanged -= h
            )
            .Where(_=> !double.IsNaN(_parent.ActualWidth) && _parent.ActualWidth != this.AssociatedObject?.Width)
            .Do(_ =>
            {
                if (this.AssociatedObject != null)
                {
                    this.AssociatedObject.Width = _parent.ActualWidth;
                }
            })
            .Throttle(TimeSpan.FromSeconds(1))
            .Subscribe(_ => 
            {
                currentDispatcherQueue.TryEnqueue(async () => 
                {
                    await ResetSizing();
                });
            });

        _parent.Loaded += Parent_Loaded;
        this.AssociatedObject.NavigationCompleted += AssociatedObject_NavigationCompleted;
    }

    private void Parent_Loaded(object sender, RoutedEventArgs e)
    {
        this.AssociatedObject.Width = (this.AssociatedObject.Parent as FrameworkElement).ActualWidth;
    }

    private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
    {
        _parent.Loaded -= Parent_Loaded;
        _disposable.Dispose();
    }

    private async void AssociatedObject_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
    {
        await ResetSizing();
    }

    private async void AssociatedObject_LoadCompleted(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
		{
        await ResetSizing();
    }


    async Task ResetSizing()
    {
        this.AssociatedObject.Height = double.NaN;

        await Task.Delay(1);
        try
        {
            var heightString = await this.AssociatedObject.InvokeScriptAsync("eval", new[] { "document.body.scrollHeight.toString()" });
            if (int.TryParse(heightString, out var height))
            {
                if (this.AssociatedObject.Height != height)
                {
                    this.AssociatedObject.Height = height;
                }
            }
        }
        catch { }
    }

    protected override void OnDetaching()
		{
			base.OnDetaching();

        this.AssociatedObject.LoadCompleted -= AssociatedObject_LoadCompleted;
        this.AssociatedObject.Width = double.NaN;
    }



}

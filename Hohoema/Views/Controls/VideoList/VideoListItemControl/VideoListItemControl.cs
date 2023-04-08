using Hohoema.Models.VideoCache;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Views.Controls.VideoList;

public sealed class VideoListItemControl : ContentControl
{
    public VideoListItemControl()
    {
        DefaultStyleKey = typeof(VideoListItemControl);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        (GetTemplateChild("HiddenVideoOnceRevealButton") as Button).Click += HiddenVideoOnceRevealButton_Click;
        (GetTemplateChild("ExitRevealButton") as Button).Click += ExitRevealButton_Click;
    }


    public bool IsThumbnailUseCache
    {
        get { return (bool)GetValue(IsThumbnailUseCacheProperty); }
        set { SetValue(IsThumbnailUseCacheProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsThumbnailUseCache.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsThumbnailUseCacheProperty =
        DependencyProperty.Register("IsThumbnailUseCache", typeof(bool), typeof(VideoListItemControl), new PropertyMetadata(true));


    #region NG Video Owner


    public bool IsRevealHiddenVideo
    {
        get { return (bool)GetValue(IsRevealHiddenVideoProperty); }
        set { SetValue(IsRevealHiddenVideoProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsRevealHiddenVideo.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsRevealHiddenVideoProperty =
        DependencyProperty.Register("IsRevealHiddenVideo", typeof(bool), typeof(VideoListItemControl), new PropertyMetadata(false, OnIsHiddenPropertyChanged));


    private void HiddenVideoOnceRevealButton_Click(object sender, RoutedEventArgs e)
    {
        IsRevealHiddenVideo = true;
    }

    private void ExitRevealButton_Click(object sender, RoutedEventArgs e)
    {
        IsRevealHiddenVideo = false;
    }



    public bool IsHidden
    {
        get { return (bool)GetValue(IsHiddenProperty); }
        set { SetValue(IsHiddenProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsHidden.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsHiddenProperty =
        DependencyProperty.Register("IsHidden", typeof(bool), typeof(VideoListItemControl), new PropertyMetadata(false, OnIsHiddenPropertyChanged));

    private static void OnIsHiddenPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var _this = d as VideoListItemControl;

        if (_this.IsRevealHiddenVideo)
        {
            VisualStateManager.GoToState(_this, "VS_RevealHiddenVideo", false);
        }
        else if (_this.IsHidden)
        {
            VisualStateManager.GoToState(_this, "VS_HiddenVideo", false);
        }
        else
        {
            VisualStateManager.GoToState(_this, "VS_NotHiddenVideo", false);
        }
    }

    #endregion




    public object ImageSource
    {
        get { return GetValue(ImageSourceProperty); }
        set { SetValue(ImageSourceProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ThumbnailUrl.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ImageSourceProperty =
        DependencyProperty.Register("ImageSource", typeof(object), typeof(VideoListItemControl), new PropertyMetadata(null));




    public string Length
    {
        get { return (string)GetValue(LengthProperty); }
        set { SetValue(LengthProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Length.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty LengthProperty =
        DependencyProperty.Register("Length", typeof(string), typeof(VideoListItemControl), new PropertyMetadata(null));




    public bool IsQueueItem
    {
        get { return (bool)GetValue(IsQueueItemProperty); }
        set { SetValue(IsQueueItemProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsQueueItem.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsQueueItemProperty =
        DependencyProperty.Register("IsQueueItem", typeof(bool), typeof(VideoListItemControl), new PropertyMetadata(false));





    public VideoCacheStatus? CacheStatus
    {
        get { return (VideoCacheStatus?)GetValue(CacheStatusProperty); }
        set { SetValue(CacheStatusProperty, value); }
    }

    // Using a DependencyProperty as the backing store for CacheStatus.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CacheStatusProperty =
        DependencyProperty.Register("CacheStatus", typeof(VideoCacheStatus?), typeof(VideoListItemControl), new PropertyMetadata(null));





    public string PostedAt
    {
        get { return (string)GetValue(PostedAtProperty); }
        set { SetValue(PostedAtProperty, value); }
    }

    // Using a DependencyProperty as the backing store for PostedAt.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty PostedAtProperty =
        DependencyProperty.Register("PostedAt", typeof(string), typeof(VideoListItemControl), new PropertyMetadata(null));






    public string ViewCount
    {
        get { return (string)GetValue(ViewCountProperty); }
        set { SetValue(ViewCountProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ViewCount.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ViewCountProperty =
        DependencyProperty.Register("ViewCount", typeof(string), typeof(VideoListItemControl), new PropertyMetadata(null, OnViewCountPropertyChanged));

    private static void OnViewCountPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var _this = d as VideoListItemControl;
        if (_this.ViewCount == "0")
        {
            VisualStateManager.GoToState(_this, "VS_HideCountInfoLayout", false);
        }
    }

    public string CommentCount
    {
        get { return (string)GetValue(CommentCountProperty); }
        set { SetValue(CommentCountProperty, value); }
    }

    // Using a DependencyProperty as the backing store for CommentCount.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CommentCountProperty =
        DependencyProperty.Register("CommentCount", typeof(string), typeof(VideoListItemControl), new PropertyMetadata(null));





    public string MylistCount
    {
        get { return (string)GetValue(MylistCountProperty); }
        set { SetValue(MylistCountProperty, value); }
    }

    // Using a DependencyProperty as the backing store for MylistCount.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MylistCountProperty =
        DependencyProperty.Register("MylistCount", typeof(string), typeof(VideoListItemControl), new PropertyMetadata(null));





    public string Title
    {
        get { return (string)GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register("Title", typeof(string), typeof(VideoListItemControl), new PropertyMetadata(null));






    public bool IsDeleted
    {
        get { return (bool)GetValue(IsDeletedProperty); }
        set { SetValue(IsDeletedProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsDeleted.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsDeletedProperty =
        DependencyProperty.Register("IsDeleted", typeof(bool), typeof(VideoListItemControl), new PropertyMetadata(false, OnIsDeletedPropertyChanged));

    private static void OnIsDeletedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var _this = d as VideoListItemControl;
        if (_this.IsDeleted)
        {
            VisualStateManager.GoToState(_this, "VS_VideoDeleted", false);
        }
    }




    public bool IsSensitiveContent
    {
        get { return (bool)GetValue(IsSensitiveContentProperty); }
        set { SetValue(IsSensitiveContentProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsSensitiveContent.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsSensitiveContentProperty =
        DependencyProperty.Register("IsSensitiveContent", typeof(bool), typeof(VideoListItemControl), new PropertyMetadata(false, OnIsSensitiveContentPropertyChanged));

    private static void OnIsSensitiveContentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var _this = d as VideoListItemControl;
        if (_this.IsSensitiveContent)
        {
            VisualStateManager.GoToState(_this, "VS_SensitiveContent", false);
        }
    }

    public string PrivateReason
    {
        get { return (string)GetValue(PrivateReasonProperty); }
        set { SetValue(PrivateReasonProperty, value); }
    }

    // Using a DependencyProperty as the backing store for PrivateReason.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty PrivateReasonProperty =
        DependencyProperty.Register("PrivateReason", typeof(string), typeof(VideoListItemControl), new PropertyMetadata(null));





    public bool IsWatched
    {
        get { return (bool)GetValue(IsWatchedProperty); }
        set { SetValue(IsWatchedProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsWatched.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsWatchedProperty =
        DependencyProperty.Register("IsWatched", typeof(bool), typeof(VideoListItemControl), new PropertyMetadata(false, OnIsWatchedPropertyChanged));


    private static void OnIsWatchedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var _this = d as VideoListItemControl;
        if (_this.IsWatched)
        {
            VisualStateManager.GoToState(_this, "VS_Watched", false);
        }
        else
        {
            VisualStateManager.GoToState(_this, "VS_NotWatched", false);
        }


    }




    public Visibility IsRequirePayment
    {
        get { return (Visibility)GetValue(IsRequirePaymentProperty); }
        set { SetValue(IsRequirePaymentProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsRequirePayment.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsRequirePaymentProperty =
        DependencyProperty.Register("IsRequirePayment", typeof(Visibility), typeof(VideoListItemControl), new PropertyMetadata(Visibility.Collapsed));


}

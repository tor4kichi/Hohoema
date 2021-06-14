using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.VideoCache;
using Hohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Hohoema.Presentation.Views.Controls.VideoList.VideoListItem
{
    [TemplatePart(Name = "MyContentPresenter", Type = typeof(ContentPresenter))]
    public sealed partial class VideoListItem : ContentControl
    {
        public VideoListItem()
        {
            this.DefaultStyleKey = typeof(VideoListItem);
        }

        private Image _templateChildImage;

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

#if !DEBUG
            if (UIViewSettings.GetForCurrentView()?.UserInteractionMode == UserInteractionMode.Touch)
#endif
            {
                (GetTemplateChild("MobileSupportActionsLayout") as UIElement).Visibility = Visibility.Visible;
            }

            _templateChildImage = GetTemplateChild("ImagePart") as Image;
        }

        public string ImageUrl
        {
            get { return (string)GetValue(ImageUrlProperty); }
            set { SetValue(ImageUrlProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ThumbnailUrl.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImageUrlProperty =
            DependencyProperty.Register("ImageUrl", typeof(string), typeof(VideoListItem), new PropertyMetadata(null, OnImageUrlChanged));

        private static void OnImageUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (((VideoListItem)d)._templateChildImage is not null and var templatedChildImage)
            {
                templatedChildImage.Source = e.NewValue is string strUrl ? new BitmapImage(new Uri(strUrl)) : null;
            }
        }

        public string ImageSubText
        {
            get { return (string)GetValue(ImageSubTextProperty); }
            set { SetValue(ImageSubTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ImageSubText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImageSubTextProperty =
            DependencyProperty.Register("ImageSubText", typeof(string), typeof(VideoListItem), new PropertyMetadata(null));





        public bool IsQueueItem
        {
            get { return (bool)GetValue(IsQueueItemProperty); }
            set { SetValue(IsQueueItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsQueueItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsQueueItemProperty =
            DependencyProperty.Register("IsQueueItem", typeof(bool), typeof(VideoListItem), new PropertyMetadata(false, OnIsQueueItemPropertyChanged));

        private static void OnIsQueueItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var _this = (VideoListItem)d;
            if ((bool)e.NewValue)
            {
                VisualStateManager.GoToState(_this, "QueuedItemState", false);
            }
            else
            {
                VisualStateManager.GoToState(_this, "NotQueuedItemState", false);
            }
        }



        public VideoCacheStatus? CacheStatus
        {
            get { return (VideoCacheStatus? )GetValue(CacheStatusProperty); }
            set { SetValue(CacheStatusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CacheStatus.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CacheStatusProperty =
            DependencyProperty.Register("CacheStatus", typeof(VideoCacheStatus? ), typeof(VideoListItem), new PropertyMetadata(null, OnCacheStatusPropertyChanged));

        private static void OnCacheStatusPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var _this = (VideoListItem)d;

            var status = (VideoCacheStatus?)e.NewValue;
            switch (status)
            {
                case VideoCacheStatus.Pending:
                    VisualStateManager.GoToState(_this, "CacheStatusPendingState", false);
                    break;
                case VideoCacheStatus.Downloading:
                    VisualStateManager.GoToState(_this, "CacheStatusDownloadingState", false);
                    break;
                case VideoCacheStatus.DownloadPaused:
                    VisualStateManager.GoToState(_this, "CacheStatusDownloadPausedState", false);
                    break;
                case VideoCacheStatus.Completed:
                    VisualStateManager.GoToState(_this, "CacheStatusCompletedState", false);
                    break;
                case VideoCacheStatus.Failed:
                    VisualStateManager.GoToState(_this, "CacheStatusFailedState", false);
                    break;
                default:
                    VisualStateManager.GoToState(_this, "CacheStatusNormalState", false);
                    break;
            }
        }


    }
}

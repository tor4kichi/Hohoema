#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.Models.Application;
using Hohoema.Models.VideoCache;
using Hohoema.Services;
using Hohoema.ViewModels.Pages.Hohoema.VideoCache;
using Hohoema.ViewModels.VideoListPage;
using Hohoema.Views.UINavigation;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Windows.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media.Imaging;

namespace Hohoema.Views.Controls.VideoList.VideoListItem;

[TemplatePart(Name = "MyContentPresenter", Type = typeof(ContentPresenter))]
public sealed partial class VideoCacheListItem : ContentControl
{
    private static ApplicationLayoutManager? _layoutManager;
    private static AppearanceSettings? _appearanceSettings;

    public VideoCacheListItem()
    {
        this.DefaultStyleKey = typeof(VideoCacheListItem);
    }

    private Image _templateChildImage;
    UIElement _buttonActionLayout;
    private SelectorItem _listViewItem;

    CacheVideoViewModel __itemVM;
    CacheVideoViewModel _itemVM
    {
        get => __itemVM ??= (DataContext as CacheVideoViewModel)!;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _templateChildImage = GetTemplateChild("ImagePart") as Image;


        _layoutManager ??= Ioc.Default.GetRequiredService<ApplicationLayoutManager>();
        _appearanceSettings ??= Ioc.Default.GetRequiredService<AppearanceSettings>();
        _buttonActionLayout = (GetTemplateChild("ButtonActionsLayout") as UIElement)!;

        if (_layoutManager.IsMouseInteractionDefault
            && _appearanceSettings.IsVideoListItemMiddleClickToAddQueueEnabled
            )
        {
            this.PointerPressed += (s, e) =>
            {
                var pointProps = e.GetCurrentPoint(null).Properties;
                if (pointProps.IsMiddleButtonPressed)
                {
                    _itemVM.ToggleWatchAfter(_itemVM);
                }
            };
        }

        if (_appearanceSettings.IsVideoListItemAdditionalUIEnabled)
        {
            (GetTemplateChild("PlayButton") as Button)!.Click += (s, e) =>
            {
                _itemVM.PlayVideoCommand.Execute(_itemVM);
            };

            (GetTemplateChild("AddToQueueButton") as Button)!.Click += (s, e) =>
            {
                (_itemVM.ToggleWatchAfterCommand as ICommand)!.Execute(_itemVM);
            };

            if (_layoutManager.IsMouseInteraction)
            {
                this.PointerEntered += (s, e) =>
                {
                    _buttonActionLayout.Visibility = Visibility.Visible;
                };
                this.PointerExited += (s, e) =>
                {
                    _buttonActionLayout.Visibility = Visibility.Collapsed;
                };
            }
            else if (_layoutManager.IsTouchInteraction)
            {
                _buttonActionLayout.Visibility = Visibility.Visible;
            }
        }

        if (_layoutManager.IsControllerInteraction)
        {
            _listViewItem = this.FindAscendantOrSelf<SelectorItem>()!;

            UINavigationButtonEventHandler OnUINavigationPressed = (s, e) =>
            {
                if (_listViewItem!.FocusState is not FocusState.Unfocused)
                {
                    if (e == UINavigationButtons.Context1)
                    {
                        (_itemVM.ToggleWatchAfterCommand as ICommand)!.Execute(_itemVM);
                    }                    
                }
            };

            Loaded += (s, e) =>
            {
                UINavigationManager.Pressed += OnUINavigationPressed;
            };
            Unloaded += (s, e) =>
            {
                UINavigationManager.Pressed -= OnUINavigationPressed;
            };
        }
    }

    public object ImageSource
    {
        get { return GetValue(ImageSourceProperty); }
        set { SetValue(ImageSourceProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ThumbnailUrl.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ImageSourceProperty =
        DependencyProperty.Register("ImageSource", typeof(object), typeof(VideoCacheListItem), new PropertyMetadata(null, OnImageSourceChanged));

    private static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (((VideoCacheListItem)d)._templateChildImage is not null and var templatedChildImage)
        {
            if (e.NewValue is string strUrl && !string.IsNullOrEmpty(strUrl))
            {
                templatedChildImage.Source = new BitmapImage(new Uri(strUrl));
            }
            else if (e.NewValue is BitmapImage image)
            {
                templatedChildImage.Source = image;
            }
            else
            {
                templatedChildImage.Source = null;
            }
        }
    }

    

    public string ImageSubText
    {
        get { return (string)GetValue(ImageSubTextProperty); }
        set { SetValue(ImageSubTextProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ImageSubText.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ImageSubTextProperty =
        DependencyProperty.Register("ImageSubText", typeof(string), typeof(VideoCacheListItem), new PropertyMetadata(null));





    public bool IsQueueItem
    {
        get { return (bool)GetValue(IsQueueItemProperty); }
        set { SetValue(IsQueueItemProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsQueueItem.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsQueueItemProperty =
        DependencyProperty.Register("IsQueueItem", typeof(bool), typeof(VideoCacheListItem), new PropertyMetadata(false, OnIsQueueItemPropertyChanged));

    private static void OnIsQueueItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var _this = (VideoCacheListItem)d;
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
        DependencyProperty.Register("CacheStatus", typeof(VideoCacheStatus? ), typeof(VideoCacheListItem), new PropertyMetadata(null, OnCacheStatusPropertyChanged));

    private static void OnCacheStatusPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var _this = (VideoCacheListItem)d;

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

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace Hohoema.Views.Mylist;

public sealed class MylistCardView : Control
{
    public MylistCardView()
    {
        this.DefaultStyleKey = typeof(MylistCardView);
    }


    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _templateImageBrush = GetTemplateChild("ThumbnailImageBrush") as ImageBrush;
    }


    ImageBrush _templateImageBrush;


    public string PlaylistName
    {
        get { return (string)GetValue(PlaylistNameProperty); }
        set { SetValue(PlaylistNameProperty, value); }
    }

    // Using a DependencyProperty as the backing store for PlaylistName.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty PlaylistNameProperty =
        DependencyProperty.Register("PlaylistName", typeof(string), typeof(MylistCardView), new PropertyMetadata(string.Empty));





    public Uri ImageUrl
    {
        get { return (Uri)GetValue(ImageUrlProperty); }
        set { SetValue(ImageUrlProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ThumbnailUrl.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ImageUrlProperty =
        DependencyProperty.Register("ImageUrl", typeof(Uri), typeof(MylistCardView), new PropertyMetadata(null, OnImageUrlPropertyChanged));

    private static void OnImageUrlPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (((MylistCardView)d)._templateImageBrush is not null and var templatedChildImage)
        {
            templatedChildImage.ImageSource = e.NewValue is Uri url ? new BitmapImage(url) : null;
        }
    }

    public string ImageCaption
    {
        get { return (string)GetValue(ImageCaptionProperty); }
        set { SetValue(ImageCaptionProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ImageCaption.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ImageCaptionProperty =
        DependencyProperty.Register("ImageCaption", typeof(string), typeof(MylistCardView), new PropertyMetadata(string.Empty));


    public double ImageHeight
    {
        get { return (double)GetValue(ImageHeightProperty); }
        set { SetValue(ImageHeightProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ImageHeight.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ImageHeightProperty =
        DependencyProperty.Register("ImageHeight", typeof(double), typeof(MylistCardView), new PropertyMetadata(double.NaN));




    public double ImageWidth
    {
        get { return (double)GetValue(ImageWidthProperty); }
        set { SetValue(ImageWidthProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ImageWidth.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ImageWidthProperty =
        DependencyProperty.Register("ImageWidth", typeof(double), typeof(MylistCardView), new PropertyMetadata(double.NaN));




    public Stretch? ImageStretch
    {
        get { return (Stretch?)GetValue(ImageStretchProperty); }
        set { SetValue(ImageStretchProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ImageStretch.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ImageStretchProperty =
        DependencyProperty.Register("ImageStretch", typeof(Stretch?), typeof(MylistCardView), new PropertyMetadata(Stretch.Uniform));



}

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Views.Pages.Niconico.Live;

public sealed partial class LiveVideoListItem : UserControl
	{
		public LiveVideoListItem()
		{
			this.InitializeComponent();
		}



    public double ImageWidth
    {
        get { return (double)GetValue(ImageWidthProperty); }
        set { SetValue(ImageWidthProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ImageWidth.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ImageWidthProperty =
        DependencyProperty.Register("ImageWidth", typeof(double), typeof(LiveVideoListItem), new PropertyMetadata(160.0));


}

using NiconicoToolkit.Ichiba;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Hohoema.Views.Controls;

public sealed partial class IchibaItemCard : UserControl
{
    public IchibaItemCard()
    {
        this.InitializeComponent();
    }



    public IchibaItem Item
    {
        get { return (IchibaItem)GetValue(ItemProperty); }
        set { SetValue(ItemProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Item.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ItemProperty =
        DependencyProperty.Register("Item", typeof(IchibaItem), typeof(IchibaItemCard), new PropertyMetadata(0));


}

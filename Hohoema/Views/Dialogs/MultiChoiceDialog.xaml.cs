#nullable enable
using System.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Hohoema.Dialogs;

public sealed partial class MultiChoiceDialog : ContentDialog
	{
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(nameof(Items)
                , typeof(IList)
                , typeof(MultiChoiceDialog)
                , new PropertyMetadata(null)
            );

    public IList Items
    {
        get { return (IList)GetValue(ItemsProperty); }
        set { SetValue(ItemsProperty, value); }
    }


    public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.Register(nameof(SelectedItems)
                , typeof(IList)
                , typeof(MultiChoiceDialog)
                , new PropertyMetadata(null)
            );

    public IList SelectedItems
    {
        get { return (IList)GetValue(SelectedItemsProperty); }
        set { SetValue(SelectedItemsProperty, value); }
    }


    public static readonly DependencyProperty DisplayMemberPathProperty =
        DependencyProperty.Register(nameof(DisplayMemberPath)
                , typeof(string)
                , typeof(MultiChoiceDialog)
                , new PropertyMetadata(null)
            );

    public string DisplayMemberPath
    {
        get { return (string)GetValue(DisplayMemberPathProperty); }
        set { SetValue(DisplayMemberPathProperty, value); }
    }

    public MultiChoiceDialog()
		{
			this.InitializeComponent();
    }

	}

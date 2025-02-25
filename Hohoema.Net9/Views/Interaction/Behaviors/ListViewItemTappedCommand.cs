#nullable enable
using Microsoft.Xaml.Interactivity;
using Reactive.Bindings.Interactivity;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace Hohoema.Views.Behaviors;

[ContentProperty(Name = "Command")]
public class ListViewItemTappedCommandBehavior : Behavior<Windows.UI.Xaml.Controls.ListViewBase>
{
    public ICommand Command
    {
        get { return (ICommand)GetValue(CommandProperty); }
        set { SetValue(CommandProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register("Command", typeof(ICommand), typeof(ListViewItemTappedCommandBehavior), new PropertyMetadata(null));

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.IsItemClickEnabled = true;
        AssociatedObject.ItemClick += AssociatedObject_ItemClick;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.ItemClick -= AssociatedObject_ItemClick;
    }

    private void AssociatedObject_ItemClick(object sender, Windows.UI.Xaml.Controls.ItemClickEventArgs e)
    {
        var command = Command;
        if (command == null) { return; }

        var item = e.ClickedItem;
        if (item != null && command.CanExecute(item))
        {
            command.Execute(item);
        }
    }
}

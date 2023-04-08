#nullable enable
using Microsoft.Xaml.Interactivity;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Views.Interaction.Behaviors;

public sealed class MultiSelectionCommandAction : DependencyObject, IAction
{
    // Commandにアクションを設定して
    // ClickToActionTargetをクリックしたことを検知してCommandをExecuteする
    // Commandに渡すパラメータは ListViewBase.Items
    //   1. IEnumerable<object> で一回だけ
    //   2. または、objectをItemsの数だけバラバラに
    // 実行していく
    // 処理は非同期で実行され、その間はNowProcessingがTrueになる

    public ListViewBase Target
    {
        get { return (ListViewBase)GetValue(TargetProperty); }
        set { SetValue(TargetProperty, value); }
    }

    // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register(nameof(Target), typeof(ListViewBase), typeof(MultiSelectionCommandAction), new PropertyMetadata(default(Control)));


    public ICommand Command
    {
        get { return (ICommand)GetValue(CommandProperty); }
        set { SetValue(CommandProperty, value); }
    }

    // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(MultiSelectionCommandAction), new PropertyMetadata(default(ICommand)));

  
    public object Execute(object sender, object parameter)
    {
        var command = Command;
        if (command == null) { return false; }


        var items = Target.SelectedItems.ToArray();
        if (command.CanExecute(items))
        {
            command.Execute(items);

            Target.DeselectRange(new Windows.UI.Xaml.Data.ItemIndexRange(0, (uint)items.Length));
        }
        else
        {
            var failedItems = new List<object>();
            foreach (var item in items)
            {
                try
                {
                    if (command.CanExecute(item))
                    {
                        command.Execute(item);
                    }

                    Target.SelectedItems.Remove(item);
                }
                catch
                {
                    failedItems.Add(item);
                }
            }
        }

        return true;
    }
}

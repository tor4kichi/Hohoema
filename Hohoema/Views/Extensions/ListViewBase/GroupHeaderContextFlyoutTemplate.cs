#nullable enable
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Views.Extensions;

public partial class ListViewBase
{
    public static readonly DependencyProperty GroupHeaderContextFlyoutTemplateProperty =
        DependencyProperty.RegisterAttached(
            "GroupHeaderContextFlyoutTemplate",
            typeof(DataTemplate),
            typeof(ListViewBase),
            new PropertyMetadata(default(DataTemplate), GroupHeaderContextFlyoutTemplatePropertyChanged)
        );

    public static void SetGroupHeaderContextFlyoutTemplate(UIElement element, DataTemplate value)
    {
        element.SetValue(GroupHeaderContextFlyoutTemplateProperty, value);
    }
    public static DataTemplate GetGroupHeaderContextFlyoutTemplate(UIElement element)
    {
        return (DataTemplate)element.GetValue(GroupHeaderContextFlyoutTemplateProperty);
    }





    public static readonly DependencyProperty GroupHeaderContextFlyoutTemplateSelectorProperty =
        DependencyProperty.RegisterAttached(
            "GroupHeaderContextFlyoutTemplateSelector",
            typeof(DataTemplateSelector),
            typeof(ListViewBase),
            new PropertyMetadata(default(DataTemplateSelector), GroupHeaderContextFlyoutTemplateSelectorPropertyChanged)
        );

    public static void SetGroupHeaderContextFlyoutTemplateSelector(UIElement element, DataTemplateSelector value)
    {
        element.SetValue(GroupHeaderContextFlyoutTemplateSelectorProperty, value);
    }
    public static DataTemplateSelector GetGroupHeaderContextFlyoutTemplateSelector(UIElement element)
    {
        return (DataTemplateSelector)element.GetValue(GroupHeaderContextFlyoutTemplateSelectorProperty);
    }




    public static readonly DependencyProperty GroupHeaderContextFlyoutCustomObjectToTagProperty =
        DependencyProperty.RegisterAttached(
            "GroupHeaderContextFlyoutCustomObjectToTag",
            typeof(object),
            typeof(ListViewBase),
            new PropertyMetadata(default(object))
        );

    public static void SetGroupHeaderContextFlyoutCustomObjectToTag(UIElement element, object value)
    {
        element.SetValue(GroupHeaderContextFlyoutCustomObjectToTagProperty, value);
    }
    public static object GetGroupHeaderContextFlyoutCustomObjectToTag(UIElement element)
    {
        return (object)element.GetValue(GroupHeaderContextFlyoutCustomObjectToTagProperty);
    }


    private static void GroupHeaderContextFlyoutTemplatePropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
    {
        if (s is Windows.UI.Xaml.Controls.ListViewBase target && e.NewValue is DataTemplate template)
        {
            target.ContextRequested += (sender, args) => 
            {
                var flyout = template.LoadContent() as Windows.UI.Xaml.Controls.Primitives.FlyoutBase;
                // XboxOne向けのハック
                if (args.OriginalSource is ListViewBaseHeaderItem container)
                {
                    container.DataContext = container.Content;
                    flyout.ShowAt(container);
                    args.Handled = true;
                }
                else
                {
                    flyout.ShowAt(args.OriginalSource as FrameworkElement);
                    args.Handled = true;
                }
            };
        }
    }

    private static void GroupHeaderContextFlyoutTemplateSelectorPropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
    {
        if (s is Windows.UI.Xaml.Controls.ListViewBase target && e.NewValue is DataTemplateSelector templateSelector)
        {
            target.ChoosingGroupHeaderContainer += (sender, args) =>
            {
                if (args.GroupHeaderContainer is FrameworkElement fe)
                {
                    var dataContext = args.GroupHeaderContainer.Content;
                    var dataTemplate = templateSelector.SelectTemplate(dataContext, args.GroupHeaderContainer);
                    var flyout = dataTemplate.LoadContent() as Windows.UI.Xaml.Controls.Primitives.FlyoutBase;
                    var contentPresenter = fe.FindFirstChild<FrameworkElement>();
                    fe.ContextFlyout = flyout;
                    fe.DataContext = contentPresenter.DataContext;
                    var customContext = GetGroupHeaderContextFlyoutCustomObjectToTag(target);
                    if (customContext != null)
                    {
                        GroupHeaderFlyoutOpenerDataContextSetToTag(flyout, customContext);
                    }
                }
            };
        }
    }






    private static void GroupHeaderFlyoutOpenerDataContextSetToTag(Windows.UI.Xaml.Controls.Primitives.FlyoutBase flyoutbase, object dataContextToTag)
    {
        if (flyoutbase is MenuFlyout menuFlyout)
        {
            foreach (var menuItem in menuFlyout.Items)
            {
                GroupHeaderRecurciveSettingDataContext(menuItem, dataContextToTag);
            }
        }
        else if (flyoutbase is Flyout flyout)
        {
            if (flyout.Content is FrameworkElement fe)
            {
                //                    fe.DataContext = dataContext;
                fe.Tag = dataContextToTag;
            }
        }
    }

    private static void GroupHeaderRecurciveSettingDataContext(MenuFlyoutItemBase item, object dataContextToTag)
    {
        item.Tag = dataContextToTag;
        if (item is MenuFlyoutSubItem subItem)
        {
            foreach (var child in subItem.Items)
            {
                GroupHeaderRecurciveSettingDataContext(child, dataContextToTag);
            }
        }
    }


}

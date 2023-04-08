#nullable enable
using Microsoft.Xaml.Interactivity;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Views.Behaviors;

public class MenuFlyoutSubItemItemsSetter : Behavior<MenuFlyoutSubItem>
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource)
            , typeof(IEnumerable)
            , typeof(MenuFlyoutSubItemItemsSetter)
            , new PropertyMetadata(Enumerable.Empty<object>())
        );

    public IEnumerable ItemsSource
    {
        get { return (IEnumerable)GetValue(ItemsSourceProperty); }
        set { SetValue(ItemsSourceProperty, value); }
    }




    public static readonly DependencyProperty ItemTemplateProperty =
        DependencyProperty.Register(
            nameof(ItemTemplate)
            , typeof(DataTemplate)
            , typeof(MenuFlyoutSubItemItemsSetter)
            , new PropertyMetadata(default(DataTemplate))
        );

    public DataTemplate ItemTemplate
    {
        get { return (DataTemplate)GetValue(ItemTemplateProperty); }
        set { SetValue(ItemTemplateProperty, value); }
    }





    public static readonly DependencyProperty CustomObjectToTagProperty =
        DependencyProperty.Register(
            nameof(CustomObjectToTag)
            , typeof(object)
            , typeof(MenuFlyoutSubItemItemsSetter)
            , new PropertyMetadata(default(object))
        );

    public object CustomObjectToTag
    {
        get { return (object)GetValue(CustomObjectToTagProperty); }
        set { SetValue(CustomObjectToTagProperty, value); }
    }



    public static readonly DependencyProperty IsRequireInsertSeparaterBetweenDefaultItemsProperty =
        DependencyProperty.Register(
            nameof(IsRequireInsertSeparaterBetweenDefaultItems)
            , typeof(bool)
            , typeof(MenuFlyoutSubItemItemsSetter)
            , new PropertyMetadata(true)
        );

    /// <summary>
    /// デフォルトで配置された子アイテムとItemsSourceから生成された子アイテムの間にセパレータを配置するかのフラグです<br />
    /// </summary>
    /// <remarks>default is true</remarks>
    public bool IsRequireInsertSeparaterBetweenDefaultItems
    {
        get { return (bool)GetValue(IsRequireInsertSeparaterBetweenDefaultItemsProperty); }
        set { SetValue(IsRequireInsertSeparaterBetweenDefaultItemsProperty, value); }
    }








    bool IsRequireReset { get; set; } = true;

    List<MenuFlyoutItemBase> _addedMenuFlyoutItems = new List<MenuFlyoutItemBase>();

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject != null)
        {
            AssociatedObject.Loaded += OnLoaded;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
    }

    private void ResetItems()
    {
        if (AssociatedObject == null) { return; }

        var menuFlyout = AssociatedObject;
        var itemsSrouce = ItemsSource.Cast<object>().ToList();
        var customObjectToTag = CustomObjectToTag;


        // 前回追加分のメニューアイテムを削除
        foreach (var oldItem in _addedMenuFlyoutItems)
        {
            menuFlyout.Items.Remove(oldItem);
        }

        _addedMenuFlyoutItems.Clear();


        // これから配置するアイテムとの間にセパレータが必要な場合
        if (IsRequireInsertSeparaterBetweenDefaultItems)
        {
            if (itemsSrouce.Count >= 1 && menuFlyout.Items.Count >= 1)
            {
                var separator = new MenuFlyoutSeparator();
                menuFlyout.Items.Add(separator);
                _addedMenuFlyoutItems.Add(separator);
            }
        }

        // ItemTemplateからMenuFlyoutItem系のインスタンスを生成してMenuFlyoutSubItemに追加する
        foreach (var item in itemsSrouce)
        {
            var templatedContent = ItemTemplate.LoadContent();
            if (templatedContent is MenuFlyoutItemBase flyoutItem)
            {
                flyoutItem.DataContext = item;
                flyoutItem.Tag = customObjectToTag ?? flyoutItem.Tag;

                menuFlyout.Items.Add(flyoutItem);
                _addedMenuFlyoutItems.Add(flyoutItem);
            }
#if DEBUG
            else
            {
                throw new Infra.HohoemaException($"{nameof(MenuFlyoutItemsSetter)}.{nameof(ItemTemplate)} is must be MenuFlyoutItemBase inherit class.");
            }
#endif
        }
    }




    #region Event Handler

    private void OnLoaded(object sender, object e)
    {
        ResetItems();

        AssociatedObject.Loaded -= OnLoaded;
    }

    
    #endregion
}

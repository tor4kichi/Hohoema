using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Hohoema.Presentation.Views.Extensions
{
    public class ContextFlyoutExtension : DependencyObject
    {
        // Note: Flyoutのキャッシュは保持しません
        //
        // 理由としては、動画アイテム向けのFlyoutでマイリストの子アイテムを
        // 削除したり新規作成できるよう対応していますが、Flyoutをキャッシュして使いまわしてしまうと
        // 子アイテムの操作が反映されない（最初に作られた時点の表示で固定になってしまう）ため
        // キャッシュしない方針にしています


        public static readonly DependencyProperty FlyoutTemplateSelectorProperty =
            DependencyProperty.RegisterAttached(
                "FlyoutTemplateSelector",
                typeof(DataTemplateSelector),
                typeof(ContextFlyoutExtension),
                new PropertyMetadata(default(DataTemplateSelector), FlyoutTemplateSelectorPropertyChanged)
            );

        public static void SetFlyoutTemplateSelector(UIElement element, DataTemplateSelector value)
        {
            element.SetValue(FlyoutTemplateSelectorProperty, value);
        }
        public static DataTemplateSelector GetFlyoutTemplateSelector(UIElement element)
        {
            return (DataTemplateSelector)element.GetValue(FlyoutTemplateSelectorProperty);
        }


        static Dictionary<UIElement, DataTemplateSelector> SelectorDict = new Dictionary<UIElement, DataTemplateSelector>();


        private static void FlyoutTemplateSelectorPropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                var uiElement = s as UIElement;
                if (uiElement == null)
                {
                    throw new NotSupportedException($"FlyoutTemplateSelector must attached to UIElement. now attach to {s?.ToString() ?? "null"}");
                }

                SelectorDict.Add(uiElement, e.NewValue as DataTemplateSelector);
                uiElement.ContextRequested += UiElement_ContextRequested;
            }
            else
            {
                var uiElement = s as UIElement;
                if (SelectorDict.TryGetValue(uiElement, out var pair))
                {
                    uiElement.ContextRequested -= UiElement_ContextRequested;
                    SelectorDict.Remove(uiElement);
                }
            }
        }


        
        private static void UiElement_ContextRequested(UIElement sender, Windows.UI.Xaml.Input.ContextRequestedEventArgs args)
        {
            if (SelectorDict.TryGetValue(sender, out var flyoutTemplateSelector))
            {
                object dataContext = null;
                FrameworkElement element = null;

                // コントローラ操作かつListViewBase系コントロールの場合に対応する
                // フォーカスできるUIはListViewItemのItemContainer側に限定されるため
                // ListViewItemやGriViewItemの基底クラスであるContentControlまでダウンキャストしている
                if (args.OriginalSource is ContentControl)
                {
                    var contentControl = args.OriginalSource as ContentControl;
                    dataContext = contentControl.Content;
                    element = contentControl.ContentTemplateRoot as FrameworkElement;
                }
                else if (args.OriginalSource is FrameworkElement)
                {
                    element = args.OriginalSource as FrameworkElement;
                    dataContext = element?.DataContext;
                }

                if (dataContext != null)
                {
                    var template = flyoutTemplateSelector.SelectTemplate(dataContext, element);
                    if (template != null)
                    {
                        var flyout = template.LoadContent() as Windows.UI.Xaml.Controls.Primitives.FlyoutBase;

                        if (flyout != null)
                        {
                            // elementのDataContextがFlyoutに注入された上で表示される
                            flyout.ShowAt(element);
                            args.Handled = true;
                        }
                    }
                }
            }
        }
        
    }
}

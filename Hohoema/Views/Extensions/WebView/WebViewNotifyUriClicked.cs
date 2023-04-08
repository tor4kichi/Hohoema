using System.Windows.Input;
using Windows.UI.Xaml;

namespace Hohoema.Views.Extensions;


public partial class WebView : DependencyObject
{
    public static readonly DependencyProperty UriNotifyCommandProperty =
       DependencyProperty.RegisterAttached(
           "UriNotifyCommand",
           typeof(ICommand),
           typeof(WebView),
           new PropertyMetadata(default(ICommand), UriNotifyCommandPropertyChanged)
       );

    public static void SetUriNotifyCommand(UIElement element, ICommand value)
    {
        element.SetValue(UriNotifyCommandProperty, value);
    }
    public static ICommand GetUriNotifyCommand(UIElement element)
    {
        return (ICommand)element.GetValue(UriNotifyCommandProperty);
    }

    private static void UriNotifyCommandPropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
    {
        var wevView = s as Windows.UI.Xaml.Controls.WebView;

        if (e.NewValue is ICommand command)
        {
            wevView.NavigationStarting += (sender, args) =>
            {
                // 参考元：http://bluewatersoft.cocolog-nifty.com/blog/2013/10/windows-re-wind.html

                // テンポラリファイルからWebページを表示したとき、
                // 参考元の実装では同一ホスト判定を識別できなかったので
                // 条件を書き換えています。
                //			if (sender.Source.Host != args.Uri.Host)
                if (args.Uri != null)
                {
                    args.Cancel = true;

                    if (command?.CanExecute(args.Uri) ?? false)
                    {
                        command.Execute(args.Uri);
                    }
                }
            };

            wevView.NavigationCompleted += (sender, args) =>
            {
                _ = sender.InvokeScriptAsync("eval", new string[] {
@"
var anchors = document.getElementsByTagName('a');
for (var i = 0; i<anchors.length; i++) {
  anchors[i].target = '';
}"
                });
            };
        }
    }
}

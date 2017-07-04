using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace NicoPlayerHohoema.Views
{
	/// <summary>
	/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
	/// </summary>
	public sealed partial class UserMylistPage : Page
	{
		public UserMylistPage()
		{
			this.InitializeComponent();
		}
	}


    public class MylistListTemplateSelector : DataTemplateSelector
    {
        public DataTemplate LocalMylist { get; set; }
        public DataTemplate LoginUserMylist { get; set; }
        public DataTemplate OtherOwneredMylist { get; set; }


        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is ViewModels.MylistItemsWithTitle)
            {
                var origin = (item as ViewModels.MylistItemsWithTitle).Origin;
                switch (origin)
                {
                    case Models.PlaylistOrigin.LoginUser:
                        return LoginUserMylist;
                    case Models.PlaylistOrigin.OtherUser:
                        return OtherOwneredMylist;
                    case Models.PlaylistOrigin.Local:
                        return LocalMylist;
                    default:
                        break;
                }
            }

            return base.SelectTemplateCore(item, container);
        }
    }
}

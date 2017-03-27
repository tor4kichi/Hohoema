using Prism.Windows.Mvvm;
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
using Microsoft.Toolkit.Uwp.UI.Animations;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace NicoPlayerHohoema.Views
{
	/// <summary>
	/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
	/// </summary>
	public sealed partial class VideoPlayerControl : UserControl
    {

        public static readonly DependencyProperty IsDisplayCommentTextBoxProperty =
           DependencyProperty.Register("IsDisplayCommentTextBox"
                   , typeof(bool)
                   , typeof(VideoPlayerControl)
                   , new PropertyMetadata(false)
               );

        public bool IsDisplayCommentTextBox
        {
            get { return (bool)GetValue(IsDisplayCommentTextBoxProperty); }
            set { SetValue(IsDisplayCommentTextBoxProperty, value); }
        }

        public VideoPlayerControl()
		{
			this.InitializeComponent();

			this.Unloaded += VideoPlayerPage_Unloaded;
		}

		private void VideoPlayerPage_Unloaded(object sender, RoutedEventArgs e)
		{
			(this.DataContext as IDisposable)?.Dispose();
		}

        
    }



	
}

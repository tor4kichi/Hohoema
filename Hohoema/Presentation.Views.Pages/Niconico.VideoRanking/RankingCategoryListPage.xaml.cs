﻿using Hohoema.Presentation.ViewModels.Pages.Niconico.VideoRanking;
using NiconicoToolkit.Ranking.Video;
using Microsoft.Toolkit.Mvvm.Input;
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
using Microsoft.Toolkit.Mvvm.DependencyInjection;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Presentation.Views.Pages.Niconico.VideoRanking
{
	/// <summary>
	/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
	/// </summary>
	public sealed partial class RankingCategoryListPage : Page
	{
		public RankingCategoryListPage()
		{
			this.InitializeComponent();

            DataContext = _vm = Ioc.Default.GetRequiredService<RankingCategoryListPageViewModel>();
        }

		private readonly RankingCategoryListPageViewModel _vm;
    }
}

﻿using Hohoema.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

namespace Hohoema.Views
{
	/// <summary>
	/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
	/// </summary>
	public sealed partial class SettingsPage : Page
	{
		public SettingsPage()
		{
			this.InitializeComponent();
		}

		public ImmutableArray<ApplicationInteractionMode?> OverrideInteractionModeList { get; } = new List<ApplicationInteractionMode?>()
		{
			null,
			ApplicationInteractionMode.Controller,
			ApplicationInteractionMode.Mouse,
			ApplicationInteractionMode.Touch,
		}.ToImmutableArray();

		public IReadOnlyCollection<string> AvairableLocales { get; } = I18NPortable.I18N.Current.Languages.Select(x => x.Locale).ToList();

		bool IsDebug =>
#if DEBUG
			true;
#else
			false;
#endif

		// アピアランス
		public IReadOnlyCollection<ElementTheme?> _elementThemeList { get; } =
			Enum.GetValues(typeof(ElementTheme)).Cast<ElementTheme?>()
			.ToList();

	}
}

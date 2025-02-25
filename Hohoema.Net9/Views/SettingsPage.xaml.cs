#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.Models.Application;
using Hohoema.ViewModels.Pages.Hohoema;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Windows.Globalization.NumberFormatting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Views.Pages.Hohoema;

/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class SettingsPage : Page
{
	public SettingsPage()
	{
		this.InitializeComponent();

		SetNumberBoxNumberFormatter();

		DataContext = _vm = Ioc.Default.GetRequiredService<SettingsPageViewModel>();
	}

	private readonly SettingsPageViewModel _vm;

	private void SetNumberBoxNumberFormatter()
	{
		IncrementNumberRounder rounder = new IncrementNumberRounder();
		rounder.Increment = 0.01;
		rounder.RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp;

		DecimalFormatter formatter = new DecimalFormatter();
		formatter.IntegerDigits = 1;
		formatter.FractionDigits = 2;
		formatter.NumberRounder = rounder;
		MaxCacheSizeNumberBox.NumberFormatter = formatter;
	}




	public ImmutableArray<ApplicationInteractionMode?> OverrideInteractionModeList
	{
		get { return (ImmutableArray<ApplicationInteractionMode?>)GetValue(OverrideInteractionModeListProperty); }
		set { SetValue(OverrideInteractionModeListProperty, value); }
	}

	public static readonly DependencyProperty OverrideInteractionModeListProperty =
		DependencyProperty.Register("OverrideInteractionModeList", typeof(ImmutableArray<ApplicationInteractionMode?>), typeof(SettingsPage), new PropertyMetadata(new List<ApplicationInteractionMode?>()
        {
            null,
            ApplicationInteractionMode.Controller,
            ApplicationInteractionMode.Mouse,
            ApplicationInteractionMode.Touch,
        }.ToImmutableArray()));


	


	public IReadOnlyCollection<string> AvairableLocales
	{
		get { return (IReadOnlyCollection<string>)GetValue(AvairableLocalesProperty); }
		set { SetValue(AvairableLocalesProperty, value); }
	}

	public static readonly DependencyProperty AvairableLocalesProperty =
		DependencyProperty.Register("AvairableLocales", typeof(IReadOnlyCollection<string>), typeof(SettingsPage), new PropertyMetadata(I18NPortable.I18N.Current.Languages.Select(x => x.Locale).ToList()));


	bool IsDebug =>
#if DEBUG
		true;
#else
			false;
#endif



	public IReadOnlyCollection<ElementTheme?> ElementThemeList
	{
		get { return (IReadOnlyCollection<ElementTheme?>)GetValue(ElementThemeListProperty); }
		set { SetValue(ElementThemeListProperty, value); }
	}

	public static readonly DependencyProperty ElementThemeListProperty =
		DependencyProperty.Register("ElementThemeList", typeof(IReadOnlyCollection<ElementTheme?>), typeof(SettingsPage), new PropertyMetadata(Enum.GetValues(typeof(ElementTheme)).Cast<ElementTheme?>().ToList()));
}



	public sealed class VideoCacheMaxSizeDoubleGigaByte2NullableLongByteConverter : IValueConverter
{
		const double GigaByte = 1000_000_000.0;


		public object Convert(object value, Type targetType, object parameter, string language)
    {
			long a = 0;
        if (value is null or long)
        {
				var longValue = ((long?)value);
				var val = longValue is not null ? ((double)longValue) / GigaByte : double.NaN;
				return val;
			}

			throw new NotSupportedException();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is double)
        {
				var doubleValue = (double)value;

				var val = !double.IsNaN(doubleValue) ? (long)Math.Max(doubleValue * GigaByte, 0) : 0;
				return val != 0 ? new long?(val) : default(long?);
			}

			throw new NotSupportedException();
    }
}

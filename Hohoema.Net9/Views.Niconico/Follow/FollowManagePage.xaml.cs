#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Follow;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.Video;
using Hohoema.ViewModels.Pages.Niconico.Follow;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Views.Pages.Niconico.Follow;

/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class FollowManagePage : Page
	{
		public FollowManagePage()
		{
			this.InitializeComponent();
			DataContext = _vm = Ioc.Default.GetRequiredService<FollowManagePageViewModel>();
		}

		private readonly FollowManagePageViewModel _vm;
	}

	public class FollowTypeToSymbolIconConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is FollowItemType)
			{
				var type = (FollowItemType)value;

				switch (type)
				{
					case FollowItemType.User:
						return Symbol.Contact;
					case FollowItemType.Tag:
						return Symbol.Tag;
					case FollowItemType.Mylist:
						return Symbol.List;
                case FollowItemType.Channel:
                    return Symbol.OtherUser;
					case FollowItemType.Community:
						return Symbol.People;
					default:
						throw new NotSupportedException();
				}
			}
			else
			{
				throw new NotSupportedException();
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}


public sealed class FollowTypeItemTemplateSelector : DataTemplateSelector
{
	public DataTemplate UserItemTemplate { get; set; }
	public DataTemplate TagItemTemplate { get; set; }
	public DataTemplate MylistItemTemplate { get; set; }
	public DataTemplate ChannelItemTemplate { get; set; }

	protected override DataTemplate SelectTemplateCore(object item)
	{
		return SelectTemplateCore(item, null);
	}

	protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
	{
		return item switch
		{
			IUser => UserItemTemplate,
			ITag => TagItemTemplate,
			IMylist => MylistItemTemplate,
			IChannel => ChannelItemTemplate,
			_ => throw new NotSupportedException(),
		};
	}
}
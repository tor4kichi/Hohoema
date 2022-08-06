using Hohoema.Models.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Hohoema.Presentation.ViewModels.Pages.Niconico.Follow;
using Hohoema.Models.Domain.Niconico.Follow;
using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.Community;
using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Presentation.Navigations;
using CommunityToolkit.Mvvm.DependencyInjection;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Presentation.Views.Pages.Niconico.Follow
{
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
		public DataTemplate CommunityItemTemplate { get; set; }


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
				ICommunity => CommunityItemTemplate,
				_ => throw new NotSupportedException(),
			};
		}
    }
}

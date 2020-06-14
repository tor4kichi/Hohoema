using Mntone.Nico2.Videos.Comment;
using NicoPlayerHohoema.ViewModels;
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
using Prism.Ioc;
using System.Reactive.Disposables;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using Windows.UI.Core;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views
{
	public sealed partial class CommentCommandEditer : UserControl
	{
		
		CoreDispatcher _dispatcher;
		public CommentCommandEditer()
		{
			this.InitializeComponent();

			_dispatcher = Dispatcher;
		}

		private void AllCommandReset(object sender, RoutedEventArgs e)
		{
			CommentSizePallete.SelectedValue = null;
			AlingmentPallete.SelectedValue = null;
			ColorPallete.SelectedValue = null;
		}



		public string CommandString
		{
			get { return (string)GetValue(CommandStringProperty); }
			set { SetValue(CommandStringProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CommandString.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CommandStringProperty =
			DependencyProperty.Register("CommandString", typeof(string), typeof(CommentCommandEditer), new PropertyMetadata(string.Empty));



		public bool ChannelOrCommunityVideoModeEnable
		{
			get { return (bool)GetValue(ChannelOrCommunityVideoModeEnableProperty); }
			set { SetValue(ChannelOrCommunityVideoModeEnableProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ChannelOrCommunityVideoModeEnable.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ChannelOrCommunityVideoModeEnableProperty =
			DependencyProperty.Register("ChannelOrCommunityVideoModeEnable", typeof(bool), typeof(CommentCommandEditer), new PropertyMetadata(false));



		List<CommandType?> SizeCommandItems = new List<CommandType?>()
		{
			CommandType.big,
			CommandType.medium,
			CommandType.small
		};

		List<CommandType?> AlingmentCommandItems = new List<CommandType?>()
		{
			CommandType.ue,
			CommandType.naka,
			CommandType.shita
		};

		List<CommandType?> ColorCommandItems = new List<CommandType?>()
		{
			CommandType.white,
			CommandType.red,
			CommandType.pink,
			CommandType.orange,
			CommandType.yellow,
			CommandType.green,
			CommandType.cyan,
			CommandType.blue,
			CommandType.purple,
			CommandType.black,
		};

		List<CommandType?> ColorPremiumCommandItems = new List<CommandType?>()
		{
			CommandType.white2,
			CommandType.red2,
			CommandType.pink2,
			CommandType.orange2,
			CommandType.yellow2,
			CommandType.green2,
			CommandType.cyan2,
			CommandType.blue2,
			CommandType.purple2,
			CommandType.black2,
			CommandType.white,
			CommandType.red,
			CommandType.pink,
			CommandType.orange,
			CommandType.yellow,
			CommandType.green,
			CommandType.cyan,
			CommandType.blue,
			CommandType.purple,
			CommandType.black,
		};

	}

	public class CommentCommandTemplateSelector : DataTemplateSelector
	{
		public DataTemplate EmptyTemplate { get; set; }
		public DataTemplate Big { get; set; }
		public DataTemplate Midium { get; set; }
		public DataTemplate Small { get; set; }

		public DataTemplate Ue { get; set; }
		public DataTemplate Naka { get; set; }
		public DataTemplate Shita { get; set; }

		public DataTemplate White { get; set; }
		public DataTemplate Red { get; set; }
		public DataTemplate Pink { get; set; }
		public DataTemplate Orange { get; set; }
		public DataTemplate Yellow { get; set; }
		public DataTemplate Green { get; set; }
		public DataTemplate Cyan { get; set; }
		public DataTemplate Blue { get; set; }
		public DataTemplate Purple { get; set; }
		public DataTemplate Black { get; set; }


		public DataTemplate White2 { get; set; }
		public DataTemplate Red2 { get; set; }
		public DataTemplate Pink2 { get; set; }
		public DataTemplate Orange2 { get; set; }
		public DataTemplate Yellow2 { get; set; }
		public DataTemplate Green2 { get; set; }
		public DataTemplate Cyan2 { get; set; }
		public DataTemplate Blue2 { get; set; }
		public DataTemplate Purple2 { get; set; }
		public DataTemplate Black2 { get; set; }



		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			if (item == null) { return EmptyTemplate; }

			CommandType? type = item as CommandType?;

			if (type.HasValue)
			{
				switch (type.Value)
				{
					case CommandType.big:
						return Big;
					case CommandType.medium:
						return Midium;
					case CommandType.small:
						return Small;
					case CommandType.ue:
						return Ue;
					case CommandType.naka:
						return Naka;
					case CommandType.shita:
						return Shita;
					case CommandType.white:
						return White;
					case CommandType.red:
						return Red;
					case CommandType.pink:
						return Pink;
					case CommandType.orange:
						return Orange;
					case CommandType.yellow:
						return Yellow;
					case CommandType.green:
						return Green;
					case CommandType.cyan:
						return Cyan;
					case CommandType.blue:
						return Blue;
					case CommandType.purple:
						return Purple;
					case CommandType.black:
						return Black;
					case CommandType.white2:
						return White2;
					case CommandType.niconicowhite:
						break;
					case CommandType.red2:
						return Red2;
					case CommandType.truered:
						break;
					case CommandType.pink2:
						return Pink2;
					case CommandType.orange2:
						return Orange2;
					case CommandType.passionorange:
						break;
					case CommandType.yellow2:
						return Yellow2;
					case CommandType.madyellow:
						break;
					case CommandType.green2:
						return Green2;
					case CommandType.elementalgreen:
						break;
					case CommandType.cyan2:
						return Cyan2;
					case CommandType.blue2:
						return Blue2;
					case CommandType.marineblue:
						break;
					case CommandType.purple2:
						return Purple2;
					case CommandType.nobleviolet:
						break;
					case CommandType.black2:
						return Black2;
					case CommandType.full:
						break;
					case CommandType._184:
						break;
					case CommandType.invisible:
						break;
					case CommandType.all:
						break;
					case CommandType.from_button:
						break;
					case CommandType.is_button:
						break;
					case CommandType._live:
						break;
					default:
						break;
				}

				return base.SelectTemplateCore(item, container);
			}
			else
			{
				return base.SelectTemplateCore(item, container);
			}
		}
	}
}

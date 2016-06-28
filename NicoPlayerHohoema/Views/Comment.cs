using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.ViewModels;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Views
{
	public class Comment : BindableBase
	{
		public string CommentText { get; set; }

		public uint CommentId { get; set; }

		public string UserId { get; set; }

		public uint VideoPosition { get; set; }
		public uint EndPosition { get; set; }

		public Color Color { get; set; }

		public uint FontSize { get; set; }

		private VerticalAlignment? _VAlign;
		public VerticalAlignment? VAlign
		{
			get
			{
				return _VAlign;
			}
			set
			{
				_VAlign = value;
			}
		}

		public HorizontalAlignment? HAlign { get; set; }

		public bool IsOwnerComment { get; set; }

		private bool _IsVisible = true;
		public bool IsVisible
		{
			get { return _IsVisible && !IsNGComment; }
			set { SetProperty(ref _IsVisible, value); }
		}

		public bool IsAnonimity { get; set; } = false;

		public bool IsScrolling
		{
			get
			{
				return !(VAlign.HasValue || HAlign.HasValue);
			}
		}

		public TextWrapping TextWrapping
		{
			get
			{
				if (IsOwnerComment && VAlign.HasValue)
				{
					return TextWrapping.Wrap;
				}
				else
				{
					return TextWrapping.NoWrap;
				}
			}
		}

		public Comment(VideoPlayerPageViewModel videoPlayerPageVM)
		{
			_VideoPlayerPageViewModel = videoPlayerPageVM;
		}



		private DelegateCommand _AddNgUserCommand;
		public DelegateCommand AddNgUserCommand
		{
			get
			{
				return _AddNgUserCommand
					?? (_AddNgUserCommand = new DelegateCommand(async () =>
					{
						await _VideoPlayerPageViewModel.AddNgUser(this);
					}));
			}
		}



		private NGResult _NgResult;
		public NGResult NgResult
		{
			get { return _NgResult; }
			set
			{
				if (SetProperty(ref _NgResult, value))
				{
					OnPropertyChanged(nameof(IsNGComment));
					OnPropertyChanged(nameof(IsNGDescription));

					OnPropertyChanged(nameof(IsVisible));
				}
			}
		}


		public bool IsNGComment
		{
			get
			{
				return _NgResult != null;
			}
		}

		public string IsNGDescription
		{
			get
			{
				return _NgResult?.NGDescription ?? "";
			}
		}






		private VideoPlayerPageViewModel _VideoPlayerPageViewModel;
	}
}

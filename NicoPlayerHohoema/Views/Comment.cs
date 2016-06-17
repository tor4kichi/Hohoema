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

		public bool IsVisible { get; set; } = true;

		public bool IsAnonimity { get; set; } = false;

		public bool IsScrolling
		{
			get
			{
				return !(VAlign.HasValue || HAlign.HasValue);
			}

		}
	}
}

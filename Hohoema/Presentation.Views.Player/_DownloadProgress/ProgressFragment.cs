using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.Views.Player.DownloadProgress
{
	public class ProgressFragment : ObservableObject
	{
		public ProgressFragment(double invertedTotalSize, uint start, uint end)
		{
			InvertedTotalSize = invertedTotalSize;
			Start = start;
			End = end;
		}

		public double InvertedTotalSize { get; private set; }

		public uint Start { get; private set; }

		private uint _End;
		public uint End
		{
			get { return _End; }
			set { SetProperty(ref _End, value); }
		}


		public double GetStartPositionInCanvas(double canvasWidth)
		{
			var lerp = InvertedTotalSize * Start;
			return lerp * canvasWidth;
		}

		public double GetWidthInCanvas(double canvasWidth)
		{
			var lerp = InvertedTotalSize * (End - Start);
			return lerp * canvasWidth;
		}
	}
}

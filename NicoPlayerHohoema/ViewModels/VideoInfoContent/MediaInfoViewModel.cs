using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.VideoInfoContent
{
	abstract public class MediaInfoViewModel : BindableBase
	{
		public string Title { get; private set; }


		public MediaInfoViewModel(string title)
		{
			Title = title;
		}
	}
}

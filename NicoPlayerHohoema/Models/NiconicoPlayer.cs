using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public class NiconicoPlayer : BindableBase
	{
		internal NiconicoPlayer(HohoemaApp app)
		{

		}


		private string _VideoUrl;
		public string VideoUrl
		{
			get { return _VideoUrl; }
			set { SetProperty(ref _VideoUrl, value); }
		}


		
	}
}

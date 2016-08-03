using Mntone.Nico2.Mylist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace NicoPlayerHohoema.Util
{
	public static class NiconicoIconTypeHelper
	{
		public static Color ToColor(this IconType iconType)
		{
			switch (iconType)
			{
				case IconType.Default:
					return Colors.LightYellow;
				case IconType.Cyan:
					return Colors.Cyan;
				case IconType.SmokeWhite:
					return Colors.WhiteSmoke;
				case IconType.Dark:
					return Colors.DarkGray;
				case IconType.Red:
					return Colors.Red;
				case IconType.Orenge:
					return Colors.Orange;
				case IconType.Green:
					return Colors.Green;
				case IconType.SkyBlue:
					return Colors.SkyBlue;
				case IconType.Blue:
					return Colors.Blue;
				case IconType.Purple:
					return Colors.Purple;
				default:
					return Colors.LightYellow;
			}
		}
	}
}

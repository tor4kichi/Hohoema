using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI;

// this code from http://www.cambiaresearch.com/articles/25/convert-hex-string-to-dotnet-color
// 


namespace NicoPlayerHohoema.Helpers
{
	public static class ColorExtention
	{
		public static Color HexStringToColor(string hexColor)
		{
			string hc = ExtractHexDigits(hexColor);
			if (hc.Length != 6)
			{
				// you can choose whether to throw an exception
				//throw new ArgumentException("hexColor is not exactly 6 digits.");
				return new Color();
			}
			string r = hc.Substring(0, 2);
			string g = hc.Substring(2, 2);
			string b = hc.Substring(4, 2);
			Color color = new Color();
			color.A = 0xff;
			try
			{
				byte ri
				   = byte.Parse(r, System.Globalization.NumberStyles.HexNumber);
				byte gi
				   = byte.Parse(g, System.Globalization.NumberStyles.HexNumber);
				byte bi
				   = byte.Parse(b, System.Globalization.NumberStyles.HexNumber);
				color = ColorHelper.FromArgb(255, ri, gi, bi);
			}
			catch
			{
				// you can choose whether to throw an exception
				//throw new ArgumentException("Conversion failed.");
				return new Color();
			}
			return color;
		}


		/// <summary>
		/// Extract only the hex digits from a string.
		/// </summary>
		public static string ExtractHexDigits(string input)
		{
			// remove any characters that are not digits (like #)
			Regex isHexDigit
			   = new Regex("[abcdefABCDEF\\d]+", RegexOptions.Compiled);
			string newnum = "";
			foreach (char c in input)
			{
				if (isHexDigit.IsMatch(c.ToString()))
					newnum += c.ToString();
			}
			return newnum;
		}


		public static Color ToInverted(this Color color)
		{
			byte inv_R = (byte)(byte.MaxValue - color.R);
			byte inv_G = (byte)(byte.MaxValue - color.G);
			byte inv_B = (byte)(byte.MaxValue - color.B);

			return new Color()
			{
				A = byte.MaxValue,
				R = inv_R,
				G = inv_G,
				B = inv_B
			};
		}
	}

}

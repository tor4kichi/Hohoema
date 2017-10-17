using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Helpers
{
	public static class FilePathHelper
	{
		public static string ToSafeFilePath(this string fileName)
		{
			var invalidChars = Path.GetInvalidFileNameChars();
			return new String(fileName
				.Where(x => !invalidChars.Any(y => x == y))
				.ToArray()
				);
		}

		public static string ToSafeDirectoryPath(this string fileName)
		{
			var invalidChars = Path.GetInvalidPathChars();
			return new String(fileName
				.Where(x => !invalidChars.Any(y => x == y))
				.ToArray()
				);
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Util
{
	public static class StorageFolderExtention
	{
		public static bool ExistFile(this StorageFolder folder, string filename)
		{
			var path = Path.Combine(folder.Path, filename);
			return File.Exists(path);
		}
	}
}

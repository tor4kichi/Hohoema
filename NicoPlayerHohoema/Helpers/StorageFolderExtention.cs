using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Helpers
{
	public static class StorageFolderExtention
	{
		public static Task<bool> ExistFile(this StorageFolder folder, string filename)
		{
			if (folder == null) { return Task.FromResult(false); }

			var path = Path.Combine(folder.Path, filename);
			return Task.Run<bool>(() => { return File.Exists(path); });
		}
	}
}

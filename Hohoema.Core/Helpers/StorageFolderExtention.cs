#nullable enable
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Hohoema.Helpers;

public static class StorageFolderExtention
{
    public static Task<bool> ExistFile(this StorageFolder folder, string filename)
    {
        if (folder == null) { return Task.FromResult(false); }

        string path = Path.Combine(folder.Path, filename);
        return Task.Run<bool>(() => { return File.Exists(path); });
    }
}

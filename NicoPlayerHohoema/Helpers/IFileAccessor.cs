using System.Threading.Tasks;
using Newtonsoft.Json;
using Windows.Storage;

namespace NicoPlayerHohoema.Helpers
{
    public interface IFileAccessor<T>
    {
        string FileName { get; }

        Task<bool> Delete(StorageDeleteOption option = StorageDeleteOption.Default);
        Task<bool> ExistFile();
        Task<T> Load(JsonSerializerSettings settings = null);
        Task<bool> Rename(string filename, bool forceReplace = false);
        Task Save(T item, JsonSerializerSettings settings = null);
        Task<StorageFile> TryGetFile();
    }
}
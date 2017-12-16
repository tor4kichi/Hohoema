using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Windows.Storage;

namespace NicoPlayerHohoema.Helpers
{
    public class FileAccessor<T> : IFileAccessor<T>
    {
        AsyncLock _Lock = new AsyncLock();
        public StorageFile File { get; }

        public string FileName => File.Name;

        public FileAccessor(StorageFile file)
        {
            File = file;
        }

        public async Task<bool> Delete(StorageDeleteOption option = StorageDeleteOption.Default)
        {
            using (var releaser = await _Lock.LockAsync())
            {
                try
                {
                    await File.DeleteAsync();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public async Task<bool> ExistFile()
        {
            using (var releaser = await _Lock.LockAsync())
            {
                return File?.IsAvailable ?? false;
            }
        }

        public async Task<T> Load(JsonSerializerSettings settings = null)
        {
            using (var releaser = await _Lock.LockAsync())
            {
                if (!await ExistFile()) { return default(T); }

                settings = settings ?? new Newtonsoft.Json.JsonSerializerSettings();

                var text = await FileIO.ReadTextAsync(File);

                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(text, settings);
            }
        }

        public async Task<bool> Rename(string filename, bool forceReplace = false)
        {
            using (var releaser = await _Lock.LockAsync())
            {
                if (!await ExistFile()) { return false; }

                await File.RenameAsync(filename, forceReplace ? NameCollisionOption.ReplaceExisting : NameCollisionOption.FailIfExists);

                return true;
            }
        }

        public async Task Save(T item, JsonSerializerSettings settings = null)
        {
            using (var releaser = await _Lock.LockAsync())
            {
                settings = settings ?? new JsonSerializerSettings();
                await FileIO.WriteTextAsync(File, Newtonsoft.Json.JsonConvert.SerializeObject(item, settings));
            }
        }

        public Task<StorageFile> TryGetFile()
        {
            return Task.FromResult(File);
        }
    }


    public class FolderBasedFileAccessor<T> : IFileAccessor<T>
    {
		public StorageFolder Folder { get; private set; }
		public string FileName { get; private set; }
		private SemaphoreSlim _ReadWriteLock;

		public FolderBasedFileAccessor(StorageFolder folder, string fileName)
		{
			Folder = folder;
			FileName = fileName;

			_ReadWriteLock = new SemaphoreSlim(1, 1);
		}

		public Task<bool> ExistFile()
		{
			return Folder.ExistFile(FileName);
		}

        public async Task<StorageFile> TryGetFile()
        {
            return await Folder.TryGetItemAsync(FileName) as StorageFile;
        }

		public async Task Save(T item, Newtonsoft.Json.JsonSerializerSettings settings = null)
		{
			if (settings == null)
			{
				settings = new Newtonsoft.Json.JsonSerializerSettings();
			}

			try
			{
				await _ReadWriteLock.WaitAsync();
				var file = await Folder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);

				await FileIO.WriteTextAsync(file, Newtonsoft.Json.JsonConvert.SerializeObject(item, settings));
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
			}
			finally
			{
				_ReadWriteLock.Release();
			}
		}


		

		public async Task<T> Load(Newtonsoft.Json.JsonSerializerSettings settings = null)
		{
			if (settings == null)
			{
				settings = new Newtonsoft.Json.JsonSerializerSettings();
			}

			try
			{
				await _ReadWriteLock.WaitAsync();

				if (false == await Folder.ExistFile(FileName))
				{
					return default(T);
				}

				var file = await Folder.GetFileAsync(FileName);
				var text = await FileIO.ReadTextAsync(file);

				return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(text, settings);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
			}
			finally
			{
				_ReadWriteLock.Release();
			}

			return default(T);
		}

		/// <summary>
		/// ファイルを削除します
		/// ファイルが存在して削除が実行できた場合に true を返します
		/// </summary>
		/// <param name="option">StorageDeleteOption</param>
		/// <returns>ファイルが存在して削除を実行できた = true <br /> ファイルが存在しない、または削除に失敗 = false</returns>
		public async Task<bool> Delete(StorageDeleteOption option = StorageDeleteOption.Default)
		{
			

			try
			{
				await _ReadWriteLock.WaitAsync();

				if (false == await Folder.ExistFile(FileName))
				{
					return false;
				}

				var file = await Folder.GetFileAsync(FileName);

				await file.DeleteAsync(option);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
				return false;
			}
			finally
			{
				_ReadWriteLock.Release();
			}

			return true;
		}




		/// <summary>
		/// ファイル名を変更します。
		/// 拡張子も必要です。
		/// ファイルが存在しない場合は何もせず false を返します。
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		public async Task<bool> Rename(string filename, bool forceReplace = false)
		{
			try
			{
				await _ReadWriteLock.WaitAsync();

				if (false == await Folder.ExistFile(FileName))
				{
					return false;
				}

				var file = await Folder.GetFileAsync(FileName);

                await file.RenameAsync(filename);

				FileName = filename;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
				return false;
			}
			finally
			{
				_ReadWriteLock.Release();
			}

			return true;
		}
	}
}

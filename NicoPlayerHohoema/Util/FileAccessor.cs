using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Util
{
	public class FileAccessor<T>
	{
		public StorageFolder Folder { get; private set; }
		public string FileName { get; private set; }
		private SemaphoreSlim _ReadWriteLock;

		public FileAccessor(StorageFolder folder, string fileName)
		{
			Folder = folder;
			FileName = fileName;

			_ReadWriteLock = new SemaphoreSlim(1, 1);
		}

		public bool ExistFile()
		{
			return Folder.ExistFile(FileName);
		}

		public async Task Save(T item)
		{
			try
			{
				await _ReadWriteLock.WaitAsync();
				var file = await Folder.CreateFileAsync(FileName, CreationCollisionOption.OpenIfExists);
				await FileIO.WriteTextAsync(file, Newtonsoft.Json.JsonConvert.SerializeObject(item));
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


		

		public async Task<T> Load()
		{
			

			try
			{
				await _ReadWriteLock.WaitAsync();

				if (!Folder.ExistFile(FileName))
				{
					return default(T);
				}

				var file = await Folder.GetFileAsync(FileName);
				var text = await FileIO.ReadTextAsync(file);

				return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(text);
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

				if (!Folder.ExistFile(FileName))
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
	}
}

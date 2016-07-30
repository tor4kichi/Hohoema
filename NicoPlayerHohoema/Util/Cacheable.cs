using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace NicoPlayerHohoema.Util
{
	abstract public class Cacheable<T>
//		where T : new()
	{
		// WatchApiとかThumbnailInfoを管理するベースクラス

		public StorageFolder SaveFolder { get; private set; }
		public string FileName { get; private set; }


		/// <summary>
		/// 次のキャッシュ更新予定時間
		/// Update(true)またはGetItem(true)を使用した場合、無視されます
		/// </summary>
		public DateTime NextCacheUpdateTime { get; private set; }


		/// <summary>
		/// キャッシュ更新までの時間
		/// </summary>
		public TimeSpan ExpirationTime { get; set; }


		private T _CachedItem;

		private SemaphoreSlim _FileReadWriteLock;


		
		public Cacheable(StorageFolder saveFolder, string filename)
		{
			SaveFolder = saveFolder;
			FileName = filename;

			_FileReadWriteLock = new SemaphoreSlim(1, 1);
			_CachedItem = default(T);
			ExpirationTime = TimeSpan.FromMinutes(10);
			NextCacheUpdateTime = DateTime.Now;
		}



		

		protected virtual bool CanGetLatest { get { return true; } }


		abstract protected Task<T> GetLatest();


		protected virtual void UpdateToRecent(T item) { }
		protected virtual void UpdateToLatest(T item) { }


		protected virtual void PreUpdateToLatest() { }


		public bool CacheIsLatest
		{
			get
			{
				return NextCacheUpdateTime > DateTime.Now;
			}
		}

		public T CachedItem { get { return _CachedItem; } }


		public bool HasCache { get { return _CachedItem != null; } }


		public async Task UpdateFromLocal()
		{
			var item = await GetRecent();

			if (item != null)
			{
				_CachedItem = item;

				UpdateToRecent(_CachedItem);
			}
		}

		public async Task Update(bool requireLatest = false)
		{
			if (requireLatest || !CacheIsLatest || _CachedItem == null)
			{
				if (CanGetLatest)
				{
					PreUpdateToLatest();

					var item = await GetLatest();

					if (item != null)
					{
						_CachedItem = item;
						NextCacheUpdateTime = DateTime.Now + ExpirationTime;

						UpdateToLatest(_CachedItem);
					}
				}
			}

			if (_CachedItem == null)
			{
				await UpdateFromLocal();
			}
		}


		public async Task<T> GetItem(bool requireLatest = false)
		{
			await Update(requireLatest);

			return _CachedItem;
		}

		private async Task<T> GetRecent()
		{
			if (!ExistCachedFile())
			{
				return default(T);
			}


			T item = default(T);
			StorageFile file;
			file = await SaveFolder.GetFileAsync(FileName);
			string jsonText;
			try
			{
				await _FileReadWriteLock.WaitAsync();
				jsonText = await FileIO.ReadTextAsync(file);
			}
			finally
			{
				_FileReadWriteLock.Release();
			}

			item = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonText);

			return item;
		}


		public bool ExistCachedFile()
		{
			return SaveFolder.ExistFile(FileName);
		}

		public async Task Save()
		{
			if (_CachedItem == null)
			{
				_CachedItem = await GetItem(true);
				if (!CacheIsLatest)
				{
					return;
				}
			}

			var jsonText = Newtonsoft.Json.JsonConvert.SerializeObject(_CachedItem);
			var file = await SaveFolder.CreateFileAsync(FileName, CreationCollisionOption.OpenIfExists);

			try
			{
				await _FileReadWriteLock.WaitAsync();
				await FileIO.WriteTextAsync(file, jsonText);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
			}
			finally
			{
				_FileReadWriteLock.Release();
			}
		}


		public async Task DoCacheFileAction(Action<StorageFile> action)
		{
			if (!ExistCachedFile())
			{
				return;
			}

			try
			{
				await _FileReadWriteLock.WaitAsync();

				var file = await SaveFolder.GetFileAsync(FileName);

				action(file);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
			}
			finally
			{
				_FileReadWriteLock.Release();
			}
		}

		

		public async Task Delete(StorageDeleteOption option = StorageDeleteOption.Default)
		{
			if (!ExistCachedFile())
			{
				return;
			}

			try
			{
				await _FileReadWriteLock.WaitAsync();

				var file = await SaveFolder.GetFileAsync(FileName);

				await file.DeleteAsync(option);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
			}
			finally
			{
				_FileReadWriteLock.Release();
			}

		}

	}
}

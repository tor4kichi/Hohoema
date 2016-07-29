using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Util
{
	abstract public class Cacheable<T>
//		where T : new()
	{
		// WatchApiとかThumbnailInfoを管理するベースクラス

		public StorageFolder SaveFolder { get; private set; }
		public string FileName { get; private set; }

		public DateTime RecentCacheTime { get; private set; }
		public bool CacheIsLatest { get; private set; }

		public TimeSpan ExpirationTime { get; set; }

		private T _CachedItem;
		private SemaphoreSlim _FileReadWriteLock;


		
		public Cacheable(StorageFolder saveFolder, string filename)
		{
			SaveFolder = saveFolder;
			FileName = filename;

			_FileReadWriteLock = new SemaphoreSlim(1, 1);
			CacheIsLatest = false;
			_CachedItem = default(T);
			ExpirationTime = TimeSpan.FromMinutes(10);
		}


		public T CachedItem { get { return _CachedItem; } }


		public bool HasCache { get { return _CachedItem != null; } }


		protected virtual bool CanGetLatest { get { return true; } }


		abstract protected Task<T> GetLatest();


		protected virtual void UpdateToRecent(T item) { }
		protected virtual void UpdateToLatest(T item) { }


		protected virtual void PreUpdateToLatest() { }


		public async Task UpdateFromLocal()
		{
			var item = await GetRecent();

			if (item != null)
			{
				_CachedItem = item;
				CacheIsLatest = false;

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
						CacheIsLatest = true;
						RecentCacheTime = DateTime.Now;

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
			if (!ExistLocalCache())
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


		

		public async Task Delete(StorageDeleteOption option = StorageDeleteOption.Default)
		{
			if (!ExistLocalCache())
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

		private bool ExistLocalCache()
		{
			return System.IO.File.Exists(Path.Combine(SaveFolder.Path, FileName));
		}
	}
}
